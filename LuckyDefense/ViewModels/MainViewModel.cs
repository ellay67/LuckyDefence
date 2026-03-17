using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuckyDefense.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [RelayCommand]
    private async Task Play()
    {
        await Shell.Current.GoToAsync("//LobbyPage");
    }

    [RelayCommand]
    private async Task Settings()
    {
        await Shell.Current.GoToAsync("//SettingsPage");
    }
}
