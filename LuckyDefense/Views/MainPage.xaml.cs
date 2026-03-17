namespace LuckyDefense.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnSoloTestClicked(object? sender, EventArgs e)
    {
        var parameters = new Dictionary<string, object>
        {
            { "RoomCode", "solo-test" },
            { "DeviceId", "test-device" }
        };
        await Shell.Current.GoToAsync("//GamePage", parameters);
    }
}
