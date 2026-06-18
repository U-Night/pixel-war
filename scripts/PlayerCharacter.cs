using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerCharacter : CharacterBody2D  {
	public enum TEAMS {
		GREEN,
		RED,
		YELLOW,
		BLUE
	}

	[Export(PropertyHint.Range, "0,1000,10")]
	private float Speed = 500.00f;

	private static Dictionary<TEAMS, string> TEAMS_SPRITE = new() {
		{TEAMS.BLUE, "uid://cex2sjl5evbdq"},
		{TEAMS.GREEN, "uid://buvb5n1xwc22s"},
		{TEAMS.YELLOW, "uid://0dqnhwad5chk"},
		{TEAMS.RED, "uid://b1uk6jt5yir8w"},
	};

	private TEAMS team;
	private uint id;
	private Sprite2D sprite;
	private GameClient _gameClient;


	public void Init(GameClient gameClient, TEAMS team) {
		_gameClient = gameClient;
		this.id = gameClient.GetId();
		this.team = team;
		
		Texture2D charText = ResourceLoader.Load<Texture2D>(TEAMS_SPRITE.GetValueOrDefault(TEAMS.BLUE));
		sprite = GetNode<Sprite2D>("CharacterSprite");
		sprite.Texture = charText;
		Scale = new Vector2(2.5f, 2.5f);
		
		GetNode<Label>("UserId").Text = id.ToString();
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
	}
	
}
