using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node2D {
	MainServer mainServer;
	
	[Export]
	private PackedScene playerScene;

	private Node YellowTeam;
	private Node BlueTeam;
	private Node GreenTeam;
	private Node RedTeam;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready() {
		GD.Print("[INFO][WaitingRoom] Waiting Room loaded. Starting Main Server...");
		// Récupérer l'instance globale (Autoload) de MainServer
		mainServer = GetNode<MainServer>("/root/MainServer");
		
		YellowTeam = GetNode<Node>("Teams/Yellow");
		BlueTeam = GetNode<Node>("Teams/Blue");
		GreenTeam = GetNode<Node>("Teams/Green");
		RedTeam = GetNode<Node>("Teams/Red");

		if (mainServer == null) {
			GD.PrintErr("[EMERG][MainScene] Aucune instance de MainServer trouvée !!");
			GetTree().Quit();
			return;
		}
		
		foreach (KeyValuePair<uint, GameClient> cli in mainServer._clients) {
			PlayerCharacter ply = playerScene.Instantiate<PlayerCharacter>();
			ply.Init(cli.Value, PlayerCharacter.TEAMS.BLUE);

			ply.GlobalPosition = new(200.0f, 100.0f);
			
			BlueTeam.AddChild(ply);
		}
		
	}
}
