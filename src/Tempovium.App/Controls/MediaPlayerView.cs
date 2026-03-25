using Avalonia.Controls;
using Tempovium.Media.Mac;
using Tempovium.ViewModels;

namespace Tempovium.Controls;

public partial class MediaPlayerView : UserControl
{
    public MediaPlayerView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is not MediaPlayerViewModel viewModel)
            return;

        if (this.FindControl<MacVideoHost>("NativeVideoHost") is { } host)
        {
            host.Backend = viewModel.MacBackend;
        }
    }
}