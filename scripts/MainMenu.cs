using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control {
	private MusicManager musicManager;

	public void Init() {
		musicManager = GetNode<MusicManager>("/root/MusicManager");
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Init();
		// Gestion de la musique:
		musicManager.PlayMusic("waiting_room");

		// On récupère le numéro de version
		VersionManager versionManager = GetNode<VersionManager>("/root/VersionManager");
		Label versionLabel = GetNode<Label>("MarginContainer/HBoxContainer/VersionLabel");
		versionLabel.Text = "Pixel War " + versionManager.FullVersion + ". © " + versionManager.YearOfLicenseValidity + " U-Night & Gaya BOUNDER. All rights reserved.";
	}

	public void _on_play_button_pressed() {
		musicManager.PlaySfx_NoInterrupt("button_click");
		GetTree().ChangeSceneToFile("res://scenes/waiting_room.tscn");
	}

	public void _on_close_button_pressed() {
		GetTree().Quit();
	}

	public void _on_play_button_mouse_entered() {
		musicManager.PlaySfx_NoInterrupt("button_hover");
	}

	public void _on_close_button_mouse_entered() {
		musicManager.PlaySfx_NoInterrupt("button_hover");
	}
}
