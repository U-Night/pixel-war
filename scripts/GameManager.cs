using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node {
    private MainServer _mainServer;
    private ArenaGrid _arenaGrid;
    private Label _timerLabel;

    private double _timeElapsed = 0.0;
    
    private bool _elimination1Done = false;
    private bool _elimination2Done = false;
    private bool _gameEnded = false;

    public void Init(MainServer mainServer, ArenaGrid arenaGrid, Label timerLabel) {
        _mainServer = mainServer;
        _arenaGrid = arenaGrid;
        _timerLabel = timerLabel;
    }

    public override void _Process(double delta) {
        if (_gameEnded || _mainServer == null || _arenaGrid == null) return;

        _timeElapsed += delta;
        UpdateTimerUI();

        if (_timeElapsed >= 90.0 && !_elimination1Done) {
            _elimination1Done = true;
            EliminateLowestTeams();
        } else if (_timeElapsed >= 180.0 && !_elimination2Done) {
            _elimination2Done = true;
            EliminateLowestTeams();
        } else if (_timeElapsed >= 300.0 && !_gameEnded) {
            _gameEnded = true;
            UpdateTimerUI(0); // Force 00:00
            EndGame();
        }
    }

    private void UpdateTimerUI(int? forceTime = null) {
        if (_timerLabel != null) {
            int timeRemaining = forceTime.HasValue ? forceTime.Value : (int)Math.Max(0, 300.0 - _timeElapsed);
            int minutes = timeRemaining / 60;
            int seconds = timeRemaining % 60;
            _timerLabel.Text = $"{minutes}:{seconds:D2}";
        }
    }

    private void EliminateLowestTeams() {
        // Find active teams
        List<int> activeTeams = new List<int>();
        for (int i = 0; i < 4; i++) {
            if (!_arenaGrid.eliminatedTeams[i]) activeTeams.Add(i);
        }

        if (activeTeams.Count <= 1) return; // Should not happen, but just in case

        // Calculate scores
        var scores = new Dictionary<int, int>();
        foreach (int team in activeTeams) {
            scores[team] = _arenaGrid.teamScores[team];
        }

        // Find the lowest score
        int minScore = scores.Values.Min();
        var teamsToElim = scores.Where(kvp => kvp.Value == minScore).Select(kvp => kvp.Key).ToList();

        // Ne faire l'élimination QUE si ça laisse au moins 2 équipes
        if (activeTeams.Count - teamsToElim.Count < 2) {
            GD.Print("[GameManager] Élimination ignorée : laisserait moins de 2 équipes (égalité).");
            return;
        }

        // On élimine les équipes, toujours avec un packet "eliminated" (pas de draw avant la fin)
        foreach (int team in teamsToElim) {
            _arenaGrid.MarkTeamEliminated(team);
            NotifyTeam(team, "eliminated");
        }
    }

    private void EndGame() {
        List<int> activeTeams = new List<int>();
        for (int i = 0; i < 4; i++) {
            if (!_arenaGrid.eliminatedTeams[i]) activeTeams.Add(i);
        }

        if (activeTeams.Count == 0) return;

        var scores = new Dictionary<int, int>();
        foreach (int team in activeTeams) {
            scores[team] = _arenaGrid.teamScores[team];
        }

        int maxScore = scores.Values.Max();
        var winningTeams = scores.Where(kvp => kvp.Value == maxScore).Select(kvp => kvp.Key).ToList();
        var losingTeams = scores.Where(kvp => kvp.Value < maxScore).Select(kvp => kvp.Key).ToList();

        if (winningTeams.Count > 1) {
            // It's a draw among winners
            foreach (int team in winningTeams) {
                NotifyTeam(team, "draw");
            }
        } else {
            NotifyTeam(winningTeams[0], "victory");
        }

        foreach (int team in losingTeams) {
            NotifyTeam(team, "eliminated");
        }

        // Affichage de l'écran de fin côté Serveur via CanvasLayer
        // (le CanvasLayer garantit l'affichage au-dessus de la Camera2D)
        PackedScene gameOverScene = GD.Load<PackedScene>("res://scenes/game_over.tscn");
        if (gameOverScene != null) {
            CanvasLayer overlay = new CanvasLayer();
            overlay.Layer = 100;
            GetTree().CurrentScene.AddChild(overlay);

            Node gameOverNode = gameOverScene.Instantiate();
            overlay.AddChild(gameOverNode);
            if (gameOverNode is GameOver gameOverScript) {
                gameOverScript.Init(_arenaGrid, winningTeams);
            }
        } else {
            GD.PrintErr("[GameManager] Impossible de charger res://scenes/game_over.tscn");
        }
    }

    private async void NotifyTeam(int teamId, string type) {
        string message = $"{{\"type\":\"{type}\"}}";
        foreach (var client in _mainServer._clients.Values) {
            if (client.TeamId == teamId) {
                client.IsEliminated = true; // Set flag so character handles QueueFree
                await client.SendPacketAsync(PacketType.Message, message);
            }
        }
    }
}
