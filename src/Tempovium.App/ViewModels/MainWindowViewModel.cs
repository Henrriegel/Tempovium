using Tempovium.Core.Services;

namespace Tempovium.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public NavigationService NavigationService { get; }

    public MainWindowViewModel(NavigationService navigationService)
    {
        NavigationService = navigationService;
    }
}