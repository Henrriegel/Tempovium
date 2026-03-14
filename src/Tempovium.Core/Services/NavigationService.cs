using System.ComponentModel;

namespace Tempovium.Core.Services;

public class NavigationService : INotifyPropertyChanged
{
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentView)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}