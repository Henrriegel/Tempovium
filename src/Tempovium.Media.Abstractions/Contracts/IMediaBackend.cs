namespace Tempovium.Media.Abstractions.Contracts;

public interface IMediaBackend : IDisposable
{
    bool IsLoaded { get; }
    bool IsPlaying { get; }

    TimeSpan Duration { get; }
    TimeSpan Position { get; }

    void Load(string path);
    void Play();
    void Pause();
    void Stop();
    void Seek(TimeSpan position);
    void SetVolume(double volume);
    void UpdateState();

    event EventHandler? MediaOpened;
    event EventHandler? MediaEnded;
    event EventHandler<string>? MediaFailed;
    event EventHandler<TimeSpan>? PositionChanged;
}