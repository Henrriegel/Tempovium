using System;
using System.ComponentModel;

namespace Tempovium.Services;

public class PlaybackTimelineService : INotifyPropertyChanged
{
    private double _positionSeconds;
    private double _durationSeconds;
    private bool _isPlaying;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double PositionSeconds
    {
        get => _positionSeconds;
        set
        {
            if (_positionSeconds != value)
            {
                _positionSeconds = value;
                OnPropertyChanged(nameof(PositionSeconds));
            }
        }
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (_durationSeconds != value)
            {
                _durationSeconds = value;
                OnPropertyChanged(nameof(DurationSeconds));
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged(nameof(IsPlaying));
            }
        }
    }

    public void Reset()
    {
        PositionSeconds = 0;
        DurationSeconds = 0;
        IsPlaying = false;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}