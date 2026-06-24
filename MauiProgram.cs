//using GeoMapsPrototype.Platforms.Android;
using GraTerenowa.Services;
using GraTerenowa.ViewModels;
using GraTerenowa.Views;
using Microsoft.Extensions.Logging;

namespace GeoMapsPrototype;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<Microsoft.Maui.Controls.Maps.Map, GeoMapsPrototype.Platforms.Android.CustomMapHandler>();
#endif
            });

        // ── Wspólny stan gry ─────────────────────────────────────────
        builder.Services.AddSingleton<GameStateService>();
        builder.Services.AddSingleton<MainPage>();
        // ── Twoje serwisy (GraTerenowa) ──────────────────────────────
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddTransient<LocationSetService>();
        builder.Services.AddTransient<TaskService>();
        builder.Services.AddTransient<QRCodeGeneratorService>();
        builder.Services.AddTransient<QRScannerService>();
        builder.Services.AddTransient<QuestionImportService>();

        // ── Twoje ViewModels ─────────────────────────────────────────
        builder.Services.AddTransient<LocationSetViewModel>();
        builder.Services.AddTransient<TaskEditorViewModel>();
        builder.Services.AddTransient<TaskViewModel>();

        // ── Twoje strony ─────────────────────────────────────────────
        builder.Services.AddTransient<LocationSetListPage>();
        builder.Services.AddTransient<TaskEditorPage>();
        builder.Services.AddTransient<TaskDetailPage>();
        builder.Services.AddSingleton<QRScannerPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}