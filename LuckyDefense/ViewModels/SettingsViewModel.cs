using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuckyDefense.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool soundEnabled;

    [ObservableProperty]
    private bool vibrationEnabled;

    public SettingsViewModel()
    {
        SoundEnabled = Preferences.Get("SoundEnabled", true);
        VibrationEnabled = Preferences.Get("VibrationEnabled", true);
    }

    partial void OnSoundEnabledChanged(bool value)
    {
        Preferences.Set("SoundEnabled", value);
    }

    partial void OnVibrationEnabledChanged(bool value)
    {
        Preferences.Set("VibrationEnabled", value);
    }

    [RelayCommand]
    private async Task Back()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
