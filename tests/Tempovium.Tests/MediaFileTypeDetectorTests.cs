using Tempovium.Core.Enums;
using Tempovium.Core.Services;

namespace Tempovium.Tests;

public class MediaFileTypeDetectorTests
{
    [Fact]
    public void DetectsMkvAsVideo()
    {
        var detector = new MediaFileTypeDetector();

        var mediaType = detector.DetectFromPath("lesson.mkv");

        Assert.Equal(MediaType.Video, mediaType);
    }
}
