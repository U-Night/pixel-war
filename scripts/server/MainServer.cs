using Godot;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public partial class MainServer : Node {
	private TcpListener listener;
	private IPAddress _address;
	private ushort _port;
	private bool started = true; // enable graceful shutdown*
	private int counter = 1;
	private readonly ConcurrentDictionary<string, GameClient> _clients = new();

	public MainServer() {}
	
	public void StartServer(){
		GD.Print("[NOTICE][MainServer] Starting Game Server on all interfaces, port 6967");
		StartAsync(IPAddress.Any, 6967).ContinueWith(task => {
			if (task.IsFaulted) {
				GD.PrintErr($"[ERROR][MainServer] Failed to start server: {task.Exception}");
			}
		});
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
			_ = this.HandleClientAsync(_client, counter); /// Multiplexage: Ajouter un client et le placer dans un thread
			counter++;
		}
	}

	private async Task HandleClientAsync(TcpClient client, int id) {
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
			_clients.TryAdd(id.ToString(), gameClient);
			GD.Print($"[INFO][MainServer] Un client s'est connecété id: {id}");

			while (gameClient.IsConnected) {
				Packet? packet = await gameClient.ReceiveAsync();
				if (packet == null) break;

				await HandlePacketAsync(gameClient, packet);
			}
		} catch (ProtocolViolationException ex) {
			GD.PrintErr($"[ERROR][MainServer] Handshake failed: {ex.Message}");
			client.Close();
		}
	}

	private async Task HandlePacketAsync(GameClient sender, Packet packet) {
		GD.Print("[INFO][MainServer] Paquet reçu :", packet);
		GD.Print($"[INFO][MainServer]{sender.GetId()} {packet.GetDataAsString()}");
	}
}
