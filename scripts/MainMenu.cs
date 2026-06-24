using Godot;
using System;

public partial class MainMenu : Control {
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Gestion de la musique:
		MusicManager musicManager = GetNode<MusicManager>("/root/MusicManager");
		musicManager.PlayMusic("waiting_room");
	}

	public void _on_play_button_pressed() {
		GetTree().ChangeSceneToFile("res://scenes/waiting_room.tscn");
	}

	public void _on_close_button_pressed() {
		GetTree().Quit();
	}
}
