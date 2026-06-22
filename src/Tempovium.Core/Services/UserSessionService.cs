using Tempovium.Core.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tempovium.Core.Services;

public class UserSessionService : INotifyPropertyChanged
{
    private User? _currentUser;

    public User? CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (ReferenceEquals(_currentUser, value))
            {
                return;
            }

            _currentUser = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLoggedIn));
        }
    }

    public bool IsLoggedIn => CurrentUser != null;

    public void SetUser(User user)
    {
        CurrentUser = user;
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
