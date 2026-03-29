using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Tempovium.ViewModels;

namespace Tempovium.Controls;

public partial class MediaPlayerView : UserControl
{
    private Slider? _timelineSlider;
    private bool _isDraggingTimeline;
    private double _dragSeekValue;

    public MediaPlayerView()
    {
        InitializeComponent();

        _timelineSlider = this.FindControl<Slider>("TimelineSlider");
        if (_timelineSlider is not null)
        {
            _timelineSlider.AddHandler(InputElement.PointerPressedEvent, OnTimelineSliderPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            _timelineSlider.AddHandler(InputElement.PointerReleasedEvent, OnTimelineSliderPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            _timelineSlider.PropertyChanged += OnTimelineSliderPropertyChanged;
        }
    }

    private void OnTimelineSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MediaPlayerViewModel viewModel || _timelineSlider is null)
        {
            return;
        }

        _isDraggingTimeline = true;
        _dragSeekValue = _timelineSlider.Value;

        Console.WriteLine($"[UI] PointerPressed -> slider.Value={_timelineSlider.Value:F2}");

        viewModel.BeginUserSeek();
        viewModel.UpdateUserSeek(_dragSeekValue);
    }

    private void OnTimelineSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_timelineSlider is null)
        {
            return;
        }

        if (e.Property != RangeBase.ValueProperty)
        {
            return;
        }

        if (!_isDraggingTimeline)
        {
            return;
        }

        if (DataContext is not MediaPlayerViewModel viewModel)
        {
            return;
        }

        _dragSeekValue = _timelineSlider.Value;

        Console.WriteLine($"[UI] SliderValueChanged -> slider.Value={_timelineSlider.Value:F2}, stored={_dragSeekValue:F2}");

        viewModel.UpdateUserSeek(_dragSeekValue);
    }

    private async void OnTimelineSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDraggingTimeline)
        {
            return;
        }

        if (DataContext is not MediaPlayerViewModel viewModel || _timelineSlider is null)
        {
            return;
        }

        _dragSeekValue = _timelineSlider.Value;
        _isDraggingTimeline = false;

        Console.WriteLine($"[UI] PointerReleased -> slider.Value={_timelineSlider.Value:F2}, finalStored={_dragSeekValue:F2}");

        await viewModel.SeekToAsync(_dragSeekValue);
    }
}