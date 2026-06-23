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

    // ------------------------------------------------------------------------------------------------
    // CONFIGURATION DES POWERUPS
    // ------------------------------------------------------------------------------------------------
    [Export] public bool EnableSwordPowerup = false;         // Activer ou désactiver l'épée
    [Export] public float PowerupSpawnInterval = 20.0f;      // Temps en secondes entre chaque apparition d'un bonus
    [Export] public int PowerupsPerSpawn = 1;                // Nombre de bonus apparaissant en même temps
    [Export] public int MaxActivePowerups = 5;               // Nombre maximum de bonus autorisés simultanément sur la carte
    
    private List<ActivePowerup> _activePowerups = new();
    private double _powerupSpawnTimer = 0.0;
    private Random _random = new Random();

    // Classe interne pour stocker les informations du bonus affiché sur la carte
    private class ActivePowerup {
        public Sprite2D Node;
        public PowerupType Type;
    }

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

        // ------------------------------------------------------------------------------------------------
        // GESTION DE L'APPARITION ET DU RAMASSAGE DES POWERUPS
        // ------------------------------------------------------------------------------------------------
        _powerupSpawnTimer += delta;
        if (_powerupSpawnTimer >= PowerupSpawnInterval) { 
            _powerupSpawnTimer = 0.0;
            // On génère la quantité demandée, tant qu'on ne dépasse pas la limite maximale sur la carte
            for (int i = 0; i < PowerupsPerSpawn; i++) {
                if (_activePowerups.Count < MaxActivePowerups) {
                    SpawnRandomPowerup();
                }
            }
        }
        
        CheckPowerupCollisions();
    }

    private void SpawnRandomPowerup() {
        if (_arenaGrid == null) return;
        
        int x = _random.Next(_arenaGrid.Offset, _arenaGrid.mapWidth);
        int y = _random.Next(0, _arenaGrid.mapHeight);
        var tileSize = _arenaGrid.TileSet.TileSize;
        Vector2 spawnPos = new Vector2(x * tileSize.X + tileSize.X / 2f, y * tileSize.Y + tileSize.Y / 2f);
        
        List<PowerupType> available = new List<PowerupType> { PowerupType.Grow, PowerupType.Speed, PowerupType.PaintBomb };
        if (EnableSwordPowerup) available.Add(PowerupType.Sword);
        
        PowerupType selected = available[_random.Next(available.Count)];
        
        Sprite2D sprite = new Sprite2D();
        string texPath = selected switch {
            PowerupType.Grow => "res://assets/sprites/powerup_grow.svg",
            PowerupType.Speed => "res://assets/sprites/powerup_speed.svg",
            PowerupType.PaintBomb => "res://assets/sprites/powerup_paint_bomb.svg",
            PowerupType.Sword => "res://assets/sprites/powerup_sword.svg",
            _ => ""
        };
        sprite.Texture = GD.Load<Texture2D>(texPath);
        sprite.GlobalPosition = spawnPos;
        
        // On augmente la taille visuelle de l'icône (1.5x) pour qu'elle soit bien visible sur la carte
        sprite.Scale = new Vector2(1.5f, 1.5f);
        
        GetTree().CurrentScene.AddChild(sprite);
        
        _activePowerups.Add(new ActivePowerup { Node = sprite, Type = selected });
    }
    
    private void CheckPowerupCollisions() {
        var players = GetTree().GetNodesInGroup("Players");
        
        for (int i = _activePowerups.Count - 1; i >= 0; i--) {
            var powerup = _activePowerups[i];
            foreach (Node pNode in players) {
                if (pNode is PlayerCharacter player) {
                    // On vérifie la distance. 50 pixels représente environ une seule case (64x64).
                    // Cela garantit que la hitbox reste précise et cantonnée à une case malgré la grande icône.
                    if (player.GlobalPosition.DistanceTo(powerup.Node.GlobalPosition) < 50.0f) {
                        CollectPowerup(player, powerup);
                        _activePowerups.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
    
    private async void CollectPowerup(PlayerCharacter player, ActivePowerup powerup) {
        powerup.Node.QueueFree();
        
        string typeStr = powerup.Type switch {
            PowerupType.Grow => "grow",
            PowerupType.Speed => "speed",
            PowerupType.PaintBomb => "paint_bomb",
            PowerupType.Sword => "sword",
            _ => "unknown"
        };
        
        if (player.GameClient != null && player.GameClient.HeldPowerup == PowerupType.None) {
            player.GameClient.HeldPowerup = powerup.Type;
            await player.GameClient.SendPacketAsync(PacketType.Powerup, $"{{\"action\":\"grant\",\"powerup\":\"{typeStr}\"}}");
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
