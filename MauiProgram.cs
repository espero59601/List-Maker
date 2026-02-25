using ChecklistApp.Services;
using Microsoft.Extensions.Logging;

namespace ChecklistApp;

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
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Register our services
        builder.Services.AddSingleton<ListTemplateService>();
        builder.Services.AddSingleton<ImageService>();
        builder.Services.AddSingleton<FileImportService>();

        return builder.Build();
    }
}
