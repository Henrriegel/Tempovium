using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Services;
using Tempovium.Infrastructure.Services;

namespace Tempovium.Tests;

public class MediaImportServiceTests
{
    [Fact]
    public async Task SupportedSingleFileImportImportsOneItem()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "single media");
        var service = CreateService(repository);

        var result = await service.ImportFileAsync(userId, filePath);

        Assert.Equal(1, result.TotalFilesScanned);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.Single(result.ImportedItems);
        Assert.Equal(filePath, result.ImportedItems[0].FilePath);
        Assert.Equal(filePath, result.ImportedItems[0].OriginalSourcePath);
        Assert.Equal(new FileInfo(filePath).Length, result.ImportedItems[0].FileSizeBytes);
    }

    [Fact]
    public async Task ImportingSameSingleFileTwiceForSameUserSkipsDuplicate()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "same single media");
        var service = CreateService(repository);

        var first = await service.ImportFileAsync(userId, filePath);
        var second = await service.ImportFileAsync(userId, filePath);

        Assert.Equal(1, first.ImportedCount);
        Assert.Equal(0, second.ImportedCount);
        Assert.Equal(1, second.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(userId));
    }

    [Fact]
    public async Task AnotherUserCanImportSameSingleFileHash()
    {
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "shared single media");
        var service = CreateService(repository);

        await service.ImportFileAsync(firstUser, filePath);
        var second = await service.ImportFileAsync(secondUser, filePath);

        Assert.Equal(1, second.ImportedCount);
        Assert.Equal(0, second.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(firstUser));
        Assert.Single(await repository.GetByUserAsync(secondUser));
    }

    [Fact]
    public async Task UnsupportedSingleFileImportIsCountedAndSkipped()
    {
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("notes.txt", "not media");
        var service = CreateService(repository);

        var result = await service.ImportFileAsync(Guid.NewGuid(), filePath);

        Assert.Equal(1, result.TotalFilesScanned);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.UnsupportedCount);
        Assert.Empty(result.ImportedItems);
    }

    [Fact]
    public async Task MissingSingleFileImportIsReported()
    {
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var missingPath = System.IO.Path.Combine(folder.Path, "missing.mp4");
        var service = CreateService(repository);

        var result = await service.ImportFileAsync(Guid.NewGuid(), missingPath);

        Assert.Equal(1, result.TotalFilesScanned);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.MissingCount);
        Assert.Equal(1, result.ErrorCount);
    }

    [Fact]
    public async Task ImportingSameFileTwiceForSameUserSkipsDuplicate()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("lesson.mp4", "same media");
        var service = CreateService(repository);

        var first = await service.ImportFolderAsync(userId, folder.Path);
        var second = await service.ImportFolderAsync(userId, folder.Path);

        Assert.Equal(1, first.ImportedCount);
        Assert.Equal(0, first.DuplicateCount);
        Assert.Equal(0, second.ImportedCount);
        Assert.Equal(1, second.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(userId));
    }

    [Fact]
    public async Task FolderImportSetsIdentityMetadata()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "folder media");
        var service = CreateService(repository);

        var result = await service.ImportFolderAsync(userId, folder.Path);

        var item = Assert.Single(result.ImportedItems);
        Assert.Equal(filePath, item.OriginalSourcePath);
        Assert.Equal(new FileInfo(filePath).Length, item.FileSizeBytes);
    }

    [Fact]
    public async Task AnotherUserCanImportSameHash()
    {
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("lesson.mp4", "same media");
        var service = CreateService(repository);

        await service.ImportFolderAsync(firstUser, folder.Path);
        var second = await service.ImportFolderAsync(secondUser, folder.Path);

        Assert.Equal(1, second.ImportedCount);
        Assert.Equal(0, second.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(firstUser));
        Assert.Single(await repository.GetByUserAsync(secondUser));
    }

    [Fact]
    public async Task UnsupportedFilesAreCountedAndSkipped()
    {
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("notes.txt", "not media");
        var service = CreateService(repository);

        var result = await service.ImportFolderAsync(Guid.NewGuid(), folder.Path);

        Assert.Equal(1, result.TotalFilesScanned);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.UnsupportedCount);
        Assert.Empty(result.ImportedItems);
    }

    private static MediaImportService CreateService(FakeMediaRepository repository)
    {
        return new MediaImportService(
            repository,
            new TextFileHashService(),
            new MediaFileTypeDetector());
    }

    private sealed class TextFileHashService : IFileHashService
    {
        public async Task<string> ComputeHashAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath);
        }
    }

    private sealed class FakeMediaRepository : IMediaRepository
    {
        private readonly List<MediaItem> _items = [];

        public Task<MediaItem?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_items.FirstOrDefault(item => item.Id == id));
        }

        public Task<List<MediaItem>> GetByUserAsync(Guid user)
        {
            return Task.FromResult(_items.Where(item => item.UserId == user).ToList());
        }

        public Task<MediaItem?> GetByHashAsync(Guid userId, string hash)
        {
            return Task.FromResult(_items.FirstOrDefault(item => item.UserId == userId && item.FileHash == hash));
        }

        public Task CreateAsync(MediaItem media)
        {
            _items.Add(media);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MediaItem media)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _items.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class TestFolder : IDisposable
    {
        public string Path { get; }

        private TestFolder(string path)
        {
            Path = path;
        }

        public static TestFolder Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "TempoviumImportTests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(path);
            return new TestFolder(path);
        }

        public string WriteFile(string fileName, string content)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
