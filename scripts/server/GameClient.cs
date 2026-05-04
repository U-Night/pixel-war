using Godot;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public partial class GameClient : IDisposable {
	private readonly TcpClient tcpClient;
	private readonly int id;
	private readonly TcpMessageFramer tcpMessageFramer;
	private bool _disposed; // Pour le Garbage Controller

	public GameClient(TcpClient tcpClient, int id) {
		this.tcpClient = tcpClient;
		// On délègue la lecture des messages à une classe utilitaire pour s'assurer qu'on réspècte le protocole !
		this.tcpMessageFramer = new TcpMessageFramer(tcpClient.GetStream()); 
		this.id = id;
	}

	public int GetId() { return this.id; }

	public async Task<bool> PerformHandshake() {
		// Première étape: Le serveur dit "PIXELWAR 1.0" (il s'annonce et sa version de protocole)
		/*
			Exemple de Handshake:
			server: PIXELWAR 1.0
			client: REMOTE 1.0
			server: WELCOME {id}
		*/
		// On envoie PIXELWAR 1.0 via le Framer (texte pur)
		await tcpMessageFramer.SendAsync("PIXELWAR 1.0");

		string? clientVersion = await tcpMessageFramer.ReceiveAsync();
		if (clientVersion == null) return false;
		if (clientVersion != "REMOTE 1.0") return false;

		// On valide la connexion via le Framer (texte pur)
		await tcpMessageFramer.SendAsync($"WELCOME {id}");

		// On a réussi notre handshake à partir d'ici !!
		return true;
	}

	public bool IsConnected => tcpClient.Connected && !_disposed;

	/// <summary>
	/// Graceful shutdown
	/// </summary>
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
		
		// CORRECTION : On convertit les octets en texte via UTF-8 (remplace l'ancien .Stringify())
		string textMessage = Encoding.UTF8.GetString(serialized);
		await tcpMessageFramer.SendAsync(textMessage);
	}

	/// <summary>
	/// Reçoit un paquet du client
	/// </summary>
	public async Task<Packet?> ReceiveAsync() {
		String message = await tcpMessageFramer.ReceiveAsync();
		if (message == null) return null;
		
		// On reconvertit le texte en octets via UTF-8 pour désérialiser le Packet
		Packet p = Packet.Deserialize(Encoding.UTF8.GetBytes(message));
		return p;
	}
}