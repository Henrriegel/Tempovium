using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Services;

namespace Tempovium.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IUserRepository _userRepository;
    private readonly UserSessionService _userSessionService;
    private readonly NavigationService _navigationService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _statusMessage = string.Empty;

    public LoginViewModel(
        IUserRepository userRepository,
        UserSessionService userSessionService,
        NavigationService navigationService)
    {
        _userRepository = userRepository;
        _userSessionService = userSessionService;
        _navigationService = navigationService;
        LoginCommand = new SimpleCommand(ExecuteLogin);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand LoginCommand { get; }

    private async void ExecuteLogin()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            StatusMessage = "El usuario es obligatorio.";
            return;
        }

        var user = await _userRepository.GetByUsernameAsync(Username);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = Username,
                Email = $"{Username}@tempovium.local",
                PasswordHash = Password,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);

            StatusMessage = "Usuario creado y sesión iniciada.";
        }
        else
        {
            user.LastLoginAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            StatusMessage = $"Bienvenido de nuevo {Username}.";
        }

        _userSessionService.SetUser(user);

        var libraryViewModel = Program.AppHost.Services.GetRequiredService<LibraryViewModel>();
        _navigationService.CurrentView = libraryViewModel;
    }

    private class SimpleCommand : ICommand
    {
        private readonly Action _execute;

        public SimpleCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}