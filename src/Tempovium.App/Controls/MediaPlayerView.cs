using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.ViewModels;

namespace Tempovium.Controls;

public partial class MediaPlayerView : UserControl
{
    public MediaPlayerView()
    {
        InitializeComponent();
        DataContext = Program.AppHost.Services.GetRequiredService<MediaPlayerViewModel>();
    }
}