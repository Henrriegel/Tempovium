using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.ViewModels;

namespace Tempovium.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();

        DataContext = Program.AppHost.Services.GetRequiredService<LibraryViewModel>();
    }
}