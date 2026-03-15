using LibVLCSharp.Shared;

namespace Tempovium.ViewModels;

public class MediaPlayerViewModel : ViewModelBase
{
    public LibVLC LibVLC { get; }
    public MediaPlayer MediaPlayer { get; }

    public MediaPlayerViewModel()
    {
        LibVLC = new LibVLC();
        MediaPlayer = new MediaPlayer(LibVLC);
    }
}