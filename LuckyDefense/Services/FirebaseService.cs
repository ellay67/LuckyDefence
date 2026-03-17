using Firebase.Database;
using Firebase.Database.Query;

namespace LuckyDefense.Services;

public class FirebaseService
{
    private const string FirebaseUrl = "https://luckydefence-1fc66-default-rtdb.firebaseio.com/";
    private readonly FirebaseClient _client;

    public string DeviceId { get; }

    public FirebaseService()
    {
        // Generate and persist a unique device ID
        string? stored = Preferences.Get("DeviceId", "");
        if (string.IsNullOrEmpty(stored))
        {
            stored = Guid.NewGuid().ToString("N")[..12];
            Preferences.Set("DeviceId", stored);
        }
        DeviceId = stored;
        _client = new FirebaseClient(FirebaseUrl);
    }

    // --- Matchmaking ---

    public async Task<string?> FindOpenRoom()
    {
        // Query all rooms and filter for "waiting" status
        // (avoids case-sensitivity issues with Firebase OrderBy)
        try
        {
            var allRooms = await _client
                .Child("rooms")
                .OnceAsync<RoomData>();

            var openRoom = allRooms.FirstOrDefault(r =>
                r.Object != null &&
                r.Object.Status == "waiting" &&
                r.Object.Host != DeviceId &&
                r.Key != "solo-test");

            return openRoom?.Key;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> CreateRoom()
    {
        string roomCode = Guid.NewGuid().ToString("N")[..6].ToUpper();

        var room = new RoomData
        {
            Host = DeviceId,
            Guest = "",
            Status = "waiting",
            CurrentWave = 0
        };

        await _client.Child("rooms").Child(roomCode).PutAsync(room);
        await SetPlayerState(roomCode, DeviceId, new PlayerState());

        return roomCode;
    }

    public async Task JoinRoom(string roomCode)
    {
        // Update Guest and Status by patching the room object
        await _client.Child("rooms").Child(roomCode)
            .PatchAsync(new { Guest = DeviceId, Status = "playing" });
        await SetPlayerState(roomCode, DeviceId, new PlayerState());
    }

    public IDisposable ListenForOpponent(string roomCode, Action<string> onGuestJoined)
    {
        return _client
            .Child("rooms")
            .Child(roomCode)
            .Child("Guest")
            .AsObservable<string>()
            .Subscribe(change =>
            {
                if (!string.IsNullOrEmpty(change.Object))
                    onGuestJoined(change.Object);
            });
    }

    // --- Player State ---

    public async Task SetPlayerState(string roomCode, string playerId, PlayerState state)
    {
        await _client
            .Child("rooms").Child(roomCode)
            .Child("players").Child(playerId)
            .PutAsync(state);
    }

    public async Task UpdateHealth(string roomCode, string playerId, int health)
    {
        await _client
            .Child("rooms").Child(roomCode)
            .Child("players").Child(playerId)
            .PatchAsync(new { Health = health });
    }

    public async Task<string?> GetOpponentId(string roomCode)
    {
        var room = await _client.Child("rooms").Child(roomCode).OnceSingleAsync<RoomData>();
        if (room == null) return null;

        // If we're the host, opponent is the guest (and vice versa)
        string opponent = room.Host == DeviceId ? room.Guest : room.Host;
        return string.IsNullOrEmpty(opponent) ? null : opponent;
    }

    public IDisposable ListenToOpponentHealth(string roomCode, string opponentId, Action<int> onHealthChanged)
    {
        return _client
            .Child("rooms")
            .Child(roomCode)
            .Child("players")
            .Child(opponentId)
            .Child("Health")
            .AsObservable<int>()
            .Subscribe(change =>
            {
                onHealthChanged(change.Object);
            });
    }

    // --- Room Status ---

    public IDisposable ListenToRoomStatus(string roomCode, Action<string> onStatusChanged)
    {
        return _client
            .Child("rooms")
            .Child(roomCode)
            .Child("Status")
            .AsObservable<string>()
            .Subscribe(change =>
            {
                if (!string.IsNullOrEmpty(change.Object))
                    onStatusChanged(change.Object);
            });
    }

    public async Task UpdateRoomStatus(string roomCode, string status)
    {
        await _client.Child("rooms").Child(roomCode)
            .PatchAsync(new { Status = status });
    }

    // --- Wave & Unit Sync ---

    public async Task SetWaveCleared(string roomCode, string playerId, int wave)
    {
        await _client.Child("rooms").Child(roomCode)
            .Child("players").Child(playerId)
            .PatchAsync(new { WaveCleared = wave });
    }

    public async Task<FullPlayerSync> GetOpponentFullState(string roomCode, string opponentId)
    {
        try
        {
            var state = await _client.Child("rooms").Child(roomCode)
                .Child("players").Child(opponentId)
                .OnceSingleAsync<FullPlayerSync>();
            return state ?? new FullPlayerSync();
        }
        catch { return new FullPlayerSync(); }
    }

    public IDisposable ListenToOpponentState(string roomCode, string opponentId, Action<FullPlayerSync> onChanged)
    {
        return _client
            .Child("rooms")
            .Child(roomCode)
            .Child("players")
            .Child(opponentId)
            .AsObservable<FullPlayerSync>()
            .Subscribe(change =>
            {
                if (change.Object != null)
                    onChanged(change.Object);
            });
    }

    public async Task SyncUnits(string roomCode, string playerId, List<UnitSyncData> units)
    {
        await _client.Child("rooms").Child(roomCode)
            .Child("players").Child(playerId)
            .Child("Units")
            .PutAsync(units);
    }

    public async Task<List<UnitSyncData>> GetOpponentUnits(string roomCode, string opponentId)
    {
        try
        {
            var units = await _client.Child("rooms").Child(roomCode)
                .Child("players").Child(opponentId)
                .Child("Units")
                .OnceSingleAsync<List<UnitSyncData>>();
            return units ?? new List<UnitSyncData>();
        }
        catch { return new List<UnitSyncData>(); }
    }

    public async Task SyncEnemies(string roomCode, string playerId, List<EnemySyncData> enemies)
    {
        await _client.Child("rooms").Child(roomCode)
            .Child("players").Child(playerId)
            .Child("Enemies")
            .PutAsync(enemies);
    }

    public async Task<List<EnemySyncData>> GetOpponentEnemies(string roomCode, string opponentId)
    {
        try
        {
            var enemies = await _client.Child("rooms").Child(roomCode)
                .Child("players").Child(opponentId)
                .Child("Enemies")
                .OnceSingleAsync<List<EnemySyncData>>();
            return enemies ?? new List<EnemySyncData>();
        }
        catch { return new List<EnemySyncData>(); }
    }

    public async Task SetReady(string roomCode, string playerId)
    {
        await _client.Child("rooms").Child(roomCode)
            .Child("players").Child(playerId)
            .PatchAsync(new { Ready = true });
    }

    public async Task<bool> IsOpponentReady(string roomCode, string opponentId)
    {
        try
        {
            var state = await _client.Child("rooms").Child(roomCode)
                .Child("players").Child(opponentId)
                .Child("Ready")
                .OnceSingleAsync<bool>();
            return state;
        }
        catch { return false; }
    }

    public async Task DeleteRoom(string roomCode)
    {
        await _client.Child("rooms").Child(roomCode).DeleteAsync();
    }
}

// Firebase data models

public class RoomData
{
    public string Host { get; set; } = "";
    public string Guest { get; set; } = "";
    public string Status { get; set; } = "waiting";
    public int CurrentWave { get; set; }
}

public class PlayerState
{
    public int Health { get; set; } = 100;
    public int Money { get; set; } = 100;
    public int LuckLevel { get; set; } = 1;
    public int WaveCleared { get; set; }
}

public class UnitSyncData
{
    public int Col { get; set; }
    public int Row { get; set; }
    public int Rarity { get; set; }
    public int Level { get; set; }
}

public class FullPlayerSync
{
    public int Health { get; set; } = 100;
    public int WaveCleared { get; set; }
    public List<UnitSyncData>? Units { get; set; }
    public List<EnemySyncData>? Enemies { get; set; }
}

public class EnemySyncData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float HealthPct { get; set; }
    public bool IsBoss { get; set; }
    public bool IsMiniBoss { get; set; }
}
