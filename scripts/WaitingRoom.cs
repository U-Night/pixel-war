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

    public void _OnContinueButtonPressed() {
        // Si on n'a pas de clients, on annule, il faudrait idéalement attendre 4 clients.
        GD.Print("Ok mec");
        if (mainServer._clients.IsEmpty) return;
        
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }
}