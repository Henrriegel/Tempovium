using Tempovium.Core.Enums;

namespace Tempovium.Core.Services;

public class MediaFileTypeDetector
{
    private static readonly string[] AudioExtensions = [ ".wav", ".mp3", ".ogg", ".oga", ".wma", ".flac", "m4a", ".aac" ];

    private static readonly string[] VideoExtensions = [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm"];

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