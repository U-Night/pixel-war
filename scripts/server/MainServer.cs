using Godot;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class MainServer : Node {
	private TcpListener listener;
	private UdpClient udpClient;
	private IPAddress _address;
	private ushort _port;
	private bool started = true; // enable graceful shutdown
	private uint counter = 1;
	public readonly ConcurrentDictionary<uint, GameClient> _clients = new();

	public MainServer() {}
	
	public void StartServer(){
		GD.Print("[NOTICE][MainServer] Starting Game Server on all interfaces, port 6967");
		
		// Démarrage du serveur TCP
		StartAsync(IPAddress.Any, 6967).ContinueWith(task => {
			if (task.IsFaulted) {	
				GD.PrintErr($"[ERROR][MainServer] Failed to start TCP server: {task.Exception}");
			}
		});

		// Démarrage du serveur UDP en parallèle (même port)
		StartUdpServerAsync(IPAddress.Any, 6967).ContinueWith(task => {
			if (task.IsFaulted) {	
				GD.PrintErr($"[ERROR][MainServer] Failed to start UDP server: {task.Exception}");
			}
		});
	}

	public void StartGameServer(IPAddress addr, ushort port) {
		GD.Print($"[NOTICE][MainServer] Starting Game UDP Server on {addr}:{port}");
		
	}

	private async Task StartAsync(IPAddress addr, ushort port) {
		listener = new(addr, port);
		_address = addr;
		_port = port;
		listener.Start();

		GD.Print($"[NOTICE][MainServer] Started Game Server. Bound to {this._address}:{this._port}");
		// Gérer la connexion des clients
		while (started) { // Boucle qui accèpte des nouveaux clients.
			TcpClient _client = await listener.AcceptTcpClientAsync();
			_ = this.HandleClientAsync(_client, counter++); /// Multiplexage: Ajouter un client et le placer dans un thread
		}
	}

	private async Task StartUdpServerAsync(IPAddress addr, ushort port) {
		udpClient = new UdpClient(new IPEndPoint(addr, port));
		GD.Print($"[NOTICE][MainServer] Started Game UDP Server. Bound to {this._address}:{this._port}");
		while (started) {
			UdpReceiveResult result = await udpClient.ReceiveAsync();
			// Traiter le paquet reçu de manière synchrone car très rapide
			HandleUdpPacket(result);
		}
	}

	private async Task StopAsync() {
		started = false;
		listener.Stop();
		GD.Print("[NOTICE][MainServer] Stopped Game Server.");
	}

	private async Task HandleClientAsync(TcpClient client, uint id) {
		GD.Print("[INFO][MainServer] Un client s'est connecté.");
		GameClient gameClient = new(client, id);
		// Etape du HandShake (pour l'instant sans TLS)
		try {
			bool isSuccessful = await gameClient.PerformHandshake();

			if (!isSuccessful) {
				GD.PrintErr("[ERROR][MainServer] Un client n'a pas passé le handshake");
				client.Dispose();
				return;
			}

			// On stocke les clients 
			_clients.TryAdd(id, gameClient);
			GD.Print($"[INFO][MainServer] Un client s'est connecété id: {id}");

			while (gameClient.IsConnected) {
				Packet? packet = await gameClient.ReceiveAsync();
				if (packet == null) break;

				await HandlePacketAsync(gameClient, packet);
			}
		} catch (ProtocolViolationException ex) {
			GD.PrintErr($"[ERROR][MainServer] Protocol violation error: {ex.Message}");
			client.Close();
		}
	}

	private async Task HandlePacketAsync(GameClient sender, Packet packet) {
		
		switch (packet.Type) {
			case PacketType.Disconnect:
				sender.Dispose();
				GD.Print($"[INFO][MainServer] Le client {sender.GetId()} s'est déconnecté");
				break;
			default:
				GD.Print("[INFO][MainServer] Paquet non géré reçu : ", packet);
				GD.Print($"[INFO][MainServer] {{{sender.GetId()}}} {packet.GetDataAsString()}");
				break;
		}
	}

	private void HandleUdpPacket(UdpReceiveResult result) {
		byte[] buffer = result.Buffer;

		// 1. Taille minimale (c'est la taille de notre Header)
		int headerSize = Marshal.SizeOf<UdpPacket.PacketHeader>();
		if (buffer.Length < headerSize) return; // Discard (Paquet trop petit)
		

		ReadOnlySpan<byte> span = buffer;
		ReadOnlySpan<byte> headerSpan = span.Slice(0, headerSize);

		// 2. Décoder le header SANS allocation de mémoire
		UdpPacket.PacketHeader header = MemoryMarshal.Read<UdpPacket.PacketHeader>(headerSpan);

		// --- VERIFICATION DU CRC32 ---
		// On calcule le CRC sur l'intégralité du paquet, en excluant les 4 premiers octets (qui contiennent le CRC lui-même)
		ReadOnlySpan<byte> dataForCrc = span.Slice(4);
		uint calculatedCrc = ComputeCrc32(dataForCrc);
		if (header.Crc32 != calculatedCrc) {
			GD.PrintErr("CRC Mismatch! Paquet corrompu ou falsifié.");
			return; // Discard
		}
		// -----------------------------

		// 3. Vérifier le UserId pour voir si c'est un joueur connu
		if (!_clients.TryGetValue(header.UserId, out GameClient client)) {
			// Joueur inconnu ou non connecté
			GD.PrintErr($"[ERROR][MainServer] Received UDP packet from unknown UserId: {header.UserId}");
			return; 
		}

		// Vérifier que l'IP de provenance UDP correspond bien à l'IP du socket TCP du client !
		if (!result.RemoteEndPoint.Address.MapToIPv4().Equals(client.RemoteEndPoint.Address)) return;

		// 4. Traiter la payload en fonction du type
		switch (header.PacketType) {
			case UdpPacket.PacketType.Joystick:
				int payloadSize = Marshal.SizeOf<UdpPacket.PlayerInputPayload>();
				if (buffer.Length < headerSize + payloadSize) return; // Données corrompues/manquantes

                ReadOnlySpan<byte> payloadSpan = span.Slice(headerSize, payloadSize);
				UdpPacket.PlayerInputPayload payload = MemoryMarshal.Read<UdpPacket.PlayerInputPayload>(payloadSpan);

				// Traitement des inputs (Exemple: stocker les données dans GameClient)
				client.HandleJoystickEvent(header.SequenceId, payload.X, payload.Y);
				break;
			case UdpPacket.PacketType.Ping:
				
				client.Ping(); // Mettre à jour le dernier ping reçu
				break;
		}
	}

	// Implémentation super rapide et standalone du Crc32 (sans package externe)
	private static readonly uint[] Crc32Table = GenerateCrc32Table();
	private static uint[] GenerateCrc32Table() {
		var table = new uint[256];
		uint polynomial = 0xEDB88320;
		for (uint i = 0; i < 256; i++) {
			uint crc = i;
			for (uint j = 8; j > 0; j--) {
				if ((crc & 1) == 1)
					crc = (crc >> 1) ^ polynomial;
				else
					crc >>= 1;
			}
			table[i] = crc;
		}
		return table;
	}

	public static uint ComputeCrc32(ReadOnlySpan<byte> bytes) {
		uint crc = 0xFFFFFFFF;
		for (int i = 0; i < bytes.Length; i++) {
			byte index = (byte)((crc & 0xFF) ^ bytes[i]);
			crc = (crc >> 8) ^ Crc32Table[index];
		}
		return ~crc;
	}
}
