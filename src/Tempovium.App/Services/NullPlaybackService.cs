namespace Tempovium.Services;

public class NullPlaybackService : IPlaybackService
{
    public bool IsAvailable => false;
    public string BackendName => "Sin backend de reproducción";

    public void Play(string filePath)
    {
        // Intencionalmente vacío.
        // Este servicio existe para mantener estable la app
        // mientras resolvemos el backend real de reproducción.
    }

    public void Stop()
    {
        // Intencionalmente vacío.
    }
}