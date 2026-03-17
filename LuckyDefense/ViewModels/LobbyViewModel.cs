using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuckyDefense.Services;

namespace LuckyDefense.ViewModels;

public partial class LobbyViewModel : ObservableObject
{
    private readonly FirebaseService _firebase;
    private IDisposable? _opponentListener;
    private IDisposable? _statusListener;
    private string? _roomCode;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private string statusMessage = "";

    public LobbyViewModel()
    {
        _firebase = new FirebaseService();
    }

    public async Task StartMatchmaking()
    {
        IsSearching = true;
        StatusMessage = "Looking for a match...";

        try
        {
            // Try to find an open room
            string? existingRoom = await _firebase.FindOpenRoom();

            if (existingRoom != null)
            {
                // Join existing room
                _roomCode = existingRoom;
                StatusMessage = "Opponent found! Joining...";
                await _firebase.JoinRoom(_roomCode);
                await NavigateToGame();
            }
            else
            {
                // Create new room and wait
                _roomCode = await _firebase.CreateRoom();
                StatusMessage = "Waiting for opponent...";

                _opponentListener = _firebase.ListenForOpponent(_roomCode, async (guestId) =>
                {
                    _opponentListener?.Dispose();
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        StatusMessage = "Opponent found! Starting...";
                        await NavigateToGame();
                    });
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsSearching = false;
        }
    }

    private async Task NavigateToGame()
    {
        IsSearching = false;
        var parameters = new Dictionary<string, object>
        {
            { "RoomCode", _roomCode! },
            { "DeviceId", _firebase.DeviceId }
        };
        await Shell.Current.GoToAsync("//GamePage", parameters);
    }

    [RelayCommand]
    private async Task Cancel()
    {
        IsSearching = false;
        _opponentListener?.Dispose();
        _statusListener?.Dispose();

        // Clean up room if we created one
        if (_roomCode != null)
        {
            try { await _firebase.DeleteRoom(_roomCode); } catch { }
            _roomCode = null;
        }

        await Shell.Current.GoToAsync("//MainPage");
    }
}
