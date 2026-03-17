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
    public GameBoardDrawable Drawable { get; } = new();

    private IDispatcherTimer? _gameTimer;
    private bool _gameStarted;
    private DateTime _lastUpdate;

    // Multiplayer
    private FirebaseService? _firebase;
    private IDisposable? _opponentHealthListener;
    private IDisposable? _roomStatusListener;
    private string? _opponentId;
    private bool _isSoloMode;
    private int _lastSyncedHealth = 100;
    private float _syncTimer;
    private const float SyncInterval = 1f; // sync every 1 second
    private int _lastWave;

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
        SoundService.Instance.Initialize();

        _isSoloMode = RoomCode == "solo-test";

        _gameTimer = dispatcher.CreateTimer();
        _gameTimer.Interval = TimeSpan.FromMilliseconds(33); // ~30 FPS
        _gameTimer.Tick += OnGameTick;
        _lastUpdate = DateTime.UtcNow;

        Engine.CurrentWave = 0;
        Engine.WaveCountdown = 3f;
        _gameStarted = true;
        _gameTimer.Start();

        if (!_isSoloMode)
            _ = SetupMultiplayer();
    }

    private async Task SetupMultiplayer()
    {
        try
        {
            _firebase = new FirebaseService();
            _opponentId = await _firebase.GetOpponentId(RoomCode);

            if (_opponentId != null)
            {
                // Listen to opponent's health
                _opponentHealthListener = _firebase.ListenToOpponentHealth(
                    RoomCode, _opponentId, (health) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            OpponentHealth = health;
                            Drawable.OpponentHealth = health;

                            // Opponent died — we win!
                            if (health <= 0 && !IsGameOver)
                            {
                                IsGameOver = true;
                                GameOverText = "You Win!";
                                _gameTimer?.Stop();
                            }
                        });
                    });

                // Listen for room status changes (e.g., opponent disconnects)
                _roomStatusListener = _firebase.ListenToRoomStatus(
                    RoomCode, (status) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (status == "finished" && !IsGameOver)
                            {
                                IsGameOver = true;
                                GameOverText = "Game Over";
                                _gameTimer?.Stop();
                            }
                        });
                    });
            }
        }
        catch (Exception ex)
        {
            StatusText = $"MP Error: {ex.Message}";
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

        // Wave start sound + boss alert
        if (CurrentWave > _lastWave)
        {
            _lastWave = CurrentWave;
            SoundService.Instance.Play("wave_start");

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

        // Sync health to Firebase periodically
        if (!_isSoloMode)
        {
            _syncTimer += delta;
            if (_syncTimer >= SyncInterval && Health != _lastSyncedHealth)
            {
                _syncTimer = 0;
                _lastSyncedHealth = Health;
                _ = SyncHealthToFirebase(Health);
            }
        }

        // Request redraw
        OnPropertyChanged(nameof(Drawable));
    }

    private async Task SyncHealthToFirebase(int health)
    {
        if (_firebase == null) return;
        try
        {
            await _firebase.UpdateHealth(RoomCode, DeviceId, health);
        }
        catch { /* Ignore sync errors to not interrupt gameplay */ }
    }

    public void Cleanup()
    {
        _gameTimer?.Stop();
        _gameStarted = false;
        _opponentHealthListener?.Dispose();
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
