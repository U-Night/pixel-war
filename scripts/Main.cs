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
	
	private ArenaGrid arenaGrid;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready() {
		GD.Print("[INFO][WaitingRoom] Waiting Room loaded. Starting Main Server...");
		// Récupérer l'instance globale (Autoload) de MainServer
		mainServer = GetNode<MainServer>("/root/MainServer");
		
		YellowTeam = GetNode<Node>("Teams/Yellow");
		BlueTeam = GetNode<Node>("Teams/Blue");
		GreenTeam = GetNode<Node>("Teams/Green");
		RedTeam = GetNode<Node>("Teams/Red");
		arenaGrid = GetNode<ArenaGrid>("ArenaGrid");

		if (mainServer == null) {
			GD.PrintErr("[EMERG][MainScene] Aucune instance de MainServer trouvée !!");
			GetTree().Quit();
			return;
		}
		
		foreach (KeyValuePair<uint, GameClient> cli in mainServer._clients) {
			PlayerCharacter ply = playerScene.Instantiate<PlayerCharacter>();
			ply.Init(cli.Value, (PlayerCharacter.TEAMS) cli.Value.TeamId, arenaGrid);
			
			// Placer le joueur dans le bon nœud d'équipe (Bleu par défaut dans ton code, on peut l'améliorer)
			switch ((PlayerCharacter.TEAMS)cli.Value.TeamId) {
				case PlayerCharacter.TEAMS.BLUE: BlueTeam.AddChild(ply); break;
				case PlayerCharacter.TEAMS.RED: RedTeam.AddChild(ply); break;
				case PlayerCharacter.TEAMS.GREEN: GreenTeam.AddChild(ply); break;
				case PlayerCharacter.TEAMS.YELLOW: YellowTeam.AddChild(ply); break;
				default: BlueTeam.AddChild(ply); break; // Fallback
			}
		}

		// ═══ Timer en bas à gauche
		// On utilise un CanvasLayer pour que le timer soit toujours visible
		// indépendamment de la caméra 2D
		CanvasLayer timerLayer = new CanvasLayer();
		timerLayer.Layer = 10; // Au-dessus de tout
		AddChild(timerLayer);

		// Conteneur plein écran pour positionner avec les ancres
		Control timerContainer = new Control();
		timerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		timerLayer.AddChild(timerContainer);

		Label timerLabel = new Label();
		timerLabel.Text = "5:00";
		FontFile dsegFont = GD.Load<FontFile>("res://assets/fonts/DSEG7Classic-Regular.ttf");
		timerLabel.AddThemeFontOverride("font", dsegFont);
		timerLabel.AddThemeFontSizeOverride("font_size", 56);
		timerLabel.AddThemeColorOverride("font_color", Color.Color8(180, 30, 40, 255));
		timerLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.7f));
		timerLabel.AddThemeConstantOverride("shadow_offset_x", 3);
		timerLabel.AddThemeConstantOverride("shadow_offset_y", 3);
		timerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		timerLabel.VerticalAlignment = VerticalAlignment.Center;

		timerLabel.AnchorLeft = 0f;
		timerLabel.AnchorTop = 1f;
		timerLabel.AnchorRight = 0f;
		timerLabel.AnchorBottom = 1f;
		timerLabel.OffsetLeft = 0;
		timerLabel.OffsetTop = -110;
		timerLabel.OffsetRight = 160;
		timerLabel.OffsetBottom = -20;

		timerContainer.AddChild(timerLabel);

		// Initialisation du GameManager
		GameManager gameManager = new GameManager();
		gameManager.Name = "GameManager";
		AddChild(gameManager);
		gameManager.Init(mainServer, arenaGrid, timerLabel);

		// Gestion de la musique:
		MusicManager musicManager = GetNode<MusicManager>("/root/MusicManager");
		musicManager.PlayMusic("waiting_room");
	}
}
