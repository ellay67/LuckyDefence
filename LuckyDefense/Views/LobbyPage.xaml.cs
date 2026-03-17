using LuckyDefense.ViewModels;

namespace LuckyDefense.Views;

public partial class LobbyPage : ContentPage
{
    public LobbyPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LobbyViewModel vm)
        {
            await vm.StartMatchmaking();
        }
    }
}
