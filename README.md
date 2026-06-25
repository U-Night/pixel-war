<p align="center">
  <img src="assets/background.png" alt="Pixel War Banner" width="600"/>
</p>

<h1 align="center">🚀 Pixel War</h1>

<p align="center">
  <strong>Un jeu massivement multijoueur colocalisé</strong>
</p>

<p align="center">
  <a href="https://github.com/U-Night/pixe-war/releases/latest"><img src="https://img.shields.io/badge/Télécharger-Dernière%20Release-brightgreen?style=for-the-badge&logo=github" alt="Dernière Release"/></a>
  <img src="https://img.shields.io/badge/Godot-4.6-blue?style=for-the-badge&logo=godotengine" alt="Godot 4.6"/>
  <img src="https://img.shields.io/badge/C%23-.NET%2010-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10"/>
</p>

---

## 📖 Présentation

**Pixel War** est un jeu massivement multijoueur **colocalisé** : tous les joueurs sont physiquement présents dans une même salle (amphithéâtre, salle de cours…) face à un grand écran partagé qui affiche l'unique espace de jeu. Chaque joueur contrôle son vaisseau depuis **son propre téléphone**, qui sert de manette virtuelle.

Ce projet a été réalisé dans le cadre d'un cours à l'**IUT d'Orsay** (Université Paris-Saclay).

> **Concept du cours :** développer un jeu massivement multijoueur colocalisé (pas en ligne, mais avec tous les joueurs physiquement présents devant un grand écran commun). Les contrôles se font via le téléphone des joueurs (manette virtuelle) ou éventuellement par reconnaissance de gestes avec une caméra filmant les joueurs.

---

## 🎮 Règles du jeu

### Principe

4 équipes s'affrontent dans l'espace : **🔵 Bleue**, **🔴 Rouge**, **🟢 Verte** et **🟡 Jaune**. Chaque équipe pilote de petits vaisseaux spatiaux dont l'objectif est de **colorier le maximum de cases** sur la carte aux couleurs de son équipe.

### Déroulement d'une partie (5 minutes)

| ⏱️ Temps | Événement |
|-----------|-----------|
| `0:00` | 🏁 Début de la partie — les 4 équipes s'affrontent |
| `1:30` | ⚔️ **1ère élimination** — l'équipe avec le score le plus faible est éliminée |
| `3:00` | ⚔️ **2ème élimination** — l'équipe avec le score le plus faible (parmi les 3 restantes) est éliminée |
| `5:00` | 🏆 **Fin de partie** — l'équipe en tête parmi les 2 survivantes remporte la victoire |

> **Note :** en cas d'égalité lors d'une élimination, celle-ci est annulée pour préserver au moins 2 équipes en jeu.

### Power-ups

Au cours de la partie, des bonus apparaissent aléatoirement sur la carte. Les joueurs peuvent les ramasser en passant dessus avec leur vaisseau, puis les activer depuis leur manette :

| Power-up | Effet |
|----------|-------|
| 🔷 **Agrandissement** (Grow) | Le vaisseau grossit et colorie une zone de **3×3 cases** au lieu d'une seule |
| ⚡ **Vitesse** (Speed) | La vitesse du vaisseau est **doublée** temporairement |
| 💣 **Bombe de peinture** (Paint Bomb) | Explosion instantanée qui colorie **25 cases** (5×5) autour du vaisseau |
| ⚔️ **Épée** (Sword) | Permet d'**éliminer** les joueurs adverses au contact *(désactivé par défaut)* |

---

## 📱 Comment jouer ?

### 1. Rejoindre la partie

Les joueurs doivent **télécharger la manette virtuelle** sur leur téléphone :

👉 **[pixel-war-remote](https://github.com/U-Night/pixel-war-remote)**

### 2. Se connecter

1. L'hôte lance le jeu sur l'écran partagé (voir [Installation](#-installation))
2. Le serveur de jeu démarre automatiquement sur le **port 6967** (TCP + UDP)
3. Les joueurs ouvrent la manette sur leur téléphone et se connectent à l'adresse IP de l'hôte
4. Chaque joueur est automatiquement assigné à une équipe (répartition en round-robin)

### 3. Contrôles

- **Joystick** : déplacer le vaisseau (envoyé en UDP pour la réactivité)
- **Bouton Power-up** : activer le bonus ramassé
- **Ping** : signaler sa position aux coéquipiers (cooldown de 5 secondes)

---

## 🛠️ Installation

### 🎮 En production (pour jouer)

Téléchargez la dernière version compilée pour votre plateforme depuis les **Releases GitHub** :

👉 **[Dernière Release](https://github.com/U-Night/pixe-war/releases/latest)**

Des builds sont disponibles pour **Windows**, **macOS** et **Linux**.

> **macOS** : le build est signé en ad-hoc. Si macOS bloque l'ouverture, faites un clic droit → Ouvrir.

### 🧑‍💻 En développement

#### Prérequis

- [Godot Engine 4.6.2](https://godotengine.org/download) — **Mono / .NET** (version avec support C#)
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

#### Installation

```bash
# Cloner le dépôt
git clone https://github.com/U-Night/pixe-war.git
cd pixel-war

# Ouvrir le projet dans Godot
# (ou lancer Godot et importer le fichier project.godot)
```

#### Lancer le projet

1. Ouvrir le projet dans **Godot Engine 4.6 Mono**
2. Attendre la compilation automatique du projet C# (`.sln`)
3. Appuyer sur **F5** (ou le bouton ▶️) pour lancer le jeu
4. Le serveur TCP/UDP démarre automatiquement sur le port `6967`
5. Connecter les manettes depuis les téléphones des joueurs

#### Structure du projet

```
pixel-war/
├── assets/                  # Ressources graphiques et sonores
│   ├── fonts/               # Polices
│   ├── icons/               # Icônes
│   ├── sounds/              # Effets sonores et musiques
│   ├── sprites/             # Sprites des vaisseaux et power-ups
│   └── styles/              # Styles UI
├── scenes/                  # Scènes Godot (.tscn)
│   ├── splash.tscn          # Écran de démarrage
│   ├── mainMenu.tscn        # Menu principal
│   ├── waiting_room.tscn    # Salle d'attente (lobby)
│   ├── main.tscn            # Arène de jeu
│   ├── game_over.tscn       # Écran de fin de partie
│   └── ...
├── scripts/                 # Scripts C#
│   ├── server/              # Serveur réseau
│   │   ├── MainServer.cs    # Serveur TCP/UDP principal
│   │   ├── GameClient.cs    # Gestion d'un client connecté
│   │   ├── Packet.cs        # Protocole de paquets TCP
│   │   ├── UdpPacket.cs     # Protocole de paquets UDP
│   │   └── MessageFramer.cs # Framing des messages TCP
│   ├── GameManager.cs       # Logique de partie (timer, éliminations, power-ups)
│   ├── PlayerCharacter.cs   # Contrôle et rendu des vaisseaux
│   ├── ArenaGrid.cs         # Grille de jeu (coloriage des cases)
│   ├── WaitingRoom.cs       # Lobby d'attente
│   ├── GameOver.cs          # Écran de fin
│   └── ...
├── project.godot            # Configuration du projet Godot
└── Pixel War.csproj         # Projet C# (.NET 8)
```

---

## 🏗️ Architecture technique

```
┌──────────────────────────────────────────────┐
│            ÉCRAN PARTAGÉ (Godot)             │
│                                              │
│  ┌──────────┐  ┌───────────┐  ┌───────────┐  │
│  │  Arena   │  │   Game    │  │  Player   │  │
│  │  Grid    │  │  Manager  │  │ Character │  │
│  └──────────┘  └───────────┘  └───────────┘  │
│         ▲              ▲            ▲        │
│         └──────────────┼────────────┘        │
│                        │                     │
│              ┌─────────┴─────────┐           │
│              │   Main Server     │           │
│              │  TCP + UDP :6967  │           │
│              └─────────┬─────────┘           │
└────────────────────────┼─────────────────────┘
                         │ Réseau local (Wi-Fi)
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ 📱 Tel 1 │   │ 📱 Tel 2 │   │ 📱 Tel N  │
    │ Manette  │   │ Manette  │   │ Manette  │
    └──────────┘   └──────────┘   └──────────┘
```

- **TCP** : connexion, handshake, assignation d'équipe, power-ups, messages de jeu (victoire, élimination…)
- **UDP** : entrées joystick en temps réel (avec CRC32 pour l'intégrité et séquençage pour éviter les paquets désordonnés)

---

## 📜 Licence

Ce projet est distribué sous la **PixelWar Source Code License** (v1.0). Les assets fournies avec ce jeu sont distribuées sous la license **ASSETS_LICENSE.md**.

En résumé : usage autorisé à des fins **non commerciales** (étude, modification, contribution). L'exploitation commerciale de ce jeu est exclusive à U-Night Game Studio.

Voir le fichier [LICENSE](LICENSE) et [ASSETS_LICENSE.md](ASSETS_LICENSE.md) pour les termes complets.

---

## 🙏 Crédits

### Équipe

- **Eliott DAGOSTINOZ**
- **Pierrick DROUET DE LA THIBAUDERIE**
- **Gaya BOUNDER**

### Remerciements
- **Lucas PAUSÉ-CHAPUIS**, pour les musiques du jeu.
- Toute l'équipe Godot pour le moteur de jeu 🤍.


---

<p align="center">
  Projet réalisé à l'<strong>IUT d'Orsay</strong> — Université Paris-Saclay<br/>
  Développé avec 🤍 et <a href="https://godotengine.org">Godot Engine 4.6</a>
  <br />
  Ce projet est la propriété exclusive de U-Night Game Studio, société en formation dirigée et représentée par Eliott DAGOSTINOZ. Pour toute question: <a href="mailto:contact@u-night.org">contact@u-night.org</a>
</p>
