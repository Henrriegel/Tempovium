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

        _isLoaded = true;
        _isPlaying = false;

        PositionChanged?.Invoke(this, TimeSpan.Zero);
        MediaOpened?.Invoke(this, EventArgs.Empty);
    }

    public void Play()
    {
        ThrowIfDisposed();

        if (!_isLoaded)
        {
            MediaFailed?.Invoke(this, "No hay medio cargado.");
            return;
        }

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

        MacNative.Pause(_handle);
        _isPlaying = false;
        PositionChanged?.Invoke(this, TimeSpan.Zero);
    }

    public void Seek(TimeSpan position)
    {
        ThrowIfDisposed();

        if (!_isLoaded)
        {
            return;
        }

        PositionChanged?.Invoke(this, position);
    }

    public void SetVolume(double volume)
    {
        ThrowIfDisposed();
        // Lo implementaremos después en el bridge Swift.
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
            return;

        MacNative.tpv_mac_player_get_state(_handle, out var position, out var duration);

        Position = TimeSpan.FromSeconds(position);
        Duration = TimeSpan.FromSeconds(duration);
    }
}