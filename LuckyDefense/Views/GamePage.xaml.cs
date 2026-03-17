using LuckyDefense.ViewModels;

namespace LuckyDefense.Views;

public partial class GamePage : ContentPage
{
    private GameViewModel? _vm;
    private bool _initialized;

    public GamePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _initialized = false;

        _vm = new GameViewModel();
        BindingContext = _vm;

        GameBoard.Drawable = _vm.Drawable;

        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(GameViewModel.Drawable))
                GameBoard.Invalidate();

            if (e.PropertyName == nameof(GameViewModel.RoomCode) && !_initialized && !string.IsNullOrEmpty(_vm.RoomCode))
            {
                _initialized = true;
                _vm.Initialize(Dispatcher);
            }

            // Luck upgrade glow animation
            if (e.PropertyName == nameof(GameViewModel.LuckUpgraded))
                PlayLuckGlow();
        };
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (_vm != null && !_initialized && !string.IsNullOrEmpty(_vm.RoomCode))
        {
            _initialized = true;
            _vm.Initialize(Dispatcher);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm?.Cleanup();
    }

    private async void PlayLuckGlow()
    {
        // Flash the glow border behind the luck display
        LuckGlow.Opacity = 0.6;
        await LuckGlow.FadeTo(0, 600, Easing.CubicOut);
    }
}
