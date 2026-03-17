using Microsoft.Extensions.Logging;
using LuckyDefense.Services;

namespace LuckyDefense;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Generate device ID on first launch
        if (!Preferences.ContainsKey("DeviceId"))
        {
            Preferences.Set("DeviceId", Guid.NewGuid().ToString("N")[..12]);
        }

        // Register services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<FirebaseService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
