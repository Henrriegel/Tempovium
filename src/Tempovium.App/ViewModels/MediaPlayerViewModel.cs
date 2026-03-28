using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Tempovium.Core.Entities;
using Tempovium.Core.Services;
using Tempovium.Services;
using Tempovium.Media.Abstractions.Contracts;
using Tempovium.Media.Mac.Backends;

namespace Tempovium.ViewModels;

public class MediaPlayerViewModel : ViewModelBase
{
    public MacMediaBackend? MacBackend => _mediaBackend as MacMediaBackend;

    private readonly SelectedMediaService _selectedMediaService;
    private readonly IMediaBackend _mediaBackend;
    private readonly PlaybackTimelineService _timelineService;

    private CancellationTokenSource? _playbackLoopCts;

    private double _positionSeconds;

    private string _backendStatus;
    private string _nativeHostDebugText = "Host nativo aún no inicializado";
    private string _currentMediaTitle = "Ningún medio seleccionado";
    private string _currentMediaPath = "Sin ruta";
    private string _currentMediaType = "Sin tipo";
    private bool _hasSelectedMedia;
    private bool _isLoading;
    private string _playerStatusText = "Sin reproducción activa";

    private bool _suspendTimelineSync;

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

        _timelineService.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(DurationSeconds));
            OnPropertyChanged(nameof(IsPlaying));
        };

        _mediaBackend.MediaOpened += OnMediaOpened;
        _mediaBackend.MediaFailed += OnMediaFailed;
        _mediaBackend.PositionChanged += OnBackendPositionChanged;

        ApplySelectedMedia(_selectedMediaService.SelectedMedia);
    }

    private void OnMediaOpened(object? sender, EventArgs e)
    {
        _timelineService.DurationSeconds = _mediaBackend.Duration.TotalSeconds;
        _timelineService.PositionSeconds = _mediaBackend.Position.TotalSeconds;
        _timelineService.IsPlaying = _mediaBackend.IsPlaying;

        PositionSeconds = _mediaBackend.Position.TotalSeconds;

        IsLoading = false;
    }

    private void OnMediaFailed(object? sender, string message)
    {
        _timelineService.Reset();
        IsLoading = false;
        PlayerStatusText = $"Error: {message}";
    }

    private void OnBackendPositionChanged(object? sender, TimeSpan position)
    {
        if (_suspendTimelineSync)
            return;

        var seconds = position.TotalSeconds;
        _timelineService.PositionSeconds = seconds;
        PositionSeconds = seconds;
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
                return;

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
                return;

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
                return;

            _currentMediaType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAudio));
            OnPropertyChanged(nameof(IsVideo));
            OnPropertyChanged(nameof(IsNotAudio));
            OnPropertyChanged(nameof(IsNotVideo));
        }
    }

    public bool HasSelectedMedia
    {
        get => _hasSelectedMedia;
        set
        {
            if (_hasSelectedMedia == value)
                return;

            _hasSelectedMedia = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNoSelectedMedia));
        }
    }

    public bool HasNoSelectedMedia => !HasSelectedMedia;

    public bool IsAudio =>
        string.Equals(CurrentMediaType, "Audio", StringComparison.OrdinalIgnoreCase);

    public bool IsVideo =>
        string.Equals(CurrentMediaType, "Video", StringComparison.OrdinalIgnoreCase);

    public bool IsNotAudio => !IsAudio;
    public bool IsNotVideo => !IsVideo;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value)
                return;

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
                return;

            _playerStatusText = value;
            OnPropertyChanged();
        }
    }

    public double PositionSeconds
    {
        get => _positionSeconds;
        set
        {
            if (Math.Abs(_positionSeconds - value) < 0.01)
                return;

            _positionSeconds = value;
            OnPropertyChanged();
        }
    }

    public double DurationSeconds => _timelineService.DurationSeconds;
    public bool IsPlaying => _timelineService.IsPlaying;

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

            HasSelectedMedia = false;
            CurrentMediaTitle = "Ningún medio seleccionado";
            CurrentMediaPath = "Sin ruta";
            CurrentMediaType = "Sin tipo";
            IsLoading = false;
            PlayerStatusText = "Sin reproducción activa";
            _timelineService.Reset();
            PositionSeconds = 0;
            _suspendTimelineSync = false;
            return;
        }

        HasSelectedMedia = true;
        CurrentMediaTitle = media.Title;
        CurrentMediaPath = media.FilePath;
        CurrentMediaType = media.MediaType.ToString();
        IsLoading = true;

        try
        {
            _mediaBackend.Load(media.FilePath);

            if (string.Equals(media.MediaType.ToString(), "Video", StringComparison.OrdinalIgnoreCase))
            {
                _mediaBackend.Play();
                PlayerStatusText = "Reproduciendo";
            }
            else
            {
                PlayerStatusText = "Listo para reproducir";
            }

            _timelineService.DurationSeconds = _mediaBackend.Duration.TotalSeconds;
            _timelineService.PositionSeconds = _mediaBackend.Position.TotalSeconds;
            _timelineService.IsPlaying = _mediaBackend.IsPlaying;

            PositionSeconds = _mediaBackend.Position.TotalSeconds;
            IsLoading = false;
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

                _mediaBackend.UpdateState();

                var backendPosition = _mediaBackend.Position.TotalSeconds;
                var backendDuration = _mediaBackend.Duration.TotalSeconds;

                _timelineService.DurationSeconds = backendDuration;

                if (!_suspendTimelineSync)
                {
                    _timelineService.PositionSeconds = backendPosition;
                    PositionSeconds = backendPosition;
                }

                OnPropertyChanged(nameof(DurationSeconds));
                OnPropertyChanged(nameof(IsPlaying));
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    public async Task SeekToAsync(double seconds)
    {
        if (seconds < 0)
            seconds = 0;

        _mediaBackend.Seek(TimeSpan.FromSeconds(seconds));

        _timelineService.PositionSeconds = seconds;
        PositionSeconds = seconds;

        await Task.Delay(500);

        _mediaBackend.UpdateState();

        _timelineService.PositionSeconds = _mediaBackend.Position.TotalSeconds;
        PositionSeconds = _mediaBackend.Position.TotalSeconds;

        _suspendTimelineSync = false;
    }

    public void Play()
    {
        if (!_mediaBackend.IsLoaded)
            return;

        Console.WriteLine("[MediaPlayerVM] Play() desde UI");

        _mediaBackend.Play();
        _timelineService.IsPlaying = true;
        PlayerStatusText = "Reproduciendo";

        OnPropertyChanged(nameof(IsPlaying));
    }

    public void Pause()
    {
        if (!_mediaBackend.IsLoaded)
            return;

        Console.WriteLine("[MediaPlayerVM] Pause() desde UI");

        _mediaBackend.Pause();
        _timelineService.IsPlaying = false;
        PlayerStatusText = "Pausado";

        OnPropertyChanged(nameof(IsPlaying));
    }
    
    public void BeginUserSeek()
    {
        _suspendTimelineSync = true;
    }
}