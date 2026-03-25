using System;
using Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tempovium.Core.Services;
using Tempovium.Infrastructure.DependencyInjection;
using Tempovium.Infrastructure.Persistence;
using Tempovium.Services;
using Tempovium.ViewModels;
using Tempovium.Media.Abstractions.Contracts;
using Tempovium.Media.Mac.Backends;

namespace Tempovium;

internal class Program
{
    public static IHost AppHost { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {

        AppHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddTempoviumInfrastructure();

                // Servicios de app
                services.AddSingleton<UserSessionService>();
                services.AddSingleton<NavigationService>();
                services.AddSingleton<SelectedMediaService>();
                
                services.AddSingleton<IMediaBackend, MacMediaBackend>();

                // Timeline / control de reproducción
                services.AddSingleton<PlaybackTimelineService>();
                services.AddSingleton<PlaybackControlService>();

                // ViewModels persistentes
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MediaPlayerViewModel>();
                services.AddSingleton<NotesPanelViewModel>();

                // ViewModels de navegación
                services.AddTransient<LoginViewModel>();
                services.AddTransient<LibraryViewModel>();
                
                services.AddSingleton<MediaPlayerViewModel>();
                services.AddTransient<LibraryViewModel>();
            })
            .Build();

        using (var scope = AppHost.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TempoviumDbContext>();
            dbContext.Database.Migrate();
        }

        using (var scope = AppHost.Services.CreateScope())
        {
            var navigationService = scope.ServiceProvider.GetRequiredService<NavigationService>();
            var loginViewModel = scope.ServiceProvider.GetRequiredService<LoginViewModel>();
            navigationService.CurrentView = loginViewModel;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}