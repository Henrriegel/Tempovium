using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Tempovium.Core.Entities;
using Tempovium.Core.Services;
using Tempovium.Media.Abstractions.Contracts;
using Tempovium.Media.Mac.Backends;
using Tempovium.Services;

namespace Tempovium.ViewModels;

public class MediaPlayerViewModel : ViewModelBase
{
    public MacMediaBackend? MacBackend => _mediaBackend as MacMediaBackend;

    private readonly SelectedMediaService _selectedMediaService;
    private readonly IMediaBackend _mediaBackend;
    private readonly PlaybackTimelineService _timelineService;

    private CancellationTokenSource? _playbackLoopCts;
    private CancellationTokenSource? _seekSettleCts;

    private string _backendStatus;
    private string _nativeHostDebugText = "Host nativo aún no inicializado";
    private string _currentMediaTitle = "Ningún medio seleccionado";
    private string _currentMediaPath = "Sin ruta";
    private string _currentMediaType = "Sin tipo";
    private bool _hasSelectedMedia;
    private bool _isLoading;
    private string _playerStatusText = "Sin reproducción activa";

    private bool _suspendPlaybackPolling;

    public LibraryViewModel.SimpleCommand PlayCommand { get; }
    public LibraryViewModel.SimpleCommand PauseCommand { get; }

    public MediaPlayerViewModel(
        SelectedMediaService selectedMediaService,
        IMediaBackend mediaBackend,
        PlaybackTimelineService timelineService)
    {
        _selectedMediaService = selectedMediaService;
        _mediaBackend = mediaBackend;
        _timelineService = timelineService;

        PlayCommand = new LibraryViewModel.SimpleCommand(Play);
        PauseCommand = new LibraryViewModel.SimpleCommand(Pause);

        if (mediaBackend is IMediaBackendInfo backendInfo)
        {
            _backendStatus = $"Backend activo: {backendInfo.DisplayName}";
        }
        else
        {
            _backendStatus = "Backend activo: desconocido";
        }

        _selectedMediaService.PropertyChanged += OnSelectedMediaChanged;
        _timelineService.PropertyChanged += OnTimelineServicePropertyChanged;

        _mediaBackend.MediaOpened += OnMediaOpened;
        _mediaBackend.MediaFailed += OnMediaFailed;

        ApplySelectedMedia(_selectedMediaService.SelectedMedia);
    }

    public string BackendStatus
    {
        get => _backendStatus;
        set => SetProperty(ref _backendStatus, value);
    }

    public string CurrentMediaTitle
    {
        get => _currentMediaTitle;
        set
        {
            if (_currentMediaTitle == value)
            {
                return;
            }

            _currentMediaTitle = value;
            OnPropertyChanged();
        }
    }

    public string CurrentMediaPath
    {
        get => _currentMediaPath;
        set
        {
            if (_currentMediaPath == value)
            {
                return;
            }

            _currentMediaPath = value;
            OnPropertyChanged();
        }
    }

    public string CurrentMediaType
    {
        get => _currentMediaType;
        set
        {
            if (_currentMediaType == value)
            {
                return;
            }

            _currentMediaType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAudio));
            OnPropertyChanged(nameof(IsVideo));
            OnPropertyChanged(nameof(IsNotAudio));
            OnPropertyChanged(nameof(IsNotVideo));
            OnPropertyChanged(nameof(ShowNativeVideoHost));
            OnPropertyChanged(nameof(ShowAudioTransportControls));
        }
    }

    public bool HasSelectedMedia
    {
        get => _hasSelectedMedia;
        set
        {
            if (_hasSelectedMedia == value)
            {
                return;
            }

            _hasSelectedMedia = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNoSelectedMedia));
        }
    }

    public bool HasNoSelectedMedia => !HasSelectedMedia;

    public bool IsAudio => string.Equals(CurrentMediaType, "Audio", StringComparison.OrdinalIgnoreCase);

    public bool IsVideo => string.Equals(CurrentMediaType, "Video", StringComparison.OrdinalIgnoreCase);

    public bool IsNotAudio => !IsAudio;

    public bool IsNotVideo => !IsVideo;

    public bool ShowNativeVideoHost => HasSelectedMedia && IsVideo;

    public bool ShowAudioTransportControls => HasSelectedMedia && IsAudio;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string PlayerStatusText
    {
        get => _playerStatusText;
        set
        {
            if (_playerStatusText == value)
            {
                return;
            }

            _playerStatusText = value;
            OnPropertyChanged();
        }
    }

    public double PositionSeconds => _timelineService.DisplayPositionSeconds;

    public double DurationSeconds => _timelineService.DurationSeconds;

    public bool IsPlaying => _timelineService.IsPlaying;
    public bool IsPaused => !IsPlaying;

    public bool IsUserSeeking => _timelineService.IsUserSeeking;

    private void OnTimelineServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaybackTimelineService.DisplayPositionSeconds))
        {
            OnPropertyChanged(nameof(PositionSeconds));
        }

        if (e.PropertyName == nameof(PlaybackTimelineService.DurationSeconds))
        {
            OnPropertyChanged(nameof(DurationSeconds));
        }

        if (e.PropertyName == nameof(PlaybackTimelineService.IsPlaying))
        {
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(IsPaused));
        }

        if (e.PropertyName == nameof(PlaybackTimelineService.IsUserSeeking))
        {
            OnPropertyChanged(nameof(IsUserSeeking));
        }
    }

    private void OnMediaOpened(object? sender, EventArgs e)
    {
        Console.WriteLine($"[VM] MediaOpened -> pos={_mediaBackend.Position.TotalSeconds:F2}, dur={_mediaBackend.Duration.TotalSeconds:F2}, playing={_mediaBackend.IsPlaying}");

        _timelineService.ApplyBackendState(
            _mediaBackend.Position.TotalSeconds,
            _mediaBackend.Duration.TotalSeconds,
            _mediaBackend.IsPlaying);

        if (IsVideo && !_mediaBackend.IsPlaying)
        {
            _mediaBackend.Play();
            _timelineService.SetPlaybackStateOnly(true);
            PlayerStatusText = "Reproduciendo";
        }
        else if (IsAudio)
        {
            PlayerStatusText = "Listo para reproducir";
        }

        IsLoading = false;
    }

    private void OnMediaFailed(object? sender, string message)
    {
        Console.WriteLine($"[VM] MediaFailed -> {message}");

        _timelineService.Reset();
        IsLoading = false;
        PlayerStatusText = $"Error: {message}";
    }

    private void OnSelectedMediaChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedMediaService.SelectedMedia))
        {
            ApplySelectedMedia(_selectedMediaService.SelectedMedia);
        }
    }

    private void ApplySelectedMedia(MediaItem? media)
    {
        if (media is null)
        {
            _playbackLoopCts?.Cancel();
            _playbackLoopCts?.Dispose();
            _playbackLoopCts = null;

            _seekSettleCts?.Cancel();
            _seekSettleCts?.Dispose();
            _seekSettleCts = null;

            _suspendPlaybackPolling = false;

            HasSelectedMedia = false;
            CurrentMediaTitle = "Ningún medio seleccionado";
            CurrentMediaPath = "Sin ruta";
            CurrentMediaType = "Sin tipo";
            IsLoading = false;
            PlayerStatusText = "Sin reproducción activa";
            _timelineService.Reset();

            OnPropertyChanged(nameof(ShowNativeVideoHost));
            OnPropertyChanged(nameof(ShowAudioTransportControls));
            return;
        }

        HasSelectedMedia = true;
        CurrentMediaTitle = media.Title;
        CurrentMediaPath = media.FilePath;
        CurrentMediaType = media.MediaType.ToString();
        IsLoading = true;
        PlayerStatusText = "Cargando...";

        _seekSettleCts?.Cancel();
        _seekSettleCts?.Dispose();
        _seekSettleCts = null;
        _suspendPlaybackPolling = false;

        Console.WriteLine($"[VM] ApplySelectedMedia -> title={media.Title}, type={media.MediaType}, path={media.FilePath}");

        try
        {
            _mediaBackend.Load(media.FilePath);

            _timelineService.ApplyBackendState(
                _mediaBackend.Position.TotalSeconds,
                _mediaBackend.Duration.TotalSeconds,
                _mediaBackend.IsPlaying);
        }
        catch (Exception ex)
        {
            _timelineService.Reset();
            IsLoading = false;
            PlayerStatusText = $"Error al cargar: {ex.Message}";
        }

        RestartPlaybackLoop();
    }

    public string NativeHostDebugText
    {
        get => _nativeHostDebugText;
        set => SetProperty(ref _nativeHostDebugText, value);
    }

    public void SetNativeHostDebugInfo(string descriptor, nint handle)
    {
        NativeHostDebugText = $"Host nativo listo: {descriptor} | Handle: {handle}";
    }

    private void RestartPlaybackLoop()
    {
        _playbackLoopCts?.Cancel();
        _playbackLoopCts?.Dispose();

        _playbackLoopCts = new CancellationTokenSource();
        _ = StartPlaybackLoopAsync(_playbackLoopCts.Token);
    }

    private async Task StartPlaybackLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200, cancellationToken);

                if (_suspendPlaybackPolling)
                {
                    Console.WriteLine($"[VM] Poll skipped -> display={_timelineService.DisplayPositionSeconds:F2}, userSeeking={_timelineService.IsUserSeeking}");
                    continue;
                }

                _mediaBackend.UpdateState();

                Console.WriteLine($"[VM] Poll apply -> backendPos={_mediaBackend.Position.TotalSeconds:F2}, backendDur={_mediaBackend.Duration.TotalSeconds:F2}, playing={_mediaBackend.IsPlaying}, displayBefore={_timelineService.DisplayPositionSeconds:F2}");

                _timelineService.ApplyBackendState(
                    _mediaBackend.Position.TotalSeconds,
                    _mediaBackend.Duration.TotalSeconds,
                    _mediaBackend.IsPlaying);

                Console.WriteLine($"[VM] Poll applied -> displayAfter={_timelineService.DisplayPositionSeconds:F2}, seekPending={_timelineService.IsSeekPending}");
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void BeginUserSeek()
    {
        Console.WriteLine($"[VM] BeginUserSeek -> displayBefore={_timelineService.DisplayPositionSeconds:F2}");

        _seekSettleCts?.Cancel();
        _seekSettleCts?.Dispose();
        _seekSettleCts = null;

        _suspendPlaybackPolling = true;
        _timelineService.BeginUserSeek();
    }

    public void UpdateUserSeek(double seconds)
    {
        Console.WriteLine($"[VM] UpdateUserSeek -> seconds={seconds:F2}");
        _timelineService.UpdateUserSeek(seconds);
    }

    public Task SeekToAsync(double seconds)
    {
        var targetSeconds = seconds;

        if (targetSeconds < 0)
        {
            targetSeconds = 0;
        }

        if (_timelineService.DurationSeconds > 0 && targetSeconds > _timelineService.DurationSeconds)
        {
            targetSeconds = _timelineService.DurationSeconds;
        }

        if (!_timelineService.IsUserSeeking)
        {
            _timelineService.BeginUserSeek();
        }

        Console.WriteLine($"[VM] SeekToAsync(start) -> requested={seconds:F2}, clamped={targetSeconds:F2}, displayBefore={_timelineService.DisplayPositionSeconds:F2}");

        _timelineService.UpdateUserSeek(targetSeconds);
        var committedTarget = _timelineService.CommitUserSeek();

        Console.WriteLine($"[VM] SeekToAsync(commit) -> committed={committedTarget:F2}, seekPending={_timelineService.IsSeekPending}");

        _mediaBackend.Seek(TimeSpan.FromSeconds(committedTarget));

        _seekSettleCts?.Cancel();
        _seekSettleCts?.Dispose();
        _seekSettleCts = new CancellationTokenSource();

        _ = ResumePollingAfterSeekAsync(_seekSettleCts.Token);

        return Task.CompletedTask;
    }

    private async Task ResumePollingAfterSeekAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(450, cancellationToken);

            Console.WriteLine("[VM] ResumePollingAfterSeek -> delay finished, forcing one UpdateState");

            _mediaBackend.UpdateState();

            Console.WriteLine($"[VM] ResumePollingAfterSeek -> backendPos={_mediaBackend.Position.TotalSeconds:F2}, backendDur={_mediaBackend.Duration.TotalSeconds:F2}, displayBefore={_timelineService.DisplayPositionSeconds:F2}");

            _timelineService.ApplyBackendState(
                _mediaBackend.Position.TotalSeconds,
                _mediaBackend.Duration.TotalSeconds,
                _mediaBackend.IsPlaying);

            Console.WriteLine($"[VM] ResumePollingAfterSeek -> displayAfter={_timelineService.DisplayPositionSeconds:F2}, seekPending={_timelineService.IsSeekPending}");
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("[VM] ResumePollingAfterSeek -> cancelled");
            return;
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _suspendPlaybackPolling = false;
                Console.WriteLine("[VM] ResumePollingAfterSeek -> polling resumed");
            }
        }
    }

    public void Play()
    {
        if (!_mediaBackend.IsLoaded)
        {
            return;
        }

        Console.WriteLine("[VM] Play() desde UI");

        _mediaBackend.Play();
        _timelineService.SetPlaybackStateOnly(true);
        PlayerStatusText = "Reproduciendo";
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsPaused));
    }

    public void Pause()
    {
        if (!_mediaBackend.IsLoaded)
        {
            return;
        }

        Console.WriteLine("[VM] Pause() desde UI");

        _mediaBackend.Pause();
        _timelineService.SetPlaybackStateOnly(false);
        PlayerStatusText = "Pausado";
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsPaused));
    }
}