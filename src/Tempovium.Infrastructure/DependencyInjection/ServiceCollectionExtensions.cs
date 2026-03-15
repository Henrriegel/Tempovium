using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Services;
using Tempovium.Infrastructure.Persistence;
using Tempovium.Infrastructure.Repositories;
using Tempovium.Infrastructure.Services;

namespace Tempovium.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTempoviumInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<TempoviumDbContext>(options =>
            options.UseSqlite("Data Source=tempovium.db"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IFileHashService, FileHashService>();
        services.AddScoped<IMediaImportService, MediaImportService>();

        services.AddSingleton<MediaFileTypeDetector>();
        services.AddSingleton<MediaFileValidator>();
        services.AddSingleton<UserSessionService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<SelectedMediaService>();

        return services;
    }
}