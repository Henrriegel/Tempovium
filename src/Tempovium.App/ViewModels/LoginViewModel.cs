using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Services;
using Tempovium.Services;

namespace Tempovium.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private static readonly FilePickerFileType AvatarFileType = new("Imágenes")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"]
    };

    private readonly IUserRepository _userRepository;
    private readonly UserSessionService _userSessionService;
    private readonly NavigationService _navigationService;

    private string _username = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isCreateProfileOpen;
    private string? _selectedAvatarSourcePath;
    private Bitmap? _selectedAvatarImage;
    private UserCardViewModel? _selectedUserCard;

    public LoginViewModel(
        IUserRepository userRepository,
        UserSessionService userSessionService,
        NavigationService navigationService)
    {
        _userRepository = userRepository;
        _userSessionService = userSessionService;
        _navigationService = navigationService;

        ShowCreateProfileCommand = new SimpleCommand(_ => StartCreateProfile());
        BackToProfilesCommand = new SimpleCommand(_ => IsCreateProfileOpen = false);
        CreateProfileCommand = new SimpleCommand(_ => CreateProfile());
        PickAvatarCommand = new SimpleCommand(_ => PickAvatar());

        _ = LoadUsersAsync();
    }

    public ObservableCollection<UserCardViewModel> LocalUsers { get; } = [];

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsCreateProfileOpen
    {
        get => _isCreateProfileOpen;
        set
        {
            if (SetProperty(ref _isCreateProfileOpen, value))
            {
                OnPropertyChanged(nameof(IsProfileListVisible));
                OnPropertyChanged(nameof(IsCreateProfileVisible));
                OnPropertyChanged(nameof(CanGoBackToProfiles));
            }
        }
    }

    public UserCardViewModel? SelectedUserCard
    {
        get => _selectedUserCard;
        set
        {
            if (!SetProperty(ref _selectedUserCard, value) || value is null)
            {
                return;
            }

            _ = EnterProfileAsync(value);
        }
    }

    public Bitmap? SelectedAvatarImage
    {
        get => _selectedAvatarImage;
        private set
        {
            if (SetProperty(ref _selectedAvatarImage, value))
            {
                OnPropertyChanged(nameof(HasSelectedAvatar));
                OnPropertyChanged(nameof(HasNoSelectedAvatar));
            }
        }
    }

    public bool HasLocalUsers => LocalUsers.Count > 0;

    public bool HasNoLocalUsers => !HasLocalUsers;

    public bool IsProfileListVisible => HasLocalUsers && !IsCreateProfileOpen;

    public bool IsCreateProfileVisible => IsCreateProfileOpen || HasNoLocalUsers;

    public bool CanGoBackToProfiles => HasLocalUsers && IsCreateProfileOpen;

    public bool HasSelectedAvatar => SelectedAvatarImage is not null;

    public bool HasNoSelectedAvatar => SelectedAvatarImage is null;

    public string SelectedAvatarText => string.IsNullOrWhiteSpace(_selectedAvatarSourcePath)
        ? "Sin avatar seleccionado"
        : Path.GetFileName(_selectedAvatarSourcePath);

    public ICommand ShowCreateProfileCommand { get; }

    public ICommand BackToProfilesCommand { get; }

    public ICommand CreateProfileCommand { get; }

    public ICommand PickAvatarCommand { get; }

    private async Task LoadUsersAsync()
    {
        LocalUsers.Clear();
        var users = await _userRepository.GetAllAsync();

        foreach (var user in users)
        {
            LocalUsers.Add(new UserCardViewModel(user));
        }

        IsCreateProfileOpen = LocalUsers.Count == 0;
        OnProfileListChanged();
    }

    private void StartCreateProfile()
    {
        SelectedUserCard = null;
        Username = string.Empty;
        _selectedAvatarSourcePath = null;
        SelectedAvatarImage = null;
        OnPropertyChanged(nameof(SelectedAvatarText));
        StatusMessage = string.Empty;
        IsCreateProfileOpen = true;
    }

    private async void PickAvatar()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null)
        {
            StatusMessage = "No se pudo abrir el selector de avatar.";
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona un avatar",
            AllowMultiple = false,
            FileTypeFilter = [AvatarFileType]
        });

        if (files.Count == 0)
        {
            return;
        }

        _selectedAvatarSourcePath = files[0].Path.LocalPath;
        SelectedAvatarImage = AvatarStorage.LoadImage(_selectedAvatarSourcePath);
        OnPropertyChanged(nameof(SelectedAvatarText));
    }

    private async void CreateProfile()
    {
        var profileName = Username.Trim();
        if (string.IsNullOrWhiteSpace(profileName))
        {
            StatusMessage = "El nombre del perfil es obligatorio.";
            return;
        }

        if (await _userRepository.GetByUsernameAsync(profileName) is not null)
        {
            StatusMessage = "Ya existe un perfil con ese nombre.";
            return;
        }

        var userId = Guid.NewGuid();
        string? avatarPath = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(_selectedAvatarSourcePath))
            {
                avatarPath = AvatarStorage.CopyToManagedAvatar(_selectedAvatarSourcePath, userId);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            return;
        }

        var user = new User
        {
            Id = userId,
            Username = profileName,
            Email = $"{userId:N}@tempovium.local",
            PasswordHash = "local-profile",
            AvatarPath = avatarPath,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);
        await EnterProfileAsync(user);
    }

    private async Task EnterProfileAsync(UserCardViewModel profile)
    {
        var user = await _userRepository.GetByIdAsync(profile.Id);
        if (user == null)
        {
            StatusMessage = "No se encontró el perfil seleccionado.";
            await LoadUsersAsync();
            return;
        }

        await EnterProfileAsync(user);
    }

    private async Task EnterProfileAsync(User user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        _userSessionService.SetUser(user);

        var libraryViewModel = Program.AppHost.Services.GetRequiredService<LibraryViewModel>();
        _navigationService.CurrentView = libraryViewModel;
    }

    private void OnProfileListChanged()
    {
        OnPropertyChanged(nameof(HasLocalUsers));
        OnPropertyChanged(nameof(HasNoLocalUsers));
        OnPropertyChanged(nameof(IsProfileListVisible));
        OnPropertyChanged(nameof(IsCreateProfileVisible));
        OnPropertyChanged(nameof(CanGoBackToProfiles));
    }

    private static TopLevel? GetTopLevel()
    {
        return TopLevel.GetTopLevel(
            Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);
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

public sealed class UserCardViewModel
{
    public UserCardViewModel(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Initials = string.IsNullOrWhiteSpace(user.Username)
            ? "?"
            : user.Username[..1].ToUpperInvariant();
        LastLoginText = user.LastLoginAt is null
            ? "Perfil local"
            : $"Último acceso: {user.LastLoginAt.Value:g}";
        AvatarImage = AvatarStorage.LoadImage(user.AvatarPath);
    }

    public Guid Id { get; }

    public string Username { get; }

    public string Initials { get; }

    public string LastLoginText { get; }

    public Bitmap? AvatarImage { get; }

    public bool HasAvatar => AvatarImage is not null;

    public bool HasNoAvatar => AvatarImage is null;
}
