using Godot;
using System;

public partial class MainMenu : Control {
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Gestion de la musique:
		MusicManager musicManager = GetNode<MusicManager>("/root/MusicManager");
		musicManager.PlayMusic("waiting_room");

		// On récupère le numéro de version
		VersionManager versionManager = GetNode<VersionManager>("/root/VersionManager");
		Label versionLabel = GetNode<Label>("MarginContainer/HBoxContainer/VersionLabel");
		versionLabel.Text = "Pixel War " + versionManager.FullVersion + ". © " + versionManager.YearOfLicenseValidity + " U-Night & Gaya BOUNDER. All rights reserved.";
	}

	public void _on_play_button_pressed() {
		GetTree().ChangeSceneToFile("res://scenes/waiting_room.tscn");
	}

	public void _on_close_button_pressed() {
		GetTree().Quit();
	}
}
