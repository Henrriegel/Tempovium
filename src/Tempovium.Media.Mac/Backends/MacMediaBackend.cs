using Tempovium.Media.Abstractions;
using Tempovium.Media.Abstractions.Contracts;
using Tempovium.Media.Abstractions.Enums;

namespace Tempovium.Media.Mac.Backends;

public sealed class MacMediaBackend : IMediaBackend, IMediaBackendInfo
{
    public MediaBackendKind BackendKind => MediaBackendKind.MacOSAvFoundation;
    public string DisplayName => "macOS AVFoundation (pendiente de implementación nativa)";

    public bool IsLoaded { get; private set; }
    public bool IsPlaying { get; private set; }

    public TimeSpan Duration => TimeSpan.Zero;
    public TimeSpan Position => TimeSpan.Zero;

    public event EventHandler? MediaOpened;
    public event EventHandler? MediaEnded;
    public event EventHandler<string>? MediaFailed;
    public event EventHandler<TimeSpan>? PositionChanged;

    public void Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            MediaFailed?.Invoke(this, "La ruta del medio está vacía.");
            return;
        }

        if (!File.Exists(path))
        {
            MediaFailed?.Invoke(this, $"No existe el archivo: {path}");
            return;
        }

        IsLoaded = true;
        IsPlaying = false;
        MediaOpened?.Invoke(this, EventArgs.Empty);
    }

    public void Play()
    {
        if (!IsLoaded)
        {
            MediaFailed?.Invoke(this, "No hay medio cargado.");
            return;
        }

        IsPlaying = true;
    }

    public void Pause()
    {
        if (!IsLoaded)
        {
            return;
        }

        IsPlaying = false;
    }

    public void Stop()
    {
        if (!IsLoaded)
        {
            return;
        }

        IsPlaying = false;
        PositionChanged?.Invoke(this, TimeSpan.Zero);
    }

    public void Seek(TimeSpan position)
    {
        if (!IsLoaded)
        {
            return;
        }

        PositionChanged?.Invoke(this, position);
    }

    public void SetVolume(double volume)
    {
        // Stub temporal. La implementación real llegará con AVFoundation.
    }

    public void Dispose()
    {
        // Stub temporal. La liberación real llegará con la librería nativa.
    }
}