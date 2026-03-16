using System;
using Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tempovium.Infrastructure.DependencyInjection;
using Tempovium.Infrastructure.Persistence;
using Tempovium.ViewModels;
using Tempovium.Core.Services;
using Tempovium.Services;

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
                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<LibraryViewModel>();
                services.AddSingleton<MediaPlayerViewModel>();
                services.AddSingleton<PlaybackTimelineService>();
                services.AddSingleton<FakePlaybackDriverService>();
                services.AddSingleton<NotesPanelViewModel>();
                //Desafortunadamente VlcLib no sirve en mac con apple silicon
                //services.AddSingleton<IPlaybackService, VlcPlaybackService>();
                services.AddSingleton<IPlaybackService, NullPlaybackService>();
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