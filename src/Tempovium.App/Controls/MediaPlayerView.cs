using Avalonia.Controls;
using Tempovium.ViewModels;

namespace Tempovium.Controls;

public partial class MediaPlayerView : UserControl
{
    public MediaPlayerView()
    {
        InitializeComponent();

        DataContext = new MediaPlayerViewModel();
    }
}