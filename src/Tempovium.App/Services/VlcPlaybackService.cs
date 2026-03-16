using System;
using LibVLCSharp.Shared;

namespace Tempovium.Services;

public class VlcPlaybackService : IPlaybackService, IDisposable
{
    private const string MacLibPath = "/Applications/VLC.app/Contents/MacOS/lib";
    private const string MacPluginsPath = "/Applications/VLC.app/Contents/MacOS/plugins";

    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private bool _initializationAttempted;
    private bool _isAvailable;

    public bool IsAvailable
    {
        get
        {
            EnsureInitialized();
            return _isAvailable;
        }
    }

    public string BackendName => "LibVLC";

    public MediaPlayer? MediaPlayer
    {
        get
        {
            EnsureInitialized();
            return _mediaPlayer;
        }
    }

    public void Play(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        EnsureInitialized();

        if (!_isAvailable || _libVlc is null || _mediaPlayer is null)
            return;

        using var media = new Media(_libVlc, filePath, FromType.FromPath);
        _mediaPlayer.Play(media);
    }

    public void Stop()
    {
        EnsureInitialized();

        if (!_isAvailable || _mediaPlayer is null)
            return;

        _mediaPlayer.Stop();
    }

    private void EnsureInitialized()
    {
        if (_initializationAttempted)
            return;

        _initializationAttempted = true;

        try
        {
            Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", MacPluginsPath);
            LibVLCSharp.Shared.Core.Initialize(MacLibPath);

            _libVlc = new LibVLC(
                "--no-video-title-show"
            );

            _mediaPlayer = new MediaPlayer(_libVlc);
            _isAvailable = true;
        }
        catch (Exception ex)
        {
            _isAvailable = false;
            Console.WriteLine($"[Tempovium] VLC backend no disponible: {ex}");
        }
    }

    public void Dispose()
    {
        try
        {
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
        }
        catch
        {
            // Ignorar errores de dispose por ahora.
        }
    }
}