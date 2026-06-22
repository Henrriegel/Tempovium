using Tempovium.Services;

namespace Tempovium.Tests;

public class MediaPathDisplayFormatterTests
{
    [Fact]
    public void ManagedMediaPathShowsShortInternalIdentifier()
    {
        var root = Path.Combine(Path.GetTempPath(), "Tempovium", "Media");
        var path = Path.Combine(root, "abc123.mp4");

        var display = MediaPathDisplayFormatter.Format(path, root);

        Assert.Equal("Medio interno: abc123", display);
    }

    [Fact]
    public void ExternalPathStaysReadable()
    {
        var root = Path.Combine(Path.GetTempPath(), "Tempovium", "Media");
        var path = Path.Combine(Path.GetTempPath(), "source", "lesson.mp4");

        var display = MediaPathDisplayFormatter.Format(path, root);

        Assert.Equal(path, display);
    }
}
