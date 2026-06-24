using Godot;
using System;
using System.Collections.Generic;

public partial class MusicManager : Node {

	private AudioStreamPlayer audioPlayer;
	private Dictionary<string, AudioStream> musicTracks = new() {
		{"main_menu", GD.Load<AudioStream>("res://audio/music/mj_human_nature.mp3") },
		{"waiting_room", GD.Load<AudioStream>("res://audio/music/waiting_room.ogg") },
		{"gameplay", GD.Load<AudioStream>("res://assets/sounds/gameplay.ogg") },
	};
	
	public void _Ready() {
		// Get the AudioStreamPlayer node
		audioPlayer = GetNode<AudioStreamPlayer>("bgMusicPlayer");

		// Play the music
		audioPlayer.Play();
	}

	public void PlayMusic(string trackName) {
		if (musicTracks.TryGetValue(trackName, out AudioStream music)) {
			audioPlayer.Stream = music;
			audioPlayer.Play();
		}
	}
}
