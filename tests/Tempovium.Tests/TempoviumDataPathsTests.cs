using Tempovium.Infrastructure.Persistence;

namespace Tempovium.Tests;

public class TempoviumDataPathsTests
{
    [Fact]
    public void DatabasePathUsesTempoviumAppDataDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TempoviumTests", Guid.NewGuid().ToString("N"));

        try
        {
            var path = TempoviumDataPaths.GetDatabasePath(directory);

            Assert.Equal(TempoviumDataPaths.DatabaseFileName, Path.GetFileName(path));
            Assert.Equal(TempoviumDataPaths.AppDirectoryName, Path.GetFileName(Path.GetDirectoryName(path)));
            Assert.True(Directory.Exists(Path.GetDirectoryName(path)));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ManagedMediaDirectoryUsesTempoviumAppDataDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TempoviumTests", Guid.NewGuid().ToString("N"));

        try
        {
            var path = TempoviumDataPaths.GetManagedMediaDirectory(directory);

            Assert.Equal(TempoviumDataPaths.ManagedMediaDirectoryName, Path.GetFileName(path));
            Assert.Equal(TempoviumDataPaths.AppDirectoryName, Path.GetFileName(Path.GetDirectoryName(path)));
            Assert.True(Directory.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void AvatarDirectoryUsesTempoviumAppDataDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TempoviumTests", Guid.NewGuid().ToString("N"));

        try
        {
            var path = TempoviumDataPaths.GetAvatarDirectory(directory);

            Assert.Equal(TempoviumDataPaths.AvatarDirectoryName, Path.GetFileName(path));
            Assert.Equal(TempoviumDataPaths.AppDirectoryName, Path.GetFileName(Path.GetDirectoryName(path)));
            Assert.True(Directory.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void LegacyDatabaseCopyDoesNotOverwriteExistingTarget()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TempoviumTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(directory);

            var legacyPath = Path.Combine(directory, "legacy.db");
            var targetPath = Path.Combine(directory, TempoviumDataPaths.DatabaseFileName);
            File.WriteAllText(legacyPath, "legacy");
            File.WriteAllText(targetPath, "target");

            TempoviumDataPaths.CopyLegacyDatabaseIfNeeded(legacyPath, targetPath);

            Assert.Equal("target", File.ReadAllText(targetPath));
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
