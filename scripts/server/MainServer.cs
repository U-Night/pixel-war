using Godot;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// ❌ L'enum Team a été retiré d'ici, il vit maintenant dans PlayerCharacter.cs

public partial class MainServer : Node {
	private TcpListener listener;
	private UdpClient udpClient;
	private IPAddress _address;
	private ushort _port;
	private bool started = true; // enable graceful shutdown
	private uint counter = 1;
	public readonly ConcurrentDictionary<uint, GameClient> _clients = new();

	// ✅ Compteur pour le round-robin. Interlocked.Increment garantit qu'en cas
	// de connexions simultanées, deux clients ne reçoivent jamais le même slot.
	private int _teamCounter = 0;
	private bool _isServerRunning = false;

	public MainServer() {}
	
	public void StartServer(){
		if (_isServerRunning) {
			GD.Print("[NOTICE][MainServer] Server is already running. Ignoring start request.");
			return;
		}
		
		_isServerRunning = true;
		GD.Print("[NOTICE][MainServer] Starting Game Server on all interfaces, port 6967");
		
		StartAsync(IPAddress.Any, 6967).ContinueWith(task => {
			if (task.IsFaulted) {	
				GD.PrintErr($"[ERROR][MainServer] Failed to start TCP server: {task.Exception}");
			}
		});

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
		while (started) {
			TcpClient _client = await listener.AcceptTcpClientAsync();
			_ = this.HandleClientAsync(_client, counter++);
		}
	}

	private async Task StartUdpServerAsync(IPAddress addr, ushort port) {
		udpClient = new UdpClient(new IPEndPoint(addr, port));
		GD.Print($"[NOTICE][MainServer] Started Game UDP Server. Bound to {this._address}:{this._port}");
		while (started) {
			UdpReceiveResult result = await udpClient.ReceiveAsync();
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

		try {
			bool isSuccessful = await gameClient.PerformHandshake();

			if (!isSuccessful) {
				GD.PrintErr("[ERROR][MainServer] Un client n'a pas passé le handshake");
				client.Dispose();
				return;
			}

			// ════════════════════════════════════════════════════════════════
			// ✅ MODIFIÉ : on utilise PlayerCharacter.TEAMS au lieu de l'ancien Team
			// ════════════════════════════════════════════════════════════════
			PlayerCharacter.TEAMS team = (PlayerCharacter.TEAMS)((Interlocked.Increment(ref _teamCounter) - 1) % 4);
			gameClient.TeamId = (int)team;

			// Format : "TEAM_ASSIGNED:{0-3}"  ex: "TEAM_ASSIGNED:0" = BLUE
			await gameClient.SendPacketAsync(PacketType.TeamAssignment, $"TEAM_ASSIGNED:{(int)team}");
			// ════════════════════════════════════════════════════════════════

			_clients.TryAdd(id, gameClient);
			GD.Print($"[INFO][MainServer] Un client s'est connecété id: {id} → équipe {team}");

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

		int headerSize = Marshal.SizeOf<UdpPacket.PacketHeader>();
		if (buffer.Length < headerSize) return;

		ReadOnlySpan<byte> span = buffer;
		ReadOnlySpan<byte> headerSpan = span.Slice(0, headerSize);

		UdpPacket.PacketHeader header = MemoryMarshal.Read<UdpPacket.PacketHeader>(headerSpan);

		ReadOnlySpan<byte> dataForCrc = span.Slice(4);
		uint calculatedCrc = ComputeCrc32(dataForCrc);
		if (header.Crc32 != calculatedCrc) {
			GD.PrintErr("CRC Mismatch! Paquet corrompu ou falsifié.");
			return;
		}

		if (!_clients.TryGetValue(header.UserId, out GameClient client)) {
			GD.PrintErr($"[ERROR][MainServer] Received UDP packet from unknown UserId: {header.UserId}");
			return; 
		}

		if (!result.RemoteEndPoint.Address.MapToIPv4().Equals(client.RemoteEndPoint.Address)) return;

		switch (header.PacketType) {
			case UdpPacket.PacketType.Joystick:
				int payloadSize = Marshal.SizeOf<UdpPacket.PlayerInputPayload>();
				if (buffer.Length < headerSize + payloadSize) return;

				ReadOnlySpan<byte> payloadSpan = span.Slice(headerSize, payloadSize);
				UdpPacket.PlayerInputPayload payload = MemoryMarshal.Read<UdpPacket.PlayerInputPayload>(payloadSpan);

				client.HandleJoystickEvent(header.SequenceId, payload.X, payload.Y);
				break;
			case UdpPacket.PacketType.Ping:
				client.Ping();
				break;
		}
	}

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

	public void ResetServer() {
		// Déconnecter tous les clients
		foreach (var client in _clients.Values) {
			client.Dispose();
		}
		_clients.Clear();
		
		// Remettre le compteur d'équipes à 0
		Interlocked.Exchange(ref _teamCounter, 0);
		
		GD.Print("[INFO][MainServer] Le serveur a été réinitialisé.");
	}
}