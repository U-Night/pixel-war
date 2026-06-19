using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WaitingRoom : Control {
	MainServer mainServer;

	// ════════════════════════════════════════════════════════════════
	// ✅ Références vers les 4 panels d'équipe (déjà présents dans la scène)
	// Typés "Control" plutôt que "Panel" pour rester compatible peu importe
	// si c'est un Panel ou un ColorRect dans le .tscn
	// ════════════════════════════════════════════════════════════════
	private Control panelBlue, panelRed, panelGreen, panelYellow;

	// Labels créés dynamiquement (un par équipe) pour afficher les ids
	private Label[] teamLabels = new Label[4]; // index = TeamId (0=Blue, 1=Red, 2=Green, 3=Yellow)

	private static readonly string[] TeamNames = { "Équipe Bleue", "Équipe Rouge", "Équipe Verte", "Équipe Jaune" };

	// Refresh périodique (pas besoin d'un Timer dans la scène, on accumule le delta)
	private double _refreshTimer = 0.0;
	private const double REFRESH_INTERVAL = 0.5; // toutes les 500ms

	public override async void _Ready() {
		GD.Print("[INFO][WaitingRoom] Waiting Room loaded. Starting Main Server...");
		mainServer = GetNode<MainServer>("/root/MainServer");

		await Task.Run(() => mainServer.StartServer());

		// ════════════════════════════════════════════════════════════════
		// ✅ Récupération des panels existants + création des labels d'ids
		// ════════════════════════════════════════════════════════════════
		panelBlue = GetNode<Control>("FlowContainer/PanelBlue");
		panelRed = GetNode<Control>("FlowContainer/PanelRed");
		panelGreen = GetNode<Control>("FlowContainer/PanelGreen");
		panelYellow = GetNode<Control>("FlowContainer/PanelYellow");

		teamLabels[0] = CreateTeamLabel(panelBlue, TeamNames[0]);
		teamLabels[1] = CreateTeamLabel(panelRed, TeamNames[1]);
		teamLabels[2] = CreateTeamLabel(panelGreen, TeamNames[2]);
		teamLabels[3] = CreateTeamLabel(panelYellow, TeamNames[3]);
	}

	// ════════════════════════════════════════════════════════════════
	// ✅ Crée un Label en enfant du panel donné, pour afficher le nom
	// de l'équipe + la liste des ids connectés
	// ════════════════════════════════════════════════════════════════
	private Label CreateTeamLabel(Control parent, string teamName) {
		Label label = new Label();
		label.Name = "PlayersLabel";
		label.Text = $"{teamName}\n(en attente de joueurs...)";

		// Le label prend tout l'espace du panel
		label.SetAnchorsPreset(LayoutPreset.FullRect);
		label.OffsetLeft = 10;
		label.OffsetTop = 10;
		label.OffsetRight = -10;
		label.OffsetBottom = -10;

		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Top;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.AddThemeColorOverride("font_color", Colors.White);

		parent.AddChild(label);
		return label;
	}

	// ════════════════════════════════════════════════════════════════
	// ✅ Refresh périodique (pas à chaque frame pour éviter de spammer
	// la mise à jour de texte inutilement)
	// ════════════════════════════════════════════════════════════════
	public override void _Process(double delta) {
		if (mainServer == null) return;

		_refreshTimer += delta;
		if (_refreshTimer < REFRESH_INTERVAL) return;
		_refreshTimer = 0.0;

		RefreshTeamLabels();
	}

	// ════════════════════════════════════════════════════════════════
	// ✅ Regroupe les clients connectés par TeamId et met à jour les labels
	// ════════════════════════════════════════════════════════════════
	private void RefreshTeamLabels() {
		// Une liste d'ids par équipe (index 0-3)
		var idsByTeam = new List<uint>[4];
		for (int i = 0; i < 4; i++) idsByTeam[i] = new List<uint>();

		// mainServer._clients est un ConcurrentDictionary, donc safe à lire
		// même si le serveur ajoute/retire des clients en parallèle
		foreach (GameClient client in mainServer._clients.Values) {
			if (client.TeamId >= 0 && client.TeamId < 4)
				idsByTeam[client.TeamId].Add(client.GetId());
		}

		for (int i = 0; i < 4; i++) {
			if (teamLabels[i] == null) continue;

			string idsText = idsByTeam[i].Count > 0
				? "IDs: " + string.Join(", ", idsByTeam[i])
				: "(en attente de joueurs...)";

			teamLabels[i].Text = $"{TeamNames[i]}\n{idsText}";
		}
	}

	public void _OnContinueButtonPressed() {
		GD.Print("Ok mec");
		if (mainServer._clients.IsEmpty) return;

		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
	}
}