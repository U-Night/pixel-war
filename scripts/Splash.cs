using Godot;
using System;

public partial class Splash : Control {
	public void OnSplashScreenFinished() {
		GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
	}
}
