using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerCharacter : CharacterBody2D  {
	public enum TEAMS : int {
		BLUE   = 0,
		RED    = 1,
		GREEN  = 2,
		YELLOW = 3
	}
	
	[Export(PropertyHint.Range, "0,1000,10")]
	private float Speed = 500.00f;

	private static Dictionary<TEAMS, string> TEAMS_SPRITE = new() {
		{TEAMS.BLUE, "uid://cex2sjl5evbdq"},
		{TEAMS.GREEN, "uid://buvb5n1xwc22s"},
		{TEAMS.YELLOW, "uid://0dqnhwad5chk"},
		{TEAMS.RED, "uid://b1uk6jt5yir8w"},
	};

	private static Dictionary<TEAMS, Vector2> SPAWN_LOC = new() {
		{ TEAMS.BLUE, new Vector2(384.0f, 100.0f) },
		{ TEAMS.RED, new Vector2(3760.0f, 100.0f) },
		{ TEAMS.GREEN, new Vector2(384.0f, 2080.0f) },
		{ TEAMS.YELLOW, new Vector2(3760.0f, 2080.0f) },
	};

	public TEAMS team;
	private uint id;
	private Sprite2D sprite;
	public GameClient GameClient { get; private set; }

	private ArenaGrid _arenaGrid;


	public void Init(GameClient gameClient, TEAMS team, ArenaGrid arenaGrid) {
		GameClient = gameClient;
		this.id = gameClient.GetId();
		this.team = team;
		AddToGroup("Players");
		
		Texture2D charText = ResourceLoader.Load<Texture2D>(TEAMS_SPRITE.GetValueOrDefault(team));
		GD.Print($"[DBG] Chargement de la texture {TEAMS_SPRITE.GetValueOrDefault(team)}, {charText?.ResourceName}");
		sprite = GetNode<Sprite2D>("CharacterSprite");
		sprite.Texture = charText;
		Scale = new Vector2(2.5f, 2.5f);

		Vector2 spawn = SPAWN_LOC.GetValueOrDefault(team);

		GlobalPosition = spawn;
		
		GetNode<Label>("UserId").Text = id.ToString();
		_arenaGrid = arenaGrid;
	}
	
	
	// Un seuil minuscule pour éviter les tremblements (jitter) quand on lâche le stick
	private const float Deadzone = 0.05f;

	public override void _PhysicsProcess(double delta) {
		DateTime currentTime = DateTime.Now;
		
		float currentDx = GameClient.dx;
		float currentDy = GameClient.dy;

		// Disable active powerup if expired (except PaintBomb which is instant)
		if (GameClient.ActivePowerup != PowerupType.None && GameClient.ActivePowerup != PowerupType.PaintBomb && DateTime.Now >= GameClient.PowerupEndTime) {
			GameClient.ActivePowerup = PowerupType.None;
		}

		if (GameClient.ActivePowerup == PowerupType.Grow) {
			Scale = new Vector2(5.0f, 5.0f);
		} else {
			Scale = new Vector2(2.5f, 2.5f);
		}

		float currentSpeed = Speed;
		if (GameClient.ActivePowerup == PowerupType.Speed) {
			currentSpeed *= 2.0f; // Boost speed
		}

		// 2. LECTURE ET LISSAGE DU MOUVEMENT
		Vector2 targetVelocity = new Vector2(currentDx, currentDy) * currentSpeed;
		
		// Le Lerp sur la vitesse donne de l'inertie au vaisseau (gameplay)
		Velocity = Velocity.Lerp(targetVelocity, 0.5f);
		
		// MoveAndSlide applique Velocity en multipliant par delta et gère les murs !
		MoveAndSlide();

		// ------------------------------------------------------------------------------------------------
		// GESTION DES POWERUPS ACTIFS
		// ------------------------------------------------------------------------------------------------

		// Logique de la PaintBomb : Explosion de couleur instantanée
		if (GameClient.ActivePowerup == PowerupType.PaintBomb) {
			ExplodePaint();
			GameClient.ActivePowerup = PowerupType.None; // Consommé instantanément
		}

		// Logique de l'Épée (Sword) : Élimination des adversaires au contact
		if (GameClient.ActivePowerup == PowerupType.Sword) {
			var players = GetTree().GetNodesInGroup("Players");
			foreach (Node pNode in players) {
				if (pNode != this && pNode is PlayerCharacter otherPlayer) {
					// Si l'adversaire est très proche (80 pixels)
					if (this.GlobalPosition.DistanceTo(otherPlayer.GlobalPosition) < 80.0f) {
						if (!otherPlayer.GameClient.IsEliminated) {
							otherPlayer.GameClient.IsEliminated = true;
							_arenaGrid.MarkTeamEliminated((int)otherPlayer.team);
							_ = otherPlayer.GameClient.SendPacketAsync(PacketType.Message, "{\"type\":\"eliminated\"}");
						}
					}
				}
			}
		}

		// 3. ROTATION
		// On utilise Mathf.Abs pour ignorer le bruit du stick analogique (deadzone)
		if (Mathf.Abs(currentDx) > Deadzone || Mathf.Abs(currentDy) > Deadzone) {
			// Atan2 calcule l'angle parfait en gérant les divisions par 0 en interne
			sprite.Rotation = Mathf.Atan2(currentDy, currentDx) + (float) Math.PI / 2;
		}

		if (!GameClient.IsConnected) {
			GetParent().RemoveChild(this);
			QueueFree();
			return;
		}

		if (GameClient.IsEliminated) {
			QueueFree();
			return;
		}

		// On empêche le joueur de sortir de la carte
		var mapRect = _arenaGrid.GetUsedRect();
		var tileSize = _arenaGrid.TileSet.TileSize;
		var mapLimitsInPixels = new Rect2(mapRect.Position * tileSize, mapRect.Size * tileSize);
		
		// On prend en compte la taille du sprite pour ne pas qu'il dépasse
		if (sprite.Texture != null) {
			var spriteSize = sprite.Texture.GetSize() * Scale;
			var halfSpriteSize = spriteSize / 2f;

			var minPos = mapLimitsInPixels.Position + halfSpriteSize;
			var maxPos = mapLimitsInPixels.End - halfSpriteSize;

			GlobalPosition = GlobalPosition.Clamp(minPos, maxPos);
		}
		
		// ------------------------------------------------------------------------------------------------
		// GESTION DU DESSIN (COLORIAGE DE LA CARTE)
		// ------------------------------------------------------------------------------------------------
		if (GameClient.ActivePowerup == PowerupType.Grow) {
			// Le powerup GROW permet de colorier une grille de 3x3 autour du vaisseau
			Vector2 tileSizeVec = new Vector2(_arenaGrid.TileSet.TileSize.X, _arenaGrid.TileSet.TileSize.Y);
			for (int x = -1; x <= 1; x++) {
				for (int y = -1; y <= 1; y++) {
					_arenaGrid.PaintTile(GlobalPosition + new Vector2(x * tileSizeVec.X, y * tileSizeVec.Y), team);
				}
			}
		} else {
			// Comportement normal : on colorie uniquement la case sous le vaisseau
			_arenaGrid.PaintTile(GlobalPosition, team);
		}
	}
	
	private void ExplodePaint() {
		// Logique du powerup PaintBomb : colorie environ 25 cases (rayon ~2.5 cases)
		Vector2 tileSizeVec = new Vector2(_arenaGrid.TileSet.TileSize.X, _arenaGrid.TileSet.TileSize.Y);
		// On boucle sur un carré de -2 à +2 (ce qui donne 5x5 = 25 cases au maximum)
		for (int x = -2; x <= 2; x++) {
			for (int y = -2; y <= 2; y++) {
				// Optionnel : on peut arrondir les coins avec (x*x + y*y <= 6), mais pour ~25 blocs,
				// un carré de 5x5 fait exactement 25 blocs. Utilisons le carré complet.
				_arenaGrid.PaintTile(GlobalPosition + new Vector2(x * tileSizeVec.X, y * tileSizeVec.Y), team);
			}
		}
	}
	
}