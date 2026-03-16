using System;
using System.Timers;

namespace Tempovium.Services;

public class FakePlaybackDriverService : IDisposable
{
    private readonly PlaybackTimelineService _timelineService;
    private readonly Timer _timer;

    public FakePlaybackDriverService(PlaybackTimelineService timelineService)
    {
        _timelineService = timelineService;

        _timer = new Timer(500);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    public void Start(double durationSeconds = 120)
    {
        _timelineService.DurationSeconds = durationSeconds;
        _timelineService.PositionSeconds = 0;
        _timelineService.IsPlaying = true;

        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _timelineService.IsPlaying = false;
    }

    public void Reset()
    {
        _timer.Stop();
        _timelineService.Reset();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_timelineService.IsPlaying)
            return;

        var next = _timelineService.PositionSeconds + 0.5;

        if (next >= _timelineService.DurationSeconds)
        {
            _timelineService.PositionSeconds = _timelineService.DurationSeconds;
            _timelineService.IsPlaying = false;
            _timer.Stop();
            return;
        }

        _timelineService.PositionSeconds = next;
    }

    public void Dispose()
    {
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
    }
}