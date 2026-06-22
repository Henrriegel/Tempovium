using Tempovium.Services;

namespace Tempovium.Tests;

public class AvatarStorageTests
{
    [Fact]
    public void CopyToManagedAvatarCopiesSupportedFileIntoManagedDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TempoviumTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(directory);
            var sourcePath = Path.Combine(directory, "avatar.png");
            var avatarDirectory = Path.Combine(directory, "Avatars");
            var userId = Guid.NewGuid();
            File.WriteAllText(sourcePath, "avatar");

            var copiedPath = AvatarStorage.CopyToManagedAvatar(sourcePath, userId, avatarDirectory);

            Assert.Equal(Path.Combine(avatarDirectory, $"{userId:N}.png"), copiedPath);
            Assert.True(File.Exists(copiedPath));
            Assert.Equal("avatar", File.ReadAllText(copiedPath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
