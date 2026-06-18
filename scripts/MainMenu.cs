using Godot;
using System;

public partial class MainMenu : Control {
	// Called when the node enters the scene tree for the first time.
	public void _on_play_button_pressed() {
		GetTree().ChangeSceneToFile("res://scenes/waiting_room.tscn");
	}

	public void _on_close_button_pressed() {
		GetTree().Quit();
	}
}
