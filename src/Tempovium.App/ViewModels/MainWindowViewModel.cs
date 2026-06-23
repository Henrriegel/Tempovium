using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Services;
using Tempovium.Services;

namespace Tempovium.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly UserSessionService _userSessionService;
    private readonly SelectedMediaService _selectedMediaService;
    private readonly IUserRepository _userRepository;
    private readonly UiSettingsStore _uiSettingsStore;
    private UiSettings _uiSettings;
    private bool _isSettingsOpen;
    private bool _isImporting;
    private int _themeSelectedIndex;
    private Bitmap? _currentUserAvatarImage;
    private string _importOverlayTitle = "Importando medios";
    private string _importOverlayMessage = "La biblioteca se actualizará al terminar.";
    private string _themeStatus = "Tema: sistema";

    public MainWindowViewModel(
        NavigationService navigationService,
        UserSessionService userSessionService,
        SelectedMediaService selectedMediaService,
        IUserRepository userRepository,
        UiSettingsStore uiSettingsStore)
    {
        NavigationService = navigationService;
        _userSessionService = userSessionService;
        _selectedMediaService = selectedMediaService;
        _userRepository = userRepository;
        _uiSettingsStore = uiSettingsStore;
        _uiSettings = _uiSettingsStore.Load();

        SwitchAccountCommand = new SimpleCommand(_ => ReturnToLogin());
        OpenSettingsCommand = new SimpleCommand(_ => IsSettingsOpen = true);
        CloseSettingsCommand = new SimpleCommand(_ => IsSettingsOpen = false);
        ChangeAvatarCommand = new SimpleCommand(_ => ChangeAvatar());

        _userSessionService.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsLoggedIn));
            OnCurrentProfileChanged();
        };

        ApplyTheme(_uiSettings.ThemePreference, save: false);
        OnCurrentProfileChanged();
    }

    public NavigationService NavigationService { get; }

    public bool IsLoggedIn => _userSessionService.IsLoggedIn;

    public string CurrentUsername => _userSessionService.CurrentUser?.Username ?? "Sin usuario";

    public string CurrentUserInitials => string.IsNullOrWhiteSpace(_userSessionService.CurrentUser?.Username)
        ? "?"
        : _userSessionService.CurrentUser.Username[..1].ToUpperInvariant();

    public Bitmap? CurrentUserAvatarImage
    {
        get => _currentUserAvatarImage;
        private set => SetProperty(ref _currentUserAvatarImage, value);
    }

    public bool HasCurrentUserAvatar => _currentUserAvatarImage is not null;

    public bool HasNoCurrentUserAvatar => _currentUserAvatarImage is null;

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    public bool IsImporting
    {
        get => _isImporting;
        private set => SetProperty(ref _isImporting, value);
    }

    public string ImportOverlayTitle
    {
        get => _importOverlayTitle;
        private set => SetProperty(ref _importOverlayTitle, value);
    }

    public string ImportOverlayMessage
    {
        get => _importOverlayMessage;
        private set => SetProperty(ref _importOverlayMessage, value);
    }

    public string ThemeStatus
    {
        get => _themeStatus;
        private set => SetProperty(ref _themeStatus, value);
    }

    public int ThemeSelectedIndex
    {
        get => _themeSelectedIndex;
        set
        {
            if (SetProperty(ref _themeSelectedIndex, value))
            {
                ApplyTheme(value switch
                {
                    1 => "Light",
                    2 => "Dark",
                    _ => "Default"
                }, save: true);
            }
        }
    }

    public ICommand SwitchAccountCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand CloseSettingsCommand { get; }
    public ICommand ChangeAvatarCommand { get; }

    public void SetImporting(bool isImporting, string? title = null, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            ImportOverlayTitle = title;
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            ImportOverlayMessage = message;
        }

        IsImporting = isImporting;
    }

    private void ReturnToLogin()
    {
        _selectedMediaService.SelectedMedia = null;
        _userSessionService.Logout();
        IsSettingsOpen = false;
        NavigationService.CurrentView = Program.AppHost.Services.GetRequiredService<LoginViewModel>();
    }

    private async void ChangeAvatar()
    {
        var user = _userSessionService.CurrentUser;
        if (user == null)
        {
            return;
        }

        var topLevel = GetTopLevel();
        if (topLevel == null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona un avatar",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Imágenes")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"]
                }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        try
        {
            user.AvatarPath = AvatarStorage.CopyToManagedAvatar(files[0].Path.LocalPath, user.Id);
            await _userRepository.UpdateAsync(user);
            OnCurrentProfileChanged();
        }
        catch
        {
            // Keep the existing avatar/initials if the selected image cannot be copied.
        }
    }

    private void OnCurrentProfileChanged()
    {
        CurrentUserAvatarImage = AvatarStorage.LoadImage(_userSessionService.CurrentUser?.AvatarPath);
        OnPropertyChanged(nameof(CurrentUsername));
        OnPropertyChanged(nameof(CurrentUserInitials));
        OnPropertyChanged(nameof(HasCurrentUserAvatar));
        OnPropertyChanged(nameof(HasNoCurrentUserAvatar));
    }

    private static TopLevel? GetTopLevel()
    {
        return TopLevel.GetTopLevel(
            Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);
    }

    private void ApplyTheme(string? theme, bool save)
    {
        theme = theme is "Light" or "Dark"
            ? theme
            : "Default";

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        ThemeStatus = theme switch
        {
            "Light" => "Tema: claro",
            "Dark" => "Tema: oscuro",
            _ => "Tema: sistema"
        };

        var selectedIndex = theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        if (_themeSelectedIndex != selectedIndex)
        {
            _themeSelectedIndex = selectedIndex;
            OnPropertyChanged(nameof(ThemeSelectedIndex));
        }

        if (save)
        {
            _uiSettings.ThemePreference = theme;
            _uiSettingsStore.Save(_uiSettings);
        }
    }

    private sealed class SimpleCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public SimpleCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}
