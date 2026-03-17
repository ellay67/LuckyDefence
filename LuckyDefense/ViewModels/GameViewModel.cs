using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuckyDefense.Services;
using LuckyDefense.Views;

namespace LuckyDefense.ViewModels;

[QueryProperty(nameof(RoomCode), "RoomCode")]
[QueryProperty(nameof(DeviceId), "DeviceId")]
public partial class GameViewModel : ObservableObject
{
    [ObservableProperty]
    private string roomCode = "";

    [ObservableProperty]
    private string deviceId = "";

    [ObservableProperty]
    private int health = 100;

    [ObservableProperty]
    private int money = 200;

    [ObservableProperty]
    private int currentWave;

    [ObservableProperty]
    private int luckLevel = 1;

    [ObservableProperty]
    private string statusText = "Waiting to start...";

    [ObservableProperty]
    private int opponentHealth = 100;

    [ObservableProperty]
    private bool isGameOver;

    [ObservableProperty]
    private string gameOverText = "";

    // Rare pull notification
    [ObservableProperty]
    private bool showPullNotification;

    [ObservableProperty]
    private string pullNotificationText = "";

    [ObservableProperty]
    private int pullNotificationRarity;

    private float _pullNotificationTimer;

    // Boss alert
    [ObservableProperty]
    private bool showBossAlert;

    [ObservableProperty]
    private string bossAlertText = "";

    private float _bossAlertTimer;

    private const int BuyUnitCost = 50;

    public GameEngine Engine { get; } = new();
    public GameEngine OpponentEngine { get; } = new(); // local simulation of opponent's board
    public GameBoardDrawable Drawable { get; } = new();

    private IDispatcherTimer? _gameTimer;
    private bool _gameStarted;
    private DateTime _lastUpdate;

    // Multiplayer
    private FirebaseService? _firebase;
    private IDisposable? _opponentStateListener;
    private IDisposable? _roomStatusListener;
    private string? _opponentId;
    private bool _isSoloMode;
    private int _lastSyncedHealth = 100;
    private float _syncTimer;
    private const float SyncInterval = 0.3f;
    private int _lastWave;
    private int _lastUnitCount;
    private int _lastClearedWave;
    private float _opponentPollTimer;
    private bool _pollingOpponent;

    public string WaveText => CurrentWave == 0 ? "Ready" : $"Wave {CurrentWave}";
    public string BuyUnitButtonText => $"Buy Unit ({BuyUnitCost})";
    public string UpgradeLuckButtonText => $"Luck Lv.{LuckLevel} ({GetLuckUpgradeCost()})";

    private int GetLuckUpgradeCost() => LuckLevel * 50;

    public string LuckDescription => LuckLevel switch
    {
        1 => "Base odds",
        2 => "Slightly luckier",
        3 => "Getting lucky",
        4 => "High roller",
        5 => "Fortune favors you",
        _ => $"Luck x{LuckLevel}"
    };

    partial void OnCurrentWaveChanged(int value) => OnPropertyChanged(nameof(WaveText));
    partial void OnLuckLevelChanged(int value)
    {
        OnPropertyChanged(nameof(UpgradeLuckButtonText));
        OnPropertyChanged(nameof(LuckDescription));
        OnPropertyChanged(nameof(LuckUpgraded)); // trigger glow in page
    }

    // Incremented on each upgrade to trigger animation
    [ObservableProperty]
    private int luckUpgraded;

    public void Initialize(IDispatcher dispatcher)
    {
        Drawable.Engine = Engine;
        Drawable.OpponentEngine = OpponentEngine;
        SoundService.Instance.Initialize();

        _isSoloMode = RoomCode == "solo-test";
        Engine.IsMultiplayer = !_isSoloMode;
        OpponentEngine.IsMultiplayer = !_isSoloMode;

        _gameTimer = dispatcher.CreateTimer();
        _gameTimer.Interval = TimeSpan.FromMilliseconds(33); // ~30 FPS
        _gameTimer.Tick += OnGameTick;
        _lastUpdate = DateTime.UtcNow;

        _gameTimer.Start();

        if (_isSoloMode)
        {
            Engine.CurrentWave = 0;
            Engine.WaveCountdown = 3f;
            _gameStarted = true;
        }
        else
        {
            // Don't start game yet — wait for both players to be ready
            Engine.WaitingToStart = true;
            Engine.WaveCountdown = 999f; // will be reset when both ready
            _gameStarted = false;
            _ = SetupMultiplayer();
        }
    }

    private async Task SetupMultiplayer()
    {
        try
        {
            _firebase = new FirebaseService();
            StatusText = $"Room: {RoomCode}, Me: {_firebase.DeviceId[..6]}";

            _opponentId = await _firebase.GetOpponentId(RoomCode);

            for (int retry = 0; retry < 10 && _opponentId == null; retry++)
            {
                StatusText = $"Finding opponent... ({retry + 1})";
                await Task.Delay(1500);
                _opponentId = await _firebase.GetOpponentId(RoomCode);
            }

            if (_opponentId != null)
            {
                StatusText = "Syncing with opponent...";

                // Signal that we're ready
                await _firebase.SetReady(RoomCode, _firebase.DeviceId);

                // Wait for opponent to also be ready
                for (int i = 0; i < 20; i++)
                {
                    bool oppReady = await _firebase.IsOpponentReady(RoomCode, _opponentId);
                    if (oppReady) break;
                    await Task.Delay(500);
                }

                // Both ready — start the game simultaneously
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Engine.CurrentWave = 0;
                    Engine.WaveCountdown = 3f;
                    Engine.WaitingToStart = true;
                    _gameStarted = true;
                    _lastUpdate = DateTime.UtcNow;

                    // Start opponent engine too
                    OpponentEngine.CurrentWave = 0;
                    OpponentEngine.WaveCountdown = 3f;
                    OpponentEngine.WaitingToStart = true;

                    StatusText = "Game starting!";
                });
                // Start background polling loop for opponent data
                _ = PollOpponentLoop();
            }
            else
            {
                StatusText = "Opponent not found!";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"MP Error: {ex.Message[..Math.Min(40, ex.Message.Length)]}";
        }
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        if (!_gameStarted || IsGameOver) return;

        var now = DateTime.UtcNow;
        float delta = (float)(now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;
        delta = MathF.Min(delta, 0.1f);

        Engine.Update(delta);

        // Run opponent simulation locally (smooth 30fps)
        if (!_isSoloMode)
        {
            OpponentEngine.BoardWidth = Engine.BoardWidth;
            OpponentEngine.BoardHeight = Engine.BoardHeight;
            if (OpponentEngine.PathPoints.Count == 0)
                OpponentEngine.BuildPath();
            OpponentEngine.Update(delta);
        }

        // Tick pull notification
        if (_pullNotificationTimer > 0)
        {
            _pullNotificationTimer -= delta;
            if (_pullNotificationTimer <= 0)
                ShowPullNotification = false;
        }

        // Tick boss alert
        if (_bossAlertTimer > 0)
        {
            _bossAlertTimer -= delta;
            if (_bossAlertTimer <= 0)
                ShowBossAlert = false;
        }

        // Sync engine state to viewmodel
        Health = Engine.Health;
        Money = Engine.Money;
        CurrentWave = Engine.CurrentWave;

        // Wave start sound + boss alert + sync opponent engine wave
        if (CurrentWave > _lastWave)
        {
            _lastWave = CurrentWave;
            SoundService.Instance.Play("wave_start");

            // Keep opponent engine in sync with our wave
            if (!_isSoloMode && OpponentEngine.CurrentWave < CurrentWave)
            {
                OpponentEngine.WaitingToStart = false;
                OpponentEngine.StartNextWave();
            }

            if (Engine.IsBossWave)
            {
                BossAlertText = "BOSS INCOMING!";
                ShowBossAlert = true;
                _bossAlertTimer = 3f;
            }
            else if (Engine.IsMiniBossWave)
            {
                BossAlertText = "MINI-BOSS INCOMING!";
                ShowBossAlert = true;
                _bossAlertTimer = 2.5f;
            }
        }

        // Update status text
        if (Engine.WaitingToStart)
            StatusText = $"Starting in {(int)Engine.WaveCountdown + 1}s...";
        else if (Engine.WaitingForOpponent)
            StatusText = "Waiting for opponent...";
        else if (!Engine.IsWaveActive && Engine.WaveCountdown > 0)
            StatusText = $"Next wave in {(int)Engine.WaveCountdown + 1}s";
        else
            StatusText = $"Enemies: {Engine.Enemies.Count}";

        // Sound & vibrate when enemy leaks
        if (Engine.EnemyLeakedThisFrame)
        {
            SoundService.Instance.Play("enemy_leak");
            if (Preferences.Get("VibrationEnabled", true))
                try { Vibration.Vibrate(TimeSpan.FromMilliseconds(100)); } catch { }
        }

        // Sound when enemy dies
        if (Engine.EnemyDiedThisFrame)
            SoundService.Instance.Play("enemy_die");

        // Sound when unit fires (throttled — only play for higher rarity to avoid spam)
        if (Engine.UnitFiredThisFrame && Engine.LastFiredRarity >= 2)
        {
            string shootSound = Engine.LastFiredRarity switch
            {
                4 => "shoot_legendary",
                3 => "shoot_epic",
                2 => "shoot_rare",
                _ => "shoot_common"
            };
            SoundService.Instance.Play(shootSound);
        }

        // Check if we died
        if (Health <= 0 && !IsGameOver)
        {
            IsGameOver = true;
            GameOverText = _isSoloMode ? "Game Over!" : "You Lost!";
            _gameTimer?.Stop();
            SoundService.Instance.Play("game_over");

            if (!_isSoloMode)
                _ = SyncHealthToFirebase(0);
        }

        // Multiplayer sync
        if (!_isSoloMode && _firebase != null && _opponentId != null)
        {
            _syncTimer += delta;
            if (_syncTimer >= SyncInterval)
            {
                _syncTimer = 0;

                // Sync health
                if (Health != _lastSyncedHealth)
                {
                    _lastSyncedHealth = Health;
                    _ = SyncHealthToFirebase(Health);
                }

                // Sync units when count changes (buy/merge)
                if (Engine.Units.Count != _lastUnitCount)
                {
                    _lastUnitCount = Engine.Units.Count;
                    _ = SyncUnitsToFirebase();
                }
            }

            // Sync wave cleared immediately when it happens
            if (Engine.WaveCleared && CurrentWave > _lastClearedWave)
            {
                _lastClearedWave = CurrentWave;
                _ = _firebase.SetWaveCleared(RoomCode, DeviceId, CurrentWave);
            }
        }

        Drawable.IsSoloMode = _isSoloMode;

        // Request redraw
        OnPropertyChanged(nameof(Drawable));
    }

    private async Task PollOpponentLoop()
    {
        _pollingOpponent = true;
        while (_pollingOpponent && !IsGameOver)
        {
            try
            {
                if (_firebase != null && _opponentId != null)
                {
                    var state = await _firebase.GetOpponentFullState(RoomCode, _opponentId);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Health — use opponent's REAL health from Firebase
                        OpponentHealth = state.Health;
                        Drawable.OpponentHealth = state.Health;
                        OpponentEngine.Health = state.Health;

                        if (state.Health <= 0 && !IsGameOver)
                        {
                            IsGameOver = true;
                            GameOverText = "You Win!";
                            _gameTimer?.Stop();
                            SoundService.Instance.Play("win");
                        }

                        // Wave sync
                        if (Engine.WaitingForOpponent && state.WaveCleared >= CurrentWave)
                        {
                            Engine.WaitingForOpponent = false;
                            Engine.WaveCountdown = 10f;
                            // Also start opponent engine's next wave
                            OpponentEngine.WaitingForOpponent = false;
                            OpponentEngine.WaveCountdown = 10f;
                        }

                        // Sync opponent's units into opponent engine
                        if (state.Units != null)
                        {
                            SyncUnitsToOpponentEngine(state.Units);
                        }
                    });
                }
            }
            catch { }

            await Task.Delay(500); // Only need to poll actions, not positions
        }
    }

    private void SyncUnitsToOpponentEngine(List<UnitSyncData> units)
    {
        // Clear and rebuild opponent units from Firebase data
        OpponentEngine.Units.Clear();
        foreach (var u in units)
        {
            // Get stats based on rarity
            var (_, name, basePower, attackRange, attackSpeed, _) = GetUnitStats(u.Rarity);
            OpponentEngine.PlaceUnit(u.Rarity, name, basePower, attackRange, attackSpeed, u.Rarity, u.Col, u.Row);
            // Set the level on the placed unit
            var placed = OpponentEngine.Units.LastOrDefault();
            if (placed != null) placed.Level = u.Level;
        }
    }

    private (int id, string name, int basePower, float attackRange, float attackSpeed, int rarity) GetUnitStats(int rarity)
    {
        return rarity switch
        {
            4 => (4, "Dragon", 60, 3.5f, 1.0f, 4),
            3 => (3, "Wizard", 35, 3.0f, 1.5f, 3),
            2 => (2, "Knight", 20, 2.5f, 2.0f, 2),
            _ => (1, "Soldier", 12, 2.0f, 2.5f, 1),
        };
    }

    private async Task SyncHealthToFirebase(int health)
    {
        if (_firebase == null) return;
        try { await _firebase.UpdateHealth(RoomCode, DeviceId, health); }
        catch { }
    }

    private async Task SyncUnitsToFirebase()
    {
        if (_firebase == null) return;
        try
        {
            var unitData = Engine.Units.Select(u => new UnitSyncData
            {
                Col = u.GridX,
                Row = u.GridY,
                Rarity = u.Rarity,
                Level = u.Level
            }).ToList();
            await _firebase.SyncUnits(RoomCode, DeviceId, unitData);
        }
        catch { }
    }

    private async Task PollOpponentData()
    {
        if (_firebase == null || _opponentId == null) return;
        try
        {
            // Single fetch for all opponent data
            var state = await _firebase.GetOpponentFullState(RoomCode, _opponentId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Wave sync — unlock if opponent also cleared this wave
                if (Engine.WaitingForOpponent && state.WaveCleared >= CurrentWave)
                {
                    Engine.WaitingForOpponent = false;
                    Engine.WaveCountdown = 10f;
                }

                // Opponent health
                OpponentHealth = state.Health;
                Drawable.OpponentHealth = state.Health;

                // Sync opponent's units into opponent engine
                if (state.Units != null)
                {
                    SyncUnitsToOpponentEngine(state.Units);
                }
            });
        }
        catch { }
    }


    public void Cleanup()
    {
        _gameTimer?.Stop();
        _gameStarted = false;
        _pollingOpponent = false;
        _opponentStateListener?.Dispose();
        _roomStatusListener?.Dispose();
    }

    [RelayCommand]
    private void BuyUnit()
    {
        if (Money < BuyUnitCost || IsGameOver) return;

        var cell = Engine.GetRandomEmptyCell();
        if (cell == null) return;

        var (unitTypeId, name, basePower, attackRange, attackSpeed, rarity) = RollGacha();

        var unit = Engine.PlaceUnit(unitTypeId, name, basePower, attackRange, attackSpeed, rarity, cell.Value.col, cell.Value.row);
        if (unit != null)
        {
            Engine.Money -= BuyUnitCost;
            Money = Engine.Money;

            SoundService.Instance.Play("buy_unit");

            // Show notification for epic/legendary pulls
            if (rarity >= 3)
            {
                string rarityLabel = rarity == 4 ? "LEGENDARY" : "EPIC";
                PullNotificationText = $"{rarityLabel}!\n{name}";
                PullNotificationRarity = rarity;
                ShowPullNotification = true;
                _pullNotificationTimer = 2f;
                SoundService.Instance.Play(rarity == 4 ? "legendary_pull" : "epic_pull");
            }

            // Auto-merge: if 3 identical units exist, merge them
            bool merged = false;
            while (Engine.TryAutoMerge()) { merged = true; }
            if (merged) SoundService.Instance.Play("merge");
        }
    }

    [RelayCommand]
    private void UpgradeLuck()
    {
        if (IsGameOver) return;
        int cost = GetLuckUpgradeCost();
        if (Money < cost) return;

        Engine.Money -= cost;
        Money = Engine.Money;
        LuckLevel++;
        Engine.LuckLevel = LuckLevel;
        LuckUpgraded++;
        SoundService.Instance.Play("upgrade_luck");
    }

    [RelayCommand]
    private async Task BackToMain()
    {
        Cleanup();

        // Clean up room in Firebase
        if (!_isSoloMode && _firebase != null)
        {
            try { await _firebase.UpdateRoomStatus(RoomCode, "finished"); } catch { }
        }

        await Shell.Current.GoToAsync("//MainPage");
    }

    private (int unitTypeId, string name, int basePower, float attackRange, float attackSpeed, int rarity) RollGacha()
    {
        float luckBonus = (LuckLevel - 1) * 2f;
        float legendaryChance = 3f + luckBonus * 0.5f;
        float epicChance = 12f + luckBonus;
        float rareChance = 25f + luckBonus;

        float roll = Random.Shared.NextSingle() * 100f;

        if (roll < legendaryChance)
            return (4, "Dragon", 60, 3.5f, 1.0f, 4);
        if (roll < legendaryChance + epicChance)
            return (3, "Wizard", 35, 3.0f, 1.5f, 3);
        if (roll < legendaryChance + epicChance + rareChance)
            return (2, "Knight", 20, 2.5f, 2.0f, 2);

        return (1, "Soldier", 12, 2.0f, 2.5f, 1);
    }
}
