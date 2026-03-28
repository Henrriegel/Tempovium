using System;
using System.ComponentModel;

namespace Tempovium.Services;

public class PlaybackTimelineService : INotifyPropertyChanged
{
    private double _playbackPositionSeconds;
    private double _displayPositionSeconds;
    private double _durationSeconds;
    private bool _isPlaying;
    private bool _isUserSeeking;
    private bool _isSeekPending;
    private double? _pendingSeekTargetSeconds;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double PlaybackPositionSeconds
    {
        get => _playbackPositionSeconds;
        private set
        {
            if (Math.Abs(_playbackPositionSeconds - value) < 0.01)
            {
                return;
            }

            _playbackPositionSeconds = value;
            OnPropertyChanged(nameof(PlaybackPositionSeconds));
        }
    }

    public double DisplayPositionSeconds
    {
        get => _displayPositionSeconds;
        private set
        {
            if (Math.Abs(_displayPositionSeconds - value) < 0.01)
            {
                return;
            }

            _displayPositionSeconds = value;
            OnPropertyChanged(nameof(DisplayPositionSeconds));
        }
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        private set
        {
            if (Math.Abs(_durationSeconds - value) < 0.01)
            {
                return;
            }

            _durationSeconds = value;
            OnPropertyChanged(nameof(DurationSeconds));
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying == value)
            {
                return;
            }

            _isPlaying = value;
            OnPropertyChanged(nameof(IsPlaying));
        }
    }

    public bool IsUserSeeking
    {
        get => _isUserSeeking;
        private set
        {
            if (_isUserSeeking == value)
            {
                return;
            }

            _isUserSeeking = value;
            OnPropertyChanged(nameof(IsUserSeeking));
        }
    }

    public bool IsSeekPending
    {
        get => _isSeekPending;
        private set
        {
            if (_isSeekPending == value)
            {
                return;
            }

            _isSeekPending = value;
            OnPropertyChanged(nameof(IsSeekPending));
        }
    }

    public void BeginUserSeek()
    {
        IsUserSeeking = true;
        IsSeekPending = false;
        _pendingSeekTargetSeconds = null;
        DisplayPositionSeconds = ClampToDuration(DisplayPositionSeconds);
    }

    public void UpdateUserSeek(double seconds)
    {
        DisplayPositionSeconds = ClampToDuration(seconds);
    }

    public double CommitUserSeek()
    {
        var target = ClampToDuration(DisplayPositionSeconds);

        IsUserSeeking = false;
        IsSeekPending = true;
        _pendingSeekTargetSeconds = target;
        DisplayPositionSeconds = target;

        return target;
    }

    public void CancelUserSeek()
    {
        IsUserSeeking = false;
        IsSeekPending = false;
        _pendingSeekTargetSeconds = null;
        DisplayPositionSeconds = PlaybackPositionSeconds;
    }

    public void ApplyBackendState(double positionSeconds, double durationSeconds, bool isPlaying)
    {
        var safeDuration = Sanitize(durationSeconds);
        var safePosition = Sanitize(positionSeconds);

        if (safeDuration > 0 && safePosition > safeDuration)
        {
            safePosition = safeDuration;
        }

        DurationSeconds = safeDuration;
        IsPlaying = isPlaying;
        PlaybackPositionSeconds = safePosition;

        if (IsUserSeeking)
        {
            return;
        }

        if (IsSeekPending && _pendingSeekTargetSeconds is double target)
        {
            if (Math.Abs(safePosition - target) <= 0.35)
            {
                IsSeekPending = false;
                _pendingSeekTargetSeconds = null;
                DisplayPositionSeconds = safePosition;
                return;
            }

            DisplayPositionSeconds = target;
            return;
        }

        DisplayPositionSeconds = safePosition;
    }

    public void SetPlaybackStateOnly(bool isPlaying)
    {
        IsPlaying = isPlaying;
    }

    public void Reset()
    {
        _playbackPositionSeconds = 0;
        _displayPositionSeconds = 0;
        _durationSeconds = 0;
        _isPlaying = false;
        _isUserSeeking = false;
        _isSeekPending = false;
        _pendingSeekTargetSeconds = null;

        OnPropertyChanged(nameof(PlaybackPositionSeconds));
        OnPropertyChanged(nameof(DisplayPositionSeconds));
        OnPropertyChanged(nameof(DurationSeconds));
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsUserSeeking));
        OnPropertyChanged(nameof(IsSeekPending));
    }

    private double ClampToDuration(double seconds)
    {
        var safe = Sanitize(seconds);

        if (DurationSeconds > 0)
        {
            return Math.Clamp(safe, 0, DurationSeconds);
        }

        return Math.Max(0, safe);
    }

    private static double Sanitize(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return value < 0 ? 0 : value;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}