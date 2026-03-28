using Avalonia.Controls;
using Avalonia.Input;
using Tempovium.Media.Mac;
using Tempovium.ViewModels;

namespace Tempovium.Controls;

public partial class MediaPlayerView : UserControl
{
    public MediaPlayerView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        var slider = this.FindControl<Slider>("TimelineSlider");
        if (slider is not null)
        {
            slider.PointerPressed += OnTimelineSliderPointerPressed;
        }
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

    private void OnTimelineSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MediaPlayerViewModel viewModel)
            return;

        viewModel.BeginUserSeek();
    }

    private async void OnTimelineSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not MediaPlayerViewModel viewModel)
            return;

        if (sender is not Slider slider)
            return;

        await viewModel.SeekToAsync(slider.Value);
    }
}