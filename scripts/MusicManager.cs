using Godot;
using System;
using System.Collections.Generic;

public partial class MusicManager : Node {

	private AudioStreamPlayer audioPlayer;
	private Dictionary<string, AudioStream> musicTracks = new() {
		{"main_menu", GD.Load<AudioStream>("res://assets/sounds/mj_human_nature.mp3") },
		{"waiting_room", GD.Load<AudioStream>("res://assets/sounds/waiting_room.ogg") },
		{"color_splash_battle", GD.Load<AudioStream>("res://assets/sounds/color_splash_battle.ogg") },
		{"results", GD.Load<AudioStream>("res://assets/sounds/results.ogg") },
	};
	
	public override void _Ready() {
		// Get the AudioStreamPlayer node
		audioPlayer = GetNode<AudioStreamPlayer>("bgMusicPlayer");

		// Play the music
		audioPlayer.Play();
	}

	public void PlayMusic(string trackName) {
		if (musicTracks.TryGetValue(trackName, out AudioStream music)) {
			GD.Print($"[INFO][MusicManager] Playing music track: {trackName}");
			audioPlayer.Stop();
			audioPlayer.Stream = music;
			audioPlayer.Play();
		}
	}
}
