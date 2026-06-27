using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public partial class WaitingRoom : Control {
	MainServer mainServer;

	private Control panelBlue, panelRed, panelGreen, panelYellow;
	private Label _dataLabel;
	private string _localIp;

	private Label[] teamLabels = new Label[4]; // index = TeamId (0=Blue, 1=Red, 2=Green, 3=Yellow)

	private static readonly string[] TeamNames = { "Équipe Bleue", "Équipe Rouge", "Équipe Verte", "Équipe Jaune" };

	private double _refreshTimer = 0.0;
	private const double REFRESH_INTERVAL = 0.5; // toutes les 500ms

	private MusicManager musicManager;
	private int previousClientsCount_SfxCheck;

	public override async void _Ready() {
		GD.Print("[INFO][WaitingRoom] Waiting Room loaded. Starting Main Server...");
		mainServer = GetNode<MainServer>("/root/MainServer");

		await Task.Run(() => mainServer.StartServer());
		previousClientsCount_SfxCheck = mainServer._clients.Count;

		_dataLabel = GetNode<Label>("DataLabel");
		_localIp = GetLocalIpAddress();

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

		musicManager = GetNode<MusicManager>("/root/MusicManager");
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
		RefreshDataLabel();
		PlayPlayerJoinedSfx();
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

	private void RefreshDataLabel() {

		if (_dataLabel == null) return;
		_dataLabel.Text = $"Joueurs connectés : {mainServer._clients.Count} | Adresse : {_localIp}";
	}

	private void PlayPlayerJoinedSfx() {
		if (mainServer._clients.Count > previousClientsCount_SfxCheck) {
			musicManager.PlaySfx_NoInterrupt("player_joined");
		}
		if (mainServer._clients.Count < previousClientsCount_SfxCheck) {
			musicManager.PlaySfx_NoInterrupt("player_left");
		}
		previousClientsCount_SfxCheck = mainServer._clients.Count;
	}

	private static string GetLocalIpAddress() {
		try {
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.Connect("8.8.8.8", 80);
			if (socket.LocalEndPoint is IPEndPoint endPoint)
				return endPoint.Address.ToString();
		} catch { }
		return "127.0.0.1";
	}

	public void _OnContinueButtonMouseEntered() {
		musicManager.PlaySfx_NoInterrupt("button_hover");
	}

	public void _OnContinueButtonPressed() {
		if (mainServer._clients.IsEmpty) {
			musicManager.PlaySfx_NoInterrupt("unable_to_perform_action");
			return;
		}

		musicManager.PlaySfx_NoInterrupt("button_click");
		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
	}
}
