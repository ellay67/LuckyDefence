# Lucky Defense

A two-player Tower Defense game with luck-based gacha mechanics, built with .NET MAUI and C#.

## About

Lucky Defense is a real-time strategy game where two players compete online. Each player defends their own base by purchasing random units, upgrading their luck to get rarer characters, and merging identical units to create stronger defenders. Enemies (goblins) walk around the border of your field — if they reach the exit portal, you lose health. The last player standing wins.

## Features

- **Online Multiplayer** — Auto-matchmaking via Firebase Realtime Database
- **Gacha System** — Buy units with random rarity (Common, Rare, Epic, Legendary)
- **Luck Upgrades** — Spend credits to increase your chances of getting rare units
- **Auto-Merge** — 3 identical units automatically merge into a stronger one
- **4 Unit Types** — Slime (Common), Archer (Rare), Wizard (Epic), Dragon (Legendary)
- **Unique Projectiles** — Green blobs, arrows, magic orbs, fireballs
- **Boss Waves** — Mini-bosses every 5 waves, full bosses every 10 waves
- **Custom Graphics** — Hand-drawn characters using MAUI GraphicsView (IDrawable)
- **Sound Effects** — Procedurally generated audio (no external files needed)
- **Split-Screen View** — See your board and your opponent's board simultaneously
- **Settings** — Sound and vibration toggles saved with Preferences

## Tech Stack

- **.NET 9** with **.NET MAUI** (Android target)
- **C#** with **MVVM** architecture (CommunityToolkit.Mvvm)
- **SQLite** (sqlite-net-pcl) for local data storage
- **Firebase Realtime Database** for multiplayer sync
- **MAUI GraphicsView + IDrawable** for custom game rendering

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- MAUI workload installed:
  ```bash
  sudo dotnet workload install maui
  ```
- [Android SDK](https://developer.android.com/studio) with API level 35
- An Android emulator or physical device

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/ellay67/LuckyDefence.git
cd LuckyDefence
```

### 2. Restore packages

```bash
cd LuckyDefense
dotnet restore
```

### 3. Build the project

```bash
dotnet build -f net9.0-android
```

### 4. Run on an Android emulator

Start your emulator first, then:

```bash
dotnet build -f net9.0-android -t:Install
```

Launch the app on the emulator:

```bash
adb shell monkey -p com.companyname.luckydefense -c android.intent.category.LAUNCHER 1
```

### 5. Run on a physical Android device

1. Enable **Developer Options** on your Android phone:
   - Go to **Settings → About Phone**
   - Tap **Build Number** 7 times
2. Enable **USB Debugging**:
   - Go to **Settings → Developer Options**
   - Turn on **USB Debugging**
3. Connect your phone via USB cable
4. Verify the device is recognized:
   ```bash
   adb devices
   ```
5. Build and install:
   ```bash
   dotnet build -f net9.0-android -t:Install
   ```
6. The app will appear in your app drawer as **LuckyDefense**

### 6. Build a standalone APK

```bash
dotnet publish -f net9.0-android -c Release
```

The APK will be at:
```
bin/Release/net9.0-android/publish/com.companyname.luckydefense-Signed.apk
```

You can transfer this APK to any Android device and install it manually (enable "Install from unknown sources" in device settings).

## Firebase Setup

The app uses Firebase Realtime Database for online multiplayer. The database URL is configured in `Services/FirebaseService.cs`.

To use your own Firebase project:

1. Go to [Firebase Console](https://console.firebase.google.com)
2. Create a new project
3. Go to **Build → Realtime Database** → Create Database (Test mode)
4. Copy the database URL
5. Update the `FirebaseUrl` constant in `Services/FirebaseService.cs`
6. Set the database rules to:
   ```json
   {
     "rules": {
       ".read": true,
       ".write": true,
       "rooms": {
         ".indexOn": ["status"]
       }
     }
   }
   ```

## Project Structure

```
LuckyDefense/
├── Models/              # Data classes (Enemy, Unit, Projectile, SQLite entities)
├── ViewModels/          # MVVM ViewModels (Game, Lobby, Main, Settings)
├── Views/               # XAML pages and GameBoardDrawable
├── Services/            # GameEngine, FirebaseService, DatabaseService, SoundService
├── Resources/           # App icons, fonts, styles
└── Platforms/Android/   # Android-specific configuration
```

### Key Files

| File | Description |
|------|-------------|
| `Services/GameEngine.cs` | Core game loop, enemy spawning, combat, wave system |
| `Views/GameBoardDrawable.cs` | All custom rendering (terrain, characters, projectiles, portals) |
| `ViewModels/GameViewModel.cs` | Game state management, gacha system, multiplayer sync |
| `Services/FirebaseService.cs` | Matchmaking, room management, health sync |
| `Services/SoundService.cs` | Procedural WAV sound generation and playback |
| `Services/DatabaseService.cs` | SQLite database operations |

## How to Play

1. Open the app and tap **PLAY ONLINE** to find an opponent, or **SOLO PRACTICE** to play alone
2. The game starts after a short countdown
3. Enemies (goblins) spawn at the green portal and walk clockwise around the border
4. Tap **Buy Unit** to spend 50 credits and get a random unit placed on your field
5. Units automatically attack nearby enemies
6. Tap **Upgrade Luck** to improve your chances of getting rarer (stronger) units
7. When you have 3 identical units (same type and level), they auto-merge into a higher level
8. If enemies reach the red portal, you lose health
9. Every 5 waves: a mini-boss appears. Every 10 waves: a full boss appears
10. In multiplayer: the first player to reach 0 HP loses

## Unit Rarity

| Rarity | Unit | Base Damage | Attack Range | Base Chance |
|--------|------|-------------|-------------|-------------|
| Common | Slime | 12 | 2.0 cells | 60% |
| Rare | Archer | 20 | 2.5 cells | 25% |
| Epic | Wizard | 35 | 3.0 cells | 12% |
| Legendary | Dragon | 60 | 3.5 cells | 3% |

Upgrading luck shifts these probabilities toward rarer units.
