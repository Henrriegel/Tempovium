using Tempovium.Media.Abstractions.Contracts;
using Tempovium.Media.Abstractions.Enums;

namespace Tempovium.Media.Abstractions.Backends;

public sealed class UnsupportedMediaBackend : IMediaBackend, IMediaBackendInfo
{
    public const string UnsupportedMessage =
        "Media playback is not supported on this platform yet.";

    private EventHandler<string>? _mediaFailed;

    public bool IsLoaded => false;
    public bool IsPlaying => false;
    public TimeSpan Duration => TimeSpan.Zero;
    public TimeSpan Position => TimeSpan.Zero;

    public MediaBackendKind BackendKind => MediaBackendKind.Unknown;
    public string DisplayName => "Unsupported media backend";

    public event EventHandler? MediaOpened
    {
        add { }
        remove { }
    }

    public event EventHandler? MediaEnded
    {
        add { }
        remove { }
    }

    public event EventHandler<string>? MediaFailed
    {
        add => _mediaFailed += value;
        remove => _mediaFailed -= value;
    }

    public event EventHandler<TimeSpan>? PositionChanged
    {
        add { }
        remove { }
    }

    public void Load(string path)
    {
        ReportUnsupported();
    }

    public void Play()
    {
        ReportUnsupported();
    }

    public void Pause()
    {
        ReportUnsupported();
    }

    public void Stop()
    {
        ReportUnsupported();
    }

    public void Seek(TimeSpan position)
    {
        ReportUnsupported();
    }

    public void SetVolume(double volume)
    {
    }

    public void UpdateState()
    {
    }

    public void Dispose()
    {
    }

    private void ReportUnsupported()
    {
        _mediaFailed?.Invoke(this, UnsupportedMessage);
    }
}
