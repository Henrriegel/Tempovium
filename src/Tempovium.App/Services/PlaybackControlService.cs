using System;

namespace Tempovium.Services;

public class PlaybackControlService
{
    public event Action<double>? SeekRequested;

    public void RequestSeek(double seconds)
    {
        if (seconds < 0)
            seconds = 0;

        SeekRequested?.Invoke(seconds);
    }
}