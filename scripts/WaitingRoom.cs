using Godot;
using System;
using System.Threading.Tasks;

public partial class WaitingRoom : Control {
	MainServer mainServer;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready() {
		GD.Print("[INFO][WaitingRoom] Waiting Room loaded. Starting Main Server...");
		// Récupérer l'instance globale (Autoload) de MainServer
		mainServer = GetNode<MainServer>("/root/MainServer");
		
		await Task.Run(() => mainServer.StartServer());
	}
}
