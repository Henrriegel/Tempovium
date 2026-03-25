using Tempovium.Core.Enums;

namespace Tempovium.Core.Services;

public class MediaFileTypeDetector
{
    private static readonly string[] AudioExtensions = [
        ".mp3", ".aac", ".m4a", ".wav", ".aif", ".aiff", ".caf"
    ];

    private static readonly string[] VideoExtensions = [
        ".mp4", ".mov", ".m4v"
    ];

    public MediaType? DetectFromPath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (AudioExtensions.Contains(extension))
        {
            return MediaType.Audio;
        }

        if (VideoExtensions.Contains(extension))
        {
            return MediaType.Video;
        }
        
        return null;
    }
}