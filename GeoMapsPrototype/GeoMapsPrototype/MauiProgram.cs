using GraTerenowa.Services;
using GraTerenowa.ViewModels;
using GraTerenowa.Views;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui;

namespace GraTerenowa;

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

        // ── Baza danych (singleton – jedno połączenie przez cały czas) ──
        builder.Services.AddSingleton<DatabaseService>();

        // ── Serwisy ──────────────────────────────────────────────────────
        builder.Services.AddTransient<LocationSetService>();
        builder.Services.AddTransient<TaskService>();
        builder.Services.AddTransient<QRCodeGeneratorService>();
        builder.Services.AddTransient<QRScannerService>();
        builder.Services.AddTransient<QuestionImportService>();

        // ── ViewModels ───────────────────────────────────────────────────
        builder.Services.AddTransient<LocationSetViewModel>();
        builder.Services.AddTransient<TaskEditorViewModel>();
        builder.Services.AddTransient<TaskViewModel>();

        // ── Strony ───────────────────────────────────────────────────────
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