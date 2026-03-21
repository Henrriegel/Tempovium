using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LibMPVSharp;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.Core.Entities;
using Tempovium.Core.Services;
using Tempovium.Services;
using Tempovium.ViewModels;

namespace Tempovium.Controls;

public partial class MediaPlayerView : UserControl
{
    private readonly SelectedMediaService _selectedMediaService;
    private readonly PlaybackTimelineService _timelineService;
    private readonly PlaybackControlService _playbackControlService;

    private readonly DispatcherTimer _timelineTimer;
    private readonly SemaphoreSlim _playerLock = new(1, 1);

    private MPVMediaPlayer? _player;
    private MediaItem? _pendingMedia;
    private bool _isAttached;
    private CancellationTokenSource? _loadMediaCts;

    public MediaPlayerViewModel ViewModel =>
        (MediaPlayerViewModel)DataContext!;

    public MediaPlayerView()
    {
        InitializeComponent();

        DataContext = Program.AppHost.Services.GetRequiredService<MediaPlayerViewModel>();
        _selectedMediaService = Program.AppHost.Services.GetRequiredService<SelectedMediaService>();
        _timelineService = Program.AppHost.Services.GetRequiredService<PlaybackTimelineService>();
        _playbackControlService = Program.AppHost.Services.GetRequiredService<PlaybackControlService>();

        _timelineTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _timelineTimer.Tick += OnTimelineTimerTick;

        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        try
        {
            _isAttached = true;

            if (_player == null)
            {
                _player = new MPVMediaPlayer();
                VideoPlayer.MediaPlayer = _player;
            }

            _selectedMediaService.PropertyChanged -= OnSelectedMediaChanged;
            _selectedMediaService.PropertyChanged += OnSelectedMediaChanged;

            _playbackControlService.SeekRequested -= OnSeekRequested;
            _playbackControlService.SeekRequested += OnSeekRequested;

            ViewModel.BackendStatus = "Backend activo: mpv embebido";
            ViewModel.NativeHostDebugText = "MPV inicializado correctamente.";

            EnsureTimerStarted();

            if (_selectedMediaService.SelectedMedia != null)
            {
                _pendingMedia = _selectedMediaService.SelectedMedia;
                QueueLoadSelectedMedia();
            }
        }
        catch (Exception ex)
        {
            ViewModel.IsLoading = false;
            ViewModel.PlayerStatusText = "Error al inicializar MPV";
            ViewModel.BackendStatus = "Backend no disponible: mpv embebido";
            ViewModel.NativeHostDebugText = $"MPV init error: {ex}";
        }
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;

        _selectedMediaService.PropertyChanged -= OnSelectedMediaChanged;
        _playbackControlService.SeekRequested -= OnSeekRequested;

        CancelPendingLoad();
        StopTimer();

        try
        {
            VideoPlayer.MediaPlayer = null;

            if (_player is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch
        {
        }
        finally
        {
            _player = null;
        }
    }

    private void OnSelectedMediaChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SelectedMediaService.SelectedMedia))
            return;

        _pendingMedia = _selectedMediaService.SelectedMedia;
        QueueLoadSelectedMedia();
    }

    private void QueueLoadSelectedMedia()
    {
        CancelPendingLoad();

        var cts = new CancellationTokenSource();
        _loadMediaCts = cts;

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await LoadPendingSelectedMediaAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Esperado si el usuario cambia rápido de medio.
            }
        });
    }

    private void CancelPendingLoad()
    {
        try
        {
            _loadMediaCts?.Cancel();
            _loadMediaCts?.Dispose();
        }
        catch
        {
        }
        finally
        {
            _loadMediaCts = null;
        }
    }

    private async Task LoadPendingSelectedMediaAsync(CancellationToken cancellationToken)
    {
        if (!_isAttached || _player == null)
            return;

        var media = _pendingMedia;
        if (media == null || string.IsNullOrWhiteSpace(media.FilePath))
            return;

        ViewModel.IsLoading = true;
        ViewModel.PlayerStatusText = "Cargando medio...";
        _timelineService.Reset();

        await Task.Delay(120, cancellationToken);

        media = _selectedMediaService.SelectedMedia;
        if (media == null || string.IsNullOrWhiteSpace(media.FilePath))
            return;

        await _playerLock.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _pendingMedia = null;

            _player.ExecuteCommand(new[]
            {
                "loadfile",
                media.FilePath,
                "replace"
            });

            ViewModel.IsLoading = false;
            ViewModel.PlayerStatusText = "Reproduciendo";
            ViewModel.BackendStatus = "Backend activo: mpv embebido";
            ViewModel.NativeHostDebugText = $"Archivo enviado a MPV: {media.FilePath}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ViewModel.IsLoading = false;
            ViewModel.PlayerStatusText = "Error al cargar el medio";
            ViewModel.BackendStatus = "Backend no disponible: mpv embebido";
            ViewModel.NativeHostDebugText = $"MPV load error: {ex}";
        }
        finally
        {
            _playerLock.Release();
        }
    }

    private async void OnSeekRequested(double seconds)
    {
        if (_player == null)
            return;

        try
        {
            await _playerLock.WaitAsync();

            _player.ExecuteCommand(new[]
            {
                "seek",
                seconds.ToString(CultureInfo.InvariantCulture),
                "absolute"
            });

            _timelineService.PositionSeconds = seconds;
            ViewModel.PlayerStatusText = "Reproduciendo";
        }
        catch (Exception ex)
        {
            ViewModel.NativeHostDebugText = $"MPV seek error: {ex}";
        }
        finally
        {
            _playerLock.Release();
        }
    }

    private void OnTimelineTimerTick(object? sender, EventArgs e)
    {
        if (_player == null)
            return;

        if (!_playerLock.Wait(0))
            return;

        try
        {
            var duration = _player.GetPropertyDouble("duration");
            if (duration > 0)
            {
                _timelineService.DurationSeconds = duration;
            }

            var position = _player.GetPropertyDouble("time-pos");
            if (position >= 0)
            {
                _timelineService.PositionSeconds = position;
            }

            var isPaused = _player.GetPropertyBoolean("pause");
            _timelineService.IsPlaying = !isPaused;

            if (_timelineService.DurationSeconds > 0)
            {
                ViewModel.IsLoading = false;
            }

            ViewModel.PlayerStatusText = isPaused ? "Pausado" : "Reproduciendo";
        }
        catch
        {
            // Ignorar mientras mpv aún no devuelve propiedades válidas.
        }
        finally
        {
            _playerLock.Release();
        }
    }

    private void EnsureTimerStarted()
    {
        if (!_timelineTimer.IsEnabled)
        {
            _timelineTimer.Start();
        }
    }

    private void StopTimer()
    {
        if (_timelineTimer.IsEnabled)
        {
            _timelineTimer.Stop();
        }
    }
}