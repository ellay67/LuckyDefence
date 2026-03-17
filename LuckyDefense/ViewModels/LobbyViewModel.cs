using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuckyDefense.Services;

namespace LuckyDefense.ViewModels;

public partial class LobbyViewModel : ObservableObject
{
    private readonly FirebaseService _firebase;
    private string? _roomCode;
    private bool _cancelled;

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
        _cancelled = false;
        StatusMessage = "Looking for a match...";

        try
        {
            // Try to find an open room
            string? existingRoom = await _firebase.FindOpenRoom();

            if (_cancelled) return;

            if (existingRoom != null)
            {
                // Join existing room
                _roomCode = existingRoom;
                StatusMessage = "Opponent found! Joining...";
                await _firebase.JoinRoom(_roomCode);

                if (_cancelled) return;
                await NavigateToGame();
            }
            else
            {
                // Create new room and wait
                _roomCode = await _firebase.CreateRoom();
                StatusMessage = "Waiting for opponent...";

                if (_cancelled) return;

                // Poll for opponent joining (more reliable than listener)
                await PollForOpponent();
            }
        }
        catch (Exception ex)
        {
            if (!_cancelled)
            {
                StatusMessage = $"Error: {ex.Message}";
                IsSearching = false;
            }
        }
    }

    private async Task PollForOpponent()
    {
        while (!_cancelled)
        {
            await Task.Delay(1500);
            if (_cancelled || _roomCode == null) return;

            try
            {
                string? opponentId = await _firebase.GetOpponentId(_roomCode);
                if (!string.IsNullOrEmpty(opponentId))
                {
                    if (_cancelled) return;
                    StatusMessage = "Opponent found! Starting...";
                    await Task.Delay(500);
                    if (_cancelled) return;
                    await NavigateToGame();
                    return;
                }
            }
            catch
            {
                // Retry on next poll
            }
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
        _cancelled = true;
        IsSearching = false;

        if (_roomCode != null)
        {
            try { await _firebase.DeleteRoom(_roomCode); } catch { }
            _roomCode = null;
        }

        await Shell.Current.GoToAsync("//MainPage");
    }
}
