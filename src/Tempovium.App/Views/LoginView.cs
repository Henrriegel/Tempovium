using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.ViewModels;

namespace Tempovium.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();

        DataContext = Program.AppHost.Services.GetRequiredService<LoginViewModel>();
    }
}