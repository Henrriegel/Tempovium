using Microsoft.Extensions.DependencyInjection;
using Tempovium.DependencyInjection;
using Tempovium.Media.Abstractions.Backends;
using Tempovium.Media.Abstractions.Contracts;

namespace Tempovium.Tests;

public class MediaBackendRegistrationTests
{
    [Theory]
    [InlineData(MediaBackendPlatform.Windows)]
    [InlineData(MediaBackendPlatform.Other)]
    public void NonMacPlatformsResolveUnsupportedBackend(MediaBackendPlatform platform)
    {
        var services = new ServiceCollection();

        services.AddMediaBackendForPlatform(platform);

        using var provider = services.BuildServiceProvider();
        var backend = provider.GetRequiredService<IMediaBackend>();

        Assert.IsType<UnsupportedMediaBackend>(backend);
    }

    [Fact]
    public void WindowsRegistrationUsesUnsupportedBackend()
    {
        var services = new ServiceCollection();

        services.AddMediaBackendForPlatform(MediaBackendPlatform.Windows);

        var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IMediaBackend));
        Assert.Equal(typeof(UnsupportedMediaBackend), descriptor.ImplementationType);
    }

    [Fact]
    public void UnsupportedBackendReportsClearFailure()
    {
        using var backend = new UnsupportedMediaBackend();
        string? failure = null;
        backend.MediaFailed += (_, message) => failure = message;

        backend.Load("video.mp4");

        Assert.Equal(UnsupportedMediaBackend.UnsupportedMessage, failure);
        Assert.False(backend.IsLoaded);
        Assert.False(backend.IsPlaying);
    }
}
