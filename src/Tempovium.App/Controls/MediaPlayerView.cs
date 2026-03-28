using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Tempovium.ViewModels;

namespace Tempovium.Controls;

public partial class MediaPlayerView : UserControl
{
    public MediaPlayerView()
    {
        InitializeComponent();

        var slider = this.FindControl<Slider>("TimelineSlider");
        if (slider is not null)
        {
            slider.PointerPressed += OnTimelineSliderPointerPressed;
            slider.PointerReleased += OnTimelineSliderPointerReleased;
            slider.PropertyChanged += OnTimelineSliderPropertyChanged;
        }
    }

    private void OnTimelineSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MediaPlayerViewModel viewModel)
        {
            return;
        }

        viewModel.BeginUserSeek();
    }

    private void OnTimelineSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RangeBase.ValueProperty)
        {
            return;
        }

        if (DataContext is not MediaPlayerViewModel viewModel)
        {
            return;
        }

        if (sender is not Slider slider)
        {
            return;
        }

        if (!viewModel.IsUserSeeking)
        {
            return;
        }

        viewModel.UpdateUserSeek(slider.Value);
    }

    private async void OnTimelineSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not MediaPlayerViewModel viewModel)
        {
            return;
        }

        if (sender is not Slider slider)
        {
            return;
        }

        await viewModel.SeekToAsync(slider.Value);
    }
}