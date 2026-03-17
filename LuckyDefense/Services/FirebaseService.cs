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
        DeviceId = Preferences.Get("DeviceId", Guid.NewGuid().ToString("N")[..12]);
        _client = new FirebaseClient(FirebaseUrl);
    }

    // --- Matchmaking ---

    public async Task<string?> FindOpenRoom()
    {
        var rooms = await _client
            .Child("rooms")
            .OrderBy("status")
            .EqualTo("waiting")
            .OnceAsync<RoomData>();

        var openRoom = rooms.FirstOrDefault(r => r.Object.Host != DeviceId);
        return openRoom?.Key;
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

        // Set host player state
        await SetPlayerState(roomCode, DeviceId, new PlayerState());

        return roomCode;
    }

    public async Task JoinRoom(string roomCode)
    {
        await _client.Child("rooms").Child(roomCode).Child("Guest").PutAsync(DeviceId);
        await _client.Child("rooms").Child(roomCode).Child("Status").PutAsync("playing");

        // Set guest player state
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
            .Child("Health")
            .PutAsync(health);
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

    public async Task<string?> GetOpponentId(string roomCode)
    {
        var room = await _client.Child("rooms").Child(roomCode).OnceSingleAsync<RoomData>();
        if (room == null) return null;
        return room.Host == DeviceId ? room.Guest : room.Host;
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
        await _client.Child("rooms").Child(roomCode).Child("Status").PutAsync(status);
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
}
