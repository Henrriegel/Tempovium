using Tempovium.Media.Abstractions.Contracts;
using Tempovium.Media.Abstractions.Enums;
using Tempovium.Media.Mac.Interop;

namespace Tempovium.Media.Mac.Backends;

public sealed class MacMediaBackend : IMediaBackend, IMediaBackendInfo
{
    private readonly nint _handle;
    private bool _disposed;
    private bool _isLoaded;
    private bool _isPlaying;
    private bool _hasRaisedMediaOpened;

    public MacMediaBackend()
    {
        _handle = MacNative.Create();

        if (_handle == 0)
        {
            throw new InvalidOperationException("No se pudo crear el reproductor nativo de macOS.");
        }
    }

    public MediaBackendKind BackendKind => MediaBackendKind.MacOSAvFoundation;

    public string DisplayName => "macOS AVFoundation";

    public bool IsLoaded => _isLoaded;

    public bool IsPlaying => _isPlaying;

    public TimeSpan Duration { get; private set; } = TimeSpan.Zero;

    public TimeSpan Position { get; private set; } = TimeSpan.Zero;

    public event EventHandler? MediaOpened;
    public event EventHandler? MediaEnded;
    public event EventHandler<string>? MediaFailed;
    public event EventHandler<TimeSpan>? PositionChanged;

    public void Load(string path)
    {
        Console.WriteLine($"[MacMediaBackend] Load -> {path}");

        ThrowIfDisposed();

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

        var result = MacNative.LoadFile(_handle, path);
        if (result == 0)
        {
            MediaFailed?.Invoke(this, $"No se pudo cargar el archivo nativo: {path}");
            return;
        }

        Position = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        _isLoaded = true;
        _isPlaying = false;
        _hasRaisedMediaOpened = false;

        PositionChanged?.Invoke(this, TimeSpan.Zero);
    }

    public void Play()
    {
        ThrowIfDisposed();

        if (!_isLoaded)
        {
            MediaFailed?.Invoke(this, "No hay medio cargado.");
            return;
        }

        Console.WriteLine("[MacMediaBackend] Play()");
        MacNative.Play(_handle);
        _isPlaying = true;
    }

    public void Pause()
    {
        ThrowIfDisposed();

        if (!_isLoaded)
        {
            return;
        }

        Console.WriteLine("[MacMediaBackend] Pause()");
        MacNative.Pause(_handle);
        _isPlaying = false;
    }

    public void Stop()
    {
        ThrowIfDisposed();

        if (!_isLoaded)
        {
            return;
        }

        Console.WriteLine("[MacMediaBackend] Stop()");
        MacNative.Pause(_handle);
        _isPlaying = false;
        PositionChanged?.Invoke(this, TimeSpan.Zero);
    }

    public void SetVolume(double volume)
    {
        ThrowIfDisposed();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public nint GetNativeViewHandle()
    {
        ThrowIfDisposed();
        return MacNative.GetView(_handle);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        MacNative.Destroy(_handle);
        _disposed = true;
    }

    public void UpdateState()
    {
        if (_handle == IntPtr.Zero)
        {
            return;
        }

        MacNative.tpv_mac_player_get_state(_handle, out var position, out var duration, out var isReady);

        Position = TimeSpan.FromSeconds(position);
        Duration = TimeSpan.FromSeconds(duration);

        Console.WriteLine($"[MacMediaBackend] UpdateState -> pos={Position.TotalSeconds:F2}, dur={Duration.TotalSeconds:F2}, ready={isReady}, loaded={_isLoaded}, openedRaised={_hasRaisedMediaOpened}");

        if (!_hasRaisedMediaOpened && _isLoaded && isReady == 1)
        {
            _hasRaisedMediaOpened = true;
            Console.WriteLine("[MacMediaBackend] MediaOpened raised");
            MediaOpened?.Invoke(this, EventArgs.Empty);
        }

        PositionChanged?.Invoke(this, Position);
    }

    public void Seek(TimeSpan position)
    {
        ThrowIfDisposed();

        if (!_isLoaded || _handle == IntPtr.Zero)
        {
            return;
        }

        Console.WriteLine($"[MacMediaBackend] Seek -> requested={position.TotalSeconds:F2}");
        MacNative.tpv_mac_player_seek(_handle, position.TotalSeconds);
    }
}