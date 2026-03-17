# Lucky Defense - .NET MAUI Project Plan

## Context
This is a high school final project (5 units) for the "Systems Design & Programming" course. The project is a **two-player Tower Defense game with luck-based gacha mechanics** called "Lucky Defense", built with **.NET MAUI and C#** (instead of native Android). The game requires online multiplayer, complex game logic, proper OOP, database storage, and multiple advanced topics from the curriculum rubric.

---

## Requirements Coverage (Mahvan Rubric)

### Mandatory Requirements (1-5)
| # | Requirement | How We Fulfill It |
|---|---|---|
| 1 | Smartphone app | .NET MAUI (Android target) |
| 2 | No crashes during exam | Proper error handling, thorough testing |
| 3 | Complex logic | Gacha probability system, economy, merging |
| 4 | Interactive GUI, multiple screens | 4 screens with ContentPages |
| 5 | Events, listeners, runtime permissions | Touch events, game events, internet permission |

### Section 6 (Choose 2+) - We implement 3:
1. **6.8** - DataBinding with Converters
2. **6.9** - ObservableCollection + CollectionView (for shop, unit inventory)
3. **6.11** - MVVM throughout the project

### Section 7 - Database:
- **Local**: SQLite via `sqlite-net-pcl` (game saves, unit types)
- **Remote**: Firebase Realtime Database (multiplayer sync)
- Remote DB counts as **2 additional topics from Section 10**

### Section 8 - Advanced Topics (Option 2):
- **2 from Section 9**: Multiplayer app (9.15) + Custom IDrawable graphics (9.3)
- **2+ from Section 10**: SharedPreferences (10.2) + CountDownTimer (10.7) + Firebase remote DB bonus

### Section 9 (Choose 1+):
1. **9.15** - Multi-player app (real-time online via Firebase)
2. **9.3** - Custom graphics via IDrawable (GraphicsView for game board)

### Section 10:
1. **10.2** - Preferences (MAUI Preferences API for settings)
2. **10.7** - CountDownTimer (wave countdown timer)

---

## Architecture Overview

### Pattern: MVVM (Model-View-ViewModel)
```
LuckyDefense/
├── Models/            # Game data classes (Enemy, Unit, Projectile, SQLite entities)
├── ViewModels/        # Logic & state management
├── Views/             # XAML pages & controls
├── Services/          # Firebase, DB, game engine
├── Helpers/           # Utilities, converters
├── Resources/         # Images, fonts, styles
└── Data/              # SQLite entities & DAOs
```

### Screens (ContentPages)
| Screen | Purpose |
|--------|---------|
| **MainPage** | Welcome screen with Play and Settings buttons |
| **LobbyPage** | Auto-matchmaking, waiting for opponent |
| **GamePage** | Main game board (GraphicsView + Buy Unit / Upgrade Luck buttons) |
| **SettingsPage** | Simple settings: sound on/off, vibration on/off (Preferences) |

---

## Data Model

### SQLite (Local)
```
GameSave
├── SaveId (PK, int, autoincrement)
├── CreatedAt (DateTime)
└── RoundNumber (int)

Player
├── PlayerId (PK, int, autoincrement)
├── SaveId (FK → GameSave)
├── Money (int)
└── LuckLevel (int)

PlayerUnit
├── PlayerUnitId (PK, int, autoincrement)
├── PlayerId (FK → Player)
├── UnitTypeId (FK → UnitType)
├── PositionX (int)
└── PositionY (int)

UnitType
├── UnitTypeId (PK, int)
├── Name (string)
├── BasePower (int)
├── Rarity (int)  // 1=Common, 2=Rare, 3=Epic, 4=Legendary
├── AttackRange (float)
└── AttackSpeed (float)
```

### Firebase Realtime Database Structure
```json
{
  "rooms": {
    "<roomId>": {
      "host": "<deviceId>",
      "guest": "<deviceId>",
      "status": "waiting|playing|finished",
      "currentWave": 1,
      "players": {
        "<deviceId>": { "health": 100 }
      }
    }
  }
}
```

---

## Core Game Mechanics

### 1. Gacha / Character Shop
- 2 buttons at bottom of game screen: **"Buy Unit"** and **"Upgrade Luck"**
- Buy Unit: costs credits, rolls gacha, auto-places in random empty cell
- Base probabilities: Common 60%, Rare 25%, Epic 12%, Legendary 3%
- Luck upgrades shift probabilities (each level adds ~2% to rare tiers)
- Luck upgrade costs credits (same currency), scaling price

### 2. Merging System
- 3 identical units (same UnitType + same level) → 1 unit of next level
- Merge by tapping/selecting units on the board
- Merging is free (no credit cost)

### 3. Wave System (Simplified)
- All enemies are the same type — just basic enemies
- Each wave increases: **enemy count** and **enemy health**
- Formula: `enemyCount = 5 + wave * 2`, `enemyHealth = 50 + wave * 20`
- CountDownTimer between waves gives player time to buy/merge/upgrade

### 4. Economy
- Base income per round: 50 + (round_number * 5)
- Bonus for killing enemies
- Spend on: buy unit (50 credits), upgrade luck (scaling cost)

### 5. Multiplayer (Firebase)
- Both players play simultaneously on their own boards
- Leaked enemies reduce YOUR OWN health (not opponent's)
- Both face the same waves simultaneously
- **Win condition: Last one standing** — first to 0 health loses
- Firebase syncs health so both can see opponent's status
- **Auto-matchmaking**: press Play → join queue → matched automatically
- No auth/login — players identified by random device ID

---

## Implementation Phases

### Phase 1: Project Setup & Foundation ✅
- Created .NET MAUI project targeting Android
- Installed NuGet packages (sqlite-net-pcl, FirebaseDatabase.net, CommunityToolkit.Mvvm)
- Set up MVVM folder structure
- Configured Shell navigation

### Phase 2: Main Screen & Device Identity ✅
- Created MainPage with Play and Settings buttons
- Device ID generated via Preferences on first launch

### Phase 3: Data Layer ✅
- SQLite entities (GameSave, Player, PlayerUnit, UnitType)
- DatabaseService with CRUD operations
- UnitType seeding with character definitions
- FirebaseService for remote operations

### Phase 4: Matchmaking ✅
- LobbyPage with auto-matchmaking via Firebase
- Real-time listener for opponent joining
- Auto-navigation to GamePage when matched

### Phase 5: Game Board Rendering (CURRENT)
- GameBoardDrawable : IDrawable with grid drawing
- GamePage with GraphicsView
- Path drawing, unit sprites, enemies, projectiles
- Touch events on GraphicsView

### Phase 6: Core Game Logic
- GameEngine service with game loop (IDispatcherTimer)
- Enemy spawning, movement, tower targeting
- Damage calculation, health system
- Wave progression with CountDownTimer

### Phase 7: Shop & Economy
- Buy Unit button (gacha roll + auto-place)
- Upgrade Luck button (scaling cost)
- Income per round, kill bonuses

### Phase 8: Merging System
- Select 3 identical units to merge
- Validate same type + level, create upgraded unit

### Phase 9: Multiplayer Integration
- Sync player health to Firebase
- Listen to opponent's health in real-time
- Game over detection, win/loss result

### Phase 10: Settings
- Simple SettingsPage (sound on/off, vibration on/off)
- Save/load via Preferences API (Section 10.2)

### Phase 11: Testing & Documentation
- Test all screens, multiplayer, edge cases
- Prepare project documentation (tik project)
- Build APK for exam

---

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `GameEngine` | Main game loop, wave management, combat logic |
| `GameBoardDrawable` | IDrawable for rendering the game board |
| `GachaService` | Probability calculations, unit rolling |
| `FirebaseService` | Room management, real-time sync |
| `DatabaseService` | SQLite CRUD operations |
| `Unit` (Model) | Tower data: type, level, position, power |
| `Enemy` (Model) | Enemy data: health, speed, path position |
| `Projectile` (Model) | Projectile data: position, damage, target |
| `GameViewModel` | Binds game state to UI |
| `LobbyViewModel` | Auto-matchmaking logic |
| `SettingsViewModel` | Settings via Preferences |

---

## NuGet Packages
- `CommunityToolkit.Mvvm` - MVVM helpers
- `sqlite-net-pcl` - Local SQLite database
- `FirebaseDatabase.net` - Firebase Realtime Database
- `Microsoft.Maui.Graphics` - Built-in (GraphicsView, IDrawable)
