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

	private Dictionary<string, AudioStream> soundEffects = new() {
		{"button_hover", GD.Load<AudioStream>("res://assets/sounds/ui/button_hover.wav") },
		{"button_click", GD.Load<AudioStream>("res://assets/sounds/ui/button_click.wav") },
		{"player_joined", GD.Load<AudioStream>("res://assets/sounds/ui/player_joined.wav") },
		{"player_left", GD.Load<AudioStream>("res://assets/sounds/ui/player_left.wav") },
		{"unable_to_perform_action", GD.Load<AudioStream>("res://assets/sounds/ui/unable_to_perform_action.wav") }
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

	public void PlaySfx_NoInterrupt(string sfxName) {
		if (soundEffects.TryGetValue(sfxName, out AudioStream sfx)) {
			AudioStreamPlayer sfxPlayer = new AudioStreamPlayer();
			sfxPlayer.Stream = sfx;
			AddChild(sfxPlayer);
			sfxPlayer.Play();
			sfxPlayer.Connect("finished", Callable.From(() => {
			sfxPlayer.QueueFree();
			}));
		}
	}
}
