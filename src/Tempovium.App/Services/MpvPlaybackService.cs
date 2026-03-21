namespace Tempovium.Services;

public class MpvPlaybackService : IPlaybackService
{
    private readonly MpvProcessService _mpvProcess;
    private readonly PlaybackTimelineService _timeline;

    public bool IsAvailable => true;
    public string BackendName => "mpv";

    public MpvPlaybackService(
        MpvProcessService mpvProcess,
        PlaybackTimelineService timeline)
    {
        _mpvProcess = mpvProcess;
        _timeline = timeline;
    }

    public void Play(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        _mpvProcess.Play(filePath);

        _timeline.PositionSeconds = 0;
        _timeline.IsPlaying = true;
    }

    public void Stop()
    {
        _mpvProcess.Stop();
        _timeline.Reset();
    }
}