using Godot;
using System;

public partial class ArenaGrid : TileMapLayer {
	public int baseTileId { get; private set; } = 1;
	public int mapHeight { get; private set; } = 34;

	public int mapWidth { get; private set; } = 60;
	
	[Export] public ushort Offset;

	// Nos liens directs vers les textes de l'interface
	[Export] public Label ScoreLabelBlue;
	[Export] public Label ScoreLabelGreen;
	[Export] public Label ScoreLabelRed;
	[Export] public Label ScoreLabelYellow;
	private int solidTileId = 2;

	// Pour garder le compte des cases de chaque équipe
	public int[] teamScores { get; private set; } = new int[4];
	public bool[] eliminatedTeams = new bool[4];
	public int totalPlayableCells { get; private set; }
	
	// Statistiques de fin de partie
	public int[] maxTeamScores { get; private set; } = new int[4];
	public int[] cumulativePaintedTiles { get; private set; } = new int[4];

	public ArenaGrid() {
		this.Offset = 5; // Ton offset configuré dans l'éditeur
	}

	public override void _Ready() {
		// On calcule le total de cases où on a le droit de peindre
		totalPlayableCells = (mapWidth - Offset) * mapHeight;

		for (int x = Offset; x < mapWidth; x++) {
			for (int y = 0; y < mapHeight; y++) {
				SetCell(new Vector2I(x, y), baseTileId, new Vector2I(0, 0), 0);
			}
		}

		// On met l'interface à jour (pour afficher 0% au lieu des 25% factices)
		UpdateUIScores();
	}

	public void PaintTile(Vector2 globalPosition, PlayerCharacter.TEAMS teamColor) { 
		if (eliminatedTeams[(int)teamColor]) return;
		Vector2I cellPos = LocalToMap(ToLocal(globalPosition));

		if (cellPos.X >= Offset && cellPos.X < mapWidth && cellPos.Y >= 0 && cellPos.Y < mapHeight) {
			// On regarde ce qu'il y a actuellement sur la case
			int prevSource = GetCellSourceId(cellPos);
			int prevAlt = GetCellAlternativeTile(cellPos);

			// Si la case était déjà peinte par une équipe
			if (prevSource == solidTileId) {
				// Si elle est déjà de la bonne couleur, on ne fait rien !
				if (prevAlt == (int)teamColor) return;

				// Sinon, l'ancienne équipe perd un point
				teamScores[prevAlt]--;
			}

			// On peint la case et la nouvelle équipe gagne un point
			SetCell(cellPos, solidTileId, new Vector2I(0, 0), (int)teamColor);
			teamScores[(int)teamColor]++;
			
			// Statistiques
			cumulativePaintedTiles[(int)teamColor]++;
			if (teamScores[(int)teamColor] > maxTeamScores[(int)teamColor]) {
				maxTeamScores[(int)teamColor] = teamScores[(int)teamColor];
			}

			// On met à jour le texte à l'écran
			UpdateUIScores();
		}
	}

	private void UpdateUIScores() {
		if (totalPlayableCells == 0) return;

		// Le calcul : (score / total) * 100, et on arrondit à l'entier le plus proche
		if (ScoreLabelBlue != null)
			ScoreLabelBlue.Text = Mathf.RoundToInt((float)teamScores[(int)PlayerCharacter.TEAMS.BLUE] / totalPlayableCells * 100) +
								  "%";

		if (ScoreLabelRed != null)
			ScoreLabelRed.Text = Mathf.RoundToInt((float)teamScores[(int)PlayerCharacter.TEAMS.RED] / totalPlayableCells * 100) +
								 "%";

		if (ScoreLabelGreen != null)
			ScoreLabelGreen.Text =
				Mathf.RoundToInt((float)teamScores[(int)PlayerCharacter.TEAMS.GREEN] / totalPlayableCells * 100) + "%";

		if (ScoreLabelYellow != null)
			ScoreLabelYellow.Text =
				Mathf.RoundToInt((float)teamScores[(int)PlayerCharacter.TEAMS.YELLOW] / totalPlayableCells * 100) + "%";
	}

	public void MarkTeamEliminated(int teamId) {
		eliminatedTeams[teamId] = true;
		// Griser l'UI
		Label label = null;
		switch ((PlayerCharacter.TEAMS)teamId) {
			case PlayerCharacter.TEAMS.BLUE: label = ScoreLabelBlue; break;
			case PlayerCharacter.TEAMS.RED: label = ScoreLabelRed; break;
			case PlayerCharacter.TEAMS.GREEN: label = ScoreLabelGreen; break;
			case PlayerCharacter.TEAMS.YELLOW: label = ScoreLabelYellow; break;
		}
		if (label != null) {
			var rect = label.GetParent<ColorRect>();
			if (rect != null) {
				rect.SelfModulate = new Color(0.3f, 0.3f, 0.3f, 1f);
			}
		}
	}

	// (Tu peux remettre ton _Process ici pour tester à la souris si tu le souhaites !)
	public override void _Process(double delta) {
		// Test avec le CLIC GAUCHE : Peindre en Bleu
		if (Input.IsMouseButtonPressed(MouseButton.Left)) {
			Vector2 mousePos = GetGlobalMousePosition();
			PaintTile(mousePos, PlayerCharacter.TEAMS.BLUE);
		}

		// Test avec le CLIC DROIT : Peindre en Rouge
		if (Input.IsMouseButtonPressed(MouseButton.Right)) {
			Vector2 mousePos = GetGlobalMousePosition();
			PaintTile(mousePos, PlayerCharacter.TEAMS.RED);
		}
	}
}
