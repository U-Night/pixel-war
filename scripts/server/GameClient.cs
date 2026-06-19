using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class GameClient : IDisposable {
	private readonly TcpClient tcpClient;
	public IPEndPoint RemoteEndPoint { get; private set; }
	private readonly uint id;
	private readonly TcpMessageFramer tcpMessageFramer;
	private bool _disposed; // Pour le Garbage Controller
	public DateTime lastSeen { get; private set; } // Calculer le ping du client pour savoir s'il est encore vivant
	
	// Relatif aux coordonnées
	public volatile uint lastSequenceId = 0;
	public volatile float dx = 0.0f;
	public volatile float dy = 0.0f;

	// ✅ Équipe assignée par le serveur (-1 = pas encore assigné)
	public int TeamId { get; set; } = -1;

	public GameClient(TcpClient tcpClient, uint id) {
		this.tcpClient = tcpClient;
		this.RemoteEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
		this.lastSeen = DateTime.Now;
		
		// On délègue la lecture des messages à une classe utilitaire pour s'assurer qu'on réspècte le protocole !
		this.tcpMessageFramer = new TcpMessageFramer(tcpClient.GetStream()); 
		this.id = id;
	}

	public uint GetId() { return this.id; }

	public async Task<bool> PerformHandshake() {
		// Première étape: Le serveur dit "PIXELWAR 1.0" (il s'annonce et sa version de protocole)
		/*
			Exemple de Handshake:
			server: PIXELWAR 1.0
			client: REMOTE 1.0
			server: WELCOME {id}
		*/
		// On envoie PIXELWAR 1.0 via le Framer (texte pur)
		Packet handShake = new Packet(PacketType.Handshake, "PIXELWAR 1.0");
		var serialized = handShake.Serialize();
		await tcpMessageFramer.SendAsync(serialized);

		Packet? clientVersion = await ReceiveAsync();
		if (clientVersion == null) return false;
		string clientVersionString = Encoding.UTF8.GetString(clientVersion.Data);
		if (clientVersionString != "REMOTE 1.0") return false;

		Packet handshake = new Packet(PacketType.Handshake, $"WELCOME {id}");
		serialized = handshake.Serialize();
		await tcpMessageFramer.SendAsync(serialized);

		// On a réussi notre handshake à partir d'ici !!
		return true;
	}

	public bool IsConnected => tcpClient.Connected && !_disposed;

	/// <summary>
	/// Graceful shutdown
	/// </summary>
	/// <exception cref="NotImplementedException"></exception>
	public void Dispose() {
		if (_disposed) return;
		tcpClient.Close();
		_disposed = true;
	}

	/// <summary>
	/// Envoie un paquet au client
	/// </summary>
	public async Task SendPacketAsync(String message) {
		Packet packet = new Packet(PacketType.Message, message);
		byte[] serialized = packet.Serialize();
		
		await tcpMessageFramer.SendAsync(serialized);
	}

	// ✅ Surcharge avec un PacketType explicite (utilisé pour TeamAssignment)
	public async Task SendPacketAsync(PacketType type, String message) {
		Packet packet = new Packet(type, message);
		byte[] serialized = packet.Serialize();

		await tcpMessageFramer.SendAsync(serialized);
	}
	
	/// <summary>
	/// Reçoit un paquet du client
	/// </summary>
	public async Task<Packet?> ReceiveAsync() {
		byte[] message = await tcpMessageFramer.ReceiveAsync();
		if (message == null) return null;
		
		// On reconvertit le texte en octets via UTF-8 pour désérialiser le Packet
		Packet p = Packet.Deserialize(message);
		return p;
	}

	public async Task HandleJoystickEvent(uint sequenceId, float x, float y) {
		if (sequenceId <= lastSequenceId) return; // On discard un sequence id plus récent que ce qu'on a déjà reçu.
		dx = x;
		dy = y;
	}
	
	public void Ping() {
		// On met à jour le dernier ping reçu
		lastSeen = DateTime.Now;
	}


}