namespace Tempovium.Services;

public interface IPlaybackService
{
    bool IsAvailable { get; }
    string BackendName { get; }

    void Play(string filePath);
    void Stop();
}