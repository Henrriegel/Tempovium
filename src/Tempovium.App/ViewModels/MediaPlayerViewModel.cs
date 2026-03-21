using System.ComponentModel;
using Tempovium.Core.Entities;
using Tempovium.Core.Services;
using Tempovium.Services;

namespace Tempovium.ViewModels;

public class MediaPlayerViewModel : ViewModelBase
{
    private readonly SelectedMediaService _selectedMediaService;
    private readonly IPlaybackService _playbackService;
    private readonly PlaybackTimelineService _timelineService;

    private string _backendStatus;
    private string _nativeHostDebugText = "Host nativo aún no inicializado";
    private string _currentMediaTitle = "Ningún medio seleccionado";
    private string _currentMediaPath = "Sin ruta";
    private string _currentMediaType = "Sin tipo";
    private bool _hasSelectedMedia;
    private bool _isLoading;
    private string _playerStatusText = "Sin reproducción activa";

    public MediaPlayerViewModel(
        SelectedMediaService selectedMediaService,
        IPlaybackService playbackService,
        PlaybackTimelineService timelineService)
    {
        _selectedMediaService = selectedMediaService;
        _playbackService = playbackService;
        _timelineService = timelineService;

        _backendStatus = playbackService.IsAvailable
            ? $"Backend activo: {playbackService.BackendName}"
            : $"Backend no disponible: {playbackService.BackendName}";

        _selectedMediaService.PropertyChanged += OnSelectedMediaChanged;

        _timelineService.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(PositionSeconds));
            OnPropertyChanged(nameof(DurationSeconds));
            OnPropertyChanged(nameof(IsPlaying));

            if (_timelineService.IsPlaying)
            {
                IsLoading = false;
                PlayerStatusText = "Reproduciendo";
            }
        };

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
        set => SetProperty(ref _currentMediaTitle, value);
    }

    public string CurrentMediaPath
    {
        get => _currentMediaPath;
        set => SetProperty(ref _currentMediaPath, value);
    }

    public string CurrentMediaType
    {
        get => _currentMediaType;
        set => SetProperty(ref _currentMediaType, value);
    }

    public bool HasSelectedMedia
    {
        get => _hasSelectedMedia;
        set => SetProperty(ref _hasSelectedMedia, value);
    }

    public bool HasNoSelectedMedia => !HasSelectedMedia;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }
    }
    
    public bool IsNotLoading => !IsLoading;

    public string PlayerStatusText
    {
        get => _playerStatusText;
        set => SetProperty(ref _playerStatusText, value);
    }

    public double PositionSeconds => _timelineService.PositionSeconds;
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
            HasSelectedMedia = false;
            CurrentMediaTitle = "Ningún medio seleccionado";
            CurrentMediaPath = "Sin ruta";
            CurrentMediaType = "Sin tipo";
            IsLoading = false;
            PlayerStatusText = "Sin reproducción activa";
            OnPropertyChanged(nameof(HasNoSelectedMedia));
            return;
        }

        HasSelectedMedia = true;
        CurrentMediaTitle = media.Title;
        CurrentMediaPath = media.FilePath;
        CurrentMediaType = media.MediaType.ToString();
        IsLoading = true;
        PlayerStatusText = "Cargando medio...";
        OnPropertyChanged(nameof(HasNoSelectedMedia));
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
}