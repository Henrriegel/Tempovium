using LibVLCSharp.Shared;

namespace Tempovium.Services;

public class MediaPlayerService
{
    private const string MacLibPath = "/Applications/VLC.app/Contents/MacOS/lib";
    private const string MacPluginsPath = "/Applications/VLC.app/Contents/MacOS/plugins";

    public LibVLC LibVLC { get; }
    public MediaPlayer MediaPlayer { get; }

    public MediaPlayerService()
    {
        LibVLC = new LibVLC(
            $"--plugin-path={MacPluginsPath}",
            "--no-video-title-show"
        );

        MediaPlayer = new MediaPlayer(LibVLC);
    }

    public void Play(string filePath)
    {
        var media = new Media(LibVLC, filePath, FromType.FromPath);
        MediaPlayer.Play(media);
    }

    public void Stop()
    {
        MediaPlayer.Stop();
    }
}