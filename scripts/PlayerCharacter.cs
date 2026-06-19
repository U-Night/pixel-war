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

	private TEAMS team;
	private uint id;
	private Sprite2D sprite;
	private GameClient _gameClient;

	private ArenaGrid _arenaGrid;


	public void Init(GameClient gameClient, TEAMS team, ArenaGrid arenaGrid) {
		_gameClient = gameClient;
		this.id = gameClient.GetId();
		this.team = team;
		
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
		
		float currentDx = _gameClient.dx;
		float currentDy = _gameClient.dy;

		// Si on n'a rien reçu depuis plus de 15 secondes, on force les inputs locaux à 0.
		/* if ((currentTime - _gameClient.lastSeen).TotalMilliseconds > 15_000) {
			currentDx = 0;
			currentDy = 0;
		} */
		

		// 2. LECTURE ET LISSAGE DU MOUVEMENT
		Vector2 targetVelocity = new Vector2(currentDx, currentDy) * Speed;
		
		// Le Lerp sur la vitesse donne de l'inertie au vaisseau (gameplay)
		Velocity = Velocity.Lerp(targetVelocity, 0.5f);
		
		// MoveAndSlide applique Velocity en multipliant par delta et gère les murs !
		MoveAndSlide();

		// 3. ROTATION
		// On utilise Mathf.Abs pour ignorer le bruit du stick analogique (deadzone)
		if (Mathf.Abs(currentDx) > Deadzone || Mathf.Abs(currentDy) > Deadzone) {
			// Atan2 calcule l'angle parfait en gérant les divisions par 0 en interne
			sprite.Rotation = Mathf.Atan2(currentDy, currentDx) + (float) Math.PI / 2;
		}

		if (!_gameClient.IsConnected) {
			GetParent().RemoveChild(this);
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
		
		/// On colorie la map de la couleur de l'équipe
		_arenaGrid.PaintTile(GlobalPosition, team);
	}
	
}