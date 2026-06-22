using System;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.Media.Abstractions.Backends;
using Tempovium.Media.Abstractions.Contracts;
using Tempovium.Media.Mac.Backends;

namespace Tempovium.DependencyInjection;

public enum MediaBackendPlatform
{
    MacOS,
    Windows,
    Other
}

public static class MediaBackendServiceCollectionExtensions
{
    public static IServiceCollection AddMediaBackendForCurrentPlatform(this IServiceCollection services)
    {
        return services.AddMediaBackendForPlatform(DetectCurrentPlatform());
    }

    public static MediaBackendPlatform DetectCurrentPlatform()
    {
        if (OperatingSystem.IsMacOS())
        {
            return MediaBackendPlatform.MacOS;
        }

        if (OperatingSystem.IsWindows())
        {
            return MediaBackendPlatform.Windows;
        }

        return MediaBackendPlatform.Other;
    }

    public static IServiceCollection AddMediaBackendForPlatform(
        this IServiceCollection services,
        MediaBackendPlatform platform)
    {
        if (platform == MediaBackendPlatform.MacOS)
        {
            services.AddSingleton<IMediaBackend, MacMediaBackend>();
            return services;
        }

        services.AddSingleton<IMediaBackend, UnsupportedMediaBackend>();
        return services;
    }
}
