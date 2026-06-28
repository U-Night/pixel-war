using Godot;
using System;
using System.Collections.Generic;

public partial class GameOver : Control {
	private ArenaGrid _arenaGrid;
	private List<int> _winningTeams;
	private MusicManager musicManager;

	private static readonly Dictionary<int, string> TeamNames = new() {
		{ 0, "Bleue" },
		{ 1, "Rouge" },
		{ 2, "Verte" },
		{ 3, "Jaune" }
	};
	
	private static readonly Dictionary<int, Color> TeamColors = new() {
		{ 0, new Color(0.2f, 0.4f, 1f) },
		{ 1, new Color(1f, 0.2f, 0.2f) },
		{ 2, new Color(0.2f, 1f, 0.2f) },
		{ 3, new Color(1f, 1f, 0.2f) }
	};

	public async void Init(ArenaGrid arenaGrid, List<int> winningTeams) {
		_arenaGrid = arenaGrid;
		_winningTeams = winningTeams;

		// ═══ On s'assure que CE Control prend tout l'écran ═══
		SetAnchorsPreset(LayoutPreset.FullRect);

		// ═══ Fond noir quasi-opaque (à peine transparent) ═══
		ColorRect bg = new ColorRect();
		bg.Color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		// ═══ CenterContainer pour centrer le contenu peu importe la résolution ═══
		CenterContainer center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		VBoxContainer vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 40);
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		center.AddChild(vbox);

		// ═══ Titre ═══
		Label title = new Label();
		title.AddThemeFontSizeOverride("font_size", 120);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		
		if (winningTeams.Count > 1) {
			// Égalité entre plusieurs équipes
			List<string> names = new List<string>();
			foreach (int t in winningTeams) names.Add($"Équipe {TeamNames[t]}");
			title.Text = "ÉGALITÉ !";
			title.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			vbox.AddChild(title);

			Label subtitle = new Label();
			subtitle.Text = string.Join(" & ", names);
			subtitle.AddThemeFontSizeOverride("font_size", 64);
			subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			subtitle.HorizontalAlignment = HorizontalAlignment.Center;
			vbox.AddChild(subtitle);
		} else {
			title.Text = $"VICTOIRE DE L'ÉQUIPE {TeamNames[winningTeams[0]].ToUpper()} !";
			title.AddThemeColorOverride("font_color", TeamColors[winningTeams[0]]);
			vbox.AddChild(title);
		}

		// ═══ Séparateur visuel ═══
		HSeparator sep = new HSeparator();
		sep.AddThemeConstantOverride("separation", 20);
		vbox.AddChild(sep);

		// ═══ Tableau de statistiques ═══
		GridContainer grid = new GridContainer();
		grid.Columns = 4;
		grid.AddThemeConstantOverride("h_separation", 60);
		grid.AddThemeConstantOverride("v_separation", 20);
		grid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		vbox.AddChild(grid);

		// Header
		grid.AddChild(CreateLabel("Équipe", true));
		grid.AddChild(CreateLabel("Score Final", true));
		grid.AddChild(CreateLabel("Score Max", true));
		grid.AddChild(CreateLabel("Cases Peintes", true));

		// Rows pour chaque équipe
		for (int i = 0; i < 4; i++) {
			grid.AddChild(CreateLabel($"Équipe {TeamNames[i]}", false, TeamColors[i]));
			
			float finalPct = _arenaGrid.totalPlayableCells > 0
				? ((float)_arenaGrid.teamScores[i] / _arenaGrid.totalPlayableCells) * 100 : 0;
			grid.AddChild(CreateLabel($"{Mathf.RoundToInt(finalPct)}%", false));
			
			float maxPct = _arenaGrid.totalPlayableCells > 0
				? ((float)_arenaGrid.maxTeamScores[i] / _arenaGrid.totalPlayableCells) * 100 : 0;
			grid.AddChild(CreateLabel($"{Mathf.RoundToInt(maxPct)}%", false));
			
			grid.AddChild(CreateLabel($"{_arenaGrid.cumulativePaintedTiles[i]}", false));
		}

		// ═══ Boutons d'action ═══
		HBoxContainer buttonBox = new HBoxContainer();
		buttonBox.AddThemeConstantOverride("separation", 50);
		buttonBox.Alignment = BoxContainer.AlignmentMode.Center;
		
		// Un margin top pour séparer de la grille
		MarginContainer marginBox = new MarginContainer();
		marginBox.AddThemeConstantOverride("margin_top", 60);
		marginBox.AddChild(buttonBox);
		vbox.AddChild(marginBox);

		Button restartBtn = new Button();
		restartBtn.Text = "Relancer une partie";
		restartBtn.AddThemeFontSizeOverride("font_size", 40);
		restartBtn.Pressed += OnRestartPressed;
		restartBtn.MouseEntered += OnButtonMouseEntered;
		buttonBox.AddChild(restartBtn);

		Button menuBtn = new Button();
		menuBtn.Text = "Menu Principal";
		menuBtn.AddThemeFontSizeOverride("font_size", 40);
		menuBtn.Pressed += OnMainMenuPressed;
		menuBtn.MouseEntered += OnButtonMouseEntered;
		buttonBox.AddChild(menuBtn);

		Button quitBtn = new Button();
		quitBtn.Text = "Quitter";
		quitBtn.AddThemeFontSizeOverride("font_size", 40);
		quitBtn.Pressed += OnQuitPressed;
		quitBtn.MouseEntered += OnButtonMouseEntered;
		buttonBox.AddChild(quitBtn);

		// ═══ Attribution du MusicManager ═══
		musicManager = GetNodeOrNull<MusicManager>("/root/MusicManager");

		// Après 5 secondes de répit, la musique des résultats se joue et les interactions sont activées
		await ToSignal(GetTree().CreateTimer(5.0f), SceneTreeTimer.SignalName.Timeout);
		if (musicManager != null) {
			musicManager.PlayMusic("results");
		}
	}


	private void OnButtonMouseEntered() {
		musicManager.PlaySfx_NoInterrupt("button_hover");
	}

	private void OnRestartPressed() {
		MainServer mainServer = GetNodeOrNull<MainServer>("/root/MainServer");
		if (mainServer != null) {
			mainServer.ResetServer();
		}
		musicManager.PlaySfx_NoInterrupt("button_click");
		GetTree().ChangeSceneToFile("res://scenes/waiting_room.tscn");
	}

	private void OnMainMenuPressed() {
		MainServer mainServer = GetNodeOrNull<MainServer>("/root/MainServer");
		if (mainServer != null) {
			mainServer.ResetServer();
		}
		musicManager.PlaySfx_NoInterrupt("button_click");
		GetTree().ChangeSceneToFile("res://scenes/mainMenu.tscn");
	}

	private void OnQuitPressed() {
		GetTree().Quit();
	}

	private Label CreateLabel(string text, bool isHeader, Color? color = null) {
		Label l = new Label();
		l.Text = text;
		l.AddThemeFontSizeOverride("font_size", isHeader ? 48 : 42);
		if (isHeader) {
			l.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
		} else if (color.HasValue) {
			l.AddThemeColorOverride("font_color", color.Value);
		}
		l.HorizontalAlignment = HorizontalAlignment.Center;
		return l;
	}
}
