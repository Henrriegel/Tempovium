using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Models;
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
        var service = CreateService(repository, folder.ManagedMediaPath);

        var result = await service.ImportFileAsync(userId, filePath);

        Assert.Equal(1, result.TotalFilesScanned);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.Single(result.ImportedItems);
        var item = result.ImportedItems[0];
        Assert.NotEqual(filePath, item.FilePath);
        Assert.Equal(folder.ManagedMediaPath, Path.GetDirectoryName(item.FilePath));
        Assert.True(File.Exists(item.FilePath));
        Assert.True(File.Exists(filePath));
        Assert.Equal(filePath, item.OriginalSourcePath);
        Assert.Equal(new FileInfo(filePath).Length, item.FileSizeBytes);
    }

    [Fact]
    public async Task ImportingSameSingleFileTwiceForSameUserSkipsDuplicate()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "same single media");
        var service = CreateService(repository, folder.ManagedMediaPath);

        var first = await service.ImportFileAsync(userId, filePath);
        Assert.Single(Directory.GetFiles(folder.ManagedMediaPath));

        var second = await service.ImportFileAsync(userId, filePath);

        Assert.Equal(1, first.ImportedCount);
        Assert.Equal(0, second.ImportedCount);
        Assert.Equal(1, second.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(userId));
        Assert.Single(Directory.GetFiles(folder.ManagedMediaPath));
    }

    [Fact]
    public async Task AnotherUserCanImportSameSingleFileHash()
    {
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "shared single media");
        var service = CreateService(repository, folder.ManagedMediaPath);

        await service.ImportFileAsync(firstUser, filePath);
        var second = await service.ImportFileAsync(secondUser, filePath);

        Assert.Equal(1, second.ImportedCount);
        Assert.Equal(0, second.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(firstUser));
        Assert.Single(await repository.GetByUserAsync(secondUser));
        Assert.Equal(2, Directory.GetFiles(folder.ManagedMediaPath).Length);
    }

    [Fact]
    public async Task UnsupportedSingleFileImportIsCountedAndSkipped()
    {
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("notes.txt", "not media");
        var service = CreateService(repository, folder.ManagedMediaPath);

        var result = await service.ImportFileAsync(Guid.NewGuid(), filePath);

        Assert.Equal(1, result.TotalFilesScanned);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.UnsupportedCount);
        Assert.Empty(result.ImportedItems);
        Assert.Empty(Directory.GetFiles(folder.ManagedMediaPath));
    }

    [Fact]
    public async Task MissingSingleFileImportIsReported()
    {
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var missingPath = System.IO.Path.Combine(folder.Path, "missing.mp4");
        var service = CreateService(repository, folder.ManagedMediaPath);

        var result = await service.ImportFileAsync(Guid.NewGuid(), missingPath);

        Assert.Equal(1, result.TotalFilesScanned);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.MissingCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Empty(Directory.GetFiles(folder.ManagedMediaPath));
    }

    [Fact]
    public async Task FailedCreateDeletesManagedCopy()
    {
        var repository = new FakeMediaRepository
        {
            ThrowOnCreate = true
        };
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "create failure media");
        var service = CreateService(repository, folder.ManagedMediaPath);

        var result = await service.ImportFileAsync(Guid.NewGuid(), filePath);

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Empty(Directory.GetFiles(folder.ManagedMediaPath));
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task ImportingSameFileTwiceForSameUserSkipsDuplicate()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("lesson.mp4", "same media");
        var service = CreateService(repository, folder.ManagedMediaPath);

        var first = await service.ImportFolderAsync(userId, folder.Path);
        Assert.Single(Directory.GetFiles(folder.ManagedMediaPath));

        var second = await service.ImportFolderAsync(userId, folder.Path);

        Assert.Equal(1, first.ImportedCount);
        Assert.Equal(0, first.DuplicateCount);
        Assert.Equal(0, second.ImportedCount);
        Assert.Equal(1, second.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(userId));
        Assert.Single(Directory.GetFiles(folder.ManagedMediaPath));
    }

    [Fact]
    public async Task FolderImportSetsIdentityMetadata()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "folder media");
        var service = CreateService(repository, folder.ManagedMediaPath);

        var result = await service.ImportFolderAsync(userId, folder.Path);

        var item = Assert.Single(result.ImportedItems);
        Assert.NotEqual(filePath, item.FilePath);
        Assert.Equal(folder.ManagedMediaPath, Path.GetDirectoryName(item.FilePath));
        Assert.True(File.Exists(item.FilePath));
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
        var service = CreateService(repository, folder.ManagedMediaPath);

        await service.ImportFolderAsync(firstUser, folder.Path);
        var second = await service.ImportFolderAsync(secondUser, folder.Path);

        Assert.Equal(1, second.ImportedCount);
        Assert.Equal(0, second.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(firstUser));
        Assert.Single(await repository.GetByUserAsync(secondUser));
        Assert.Equal(2, Directory.GetFiles(folder.ManagedMediaPath).Length);
    }

    [Fact]
    public async Task UnsupportedFilesAreCountedAndSkipped()
    {
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("notes.txt", "not media");
        var service = CreateService(repository, folder.ManagedMediaPath);

        var result = await service.ImportFolderAsync(Guid.NewGuid(), folder.Path);

        Assert.Equal(1, result.TotalFilesScanned);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.UnsupportedCount);
        Assert.Empty(result.ImportedItems);
        Assert.Empty(Directory.GetFiles(folder.ManagedMediaPath));
    }

    [Fact]
    public async Task ScanFolderReturnsSupportedCandidatesWithoutImporting()
    {
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        var filePath = folder.WriteFile("lesson.mp4", "preview media");
        var service = CreateService(repository, folder.ManagedMediaPath);
        var userId = Guid.NewGuid();

        var preview = await service.ScanFolderAsync(userId, folder.Path);

        var candidate = Assert.Single(preview.Candidates);
        Assert.Equal(filePath, candidate.SourcePath);
        Assert.Equal("lesson", candidate.DisplayName);
        Assert.True(candidate.IsSelected);
        Assert.False(candidate.IsDuplicate);
        Assert.Null(candidate.FileHash);
        Assert.Empty(await repository.GetByUserAsync(userId));
        Assert.Empty(Directory.GetFiles(folder.ManagedMediaPath));
    }

    [Fact]
    public async Task ScanFolderMarksSameSourceAsPossibleDuplicateAndDefaultsItUnselected()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("lesson.mp4", "duplicate media");
        var service = CreateService(repository, folder.ManagedMediaPath);
        await service.ImportFolderAsync(userId, folder.Path);

        var preview = await service.ScanFolderAsync(userId, folder.Path);

        var candidate = Assert.Single(preview.Candidates);
        Assert.False(candidate.IsDuplicate);
        Assert.True(candidate.IsPossibleDuplicate);
        Assert.False(candidate.IsSelected);
        Assert.True(candidate.CanSelect);
        Assert.Equal("Posible duplicado", candidate.StatusText);
        Assert.Equal(1, preview.DuplicateCount);
    }

    [Fact]
    public async Task ScanFolderDoesNotComputeFullHashes()
    {
        var repository = new FakeMediaRepository();
        var hashService = new TextFileHashService();
        using var folder = TestFolder.Create();
        folder.WriteFile("lesson.mp4", "preview media");
        var service = CreateService(repository, folder.ManagedMediaPath, hashService);

        var preview = await service.ScanFolderAsync(Guid.NewGuid(), folder.Path);

        Assert.Single(preview.Candidates);
        Assert.Equal(0, hashService.ComputeCount);
    }

    [Fact]
    public async Task ScanFolderCountsUnsupportedFiles()
    {
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("lesson.mp4", "preview media");
        folder.WriteFile("notes.txt", "not media");
        var service = CreateService(repository, folder.ManagedMediaPath);

        var preview = await service.ScanFolderAsync(Guid.NewGuid(), folder.Path);

        Assert.Equal(2, preview.TotalFilesScanned);
        Assert.Equal(1, preview.SupportedCount);
        Assert.Equal(1, preview.UnsupportedCount);
    }

    [Fact]
    public async Task ImportCandidatesImportsOnlySelectedFiles()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        var hashService = new TextFileHashService();
        using var folder = TestFolder.Create();
        folder.WriteFile("first.mp4", "first media");
        var secondPath = folder.WriteFile("second.mp4", "second media");
        var service = CreateService(repository, folder.ManagedMediaPath, hashService);
        var preview = await service.ScanFolderAsync(userId, folder.Path);
        preview.Candidates.Single(candidate => candidate.SourcePath == secondPath).IsSelected = false;

        var result = await service.ImportCandidatesAsync(userId, preview.Candidates);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, hashService.ComputeCount);
        Assert.Single(await repository.GetByUserAsync(userId));
        Assert.Single(Directory.GetFiles(folder.ManagedMediaPath));
    }

    [Fact]
    public async Task ImportCandidatesUsesEditedDisplayName()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("lesson.mp4", "named media");
        var service = CreateService(repository, folder.ManagedMediaPath);
        var preview = await service.ScanFolderAsync(userId, folder.Path);
        preview.Candidates[0].DisplayName = "Clase editada";

        var result = await service.ImportCandidatesAsync(userId, preview.Candidates);

        var item = Assert.Single(result.ImportedItems);
        Assert.Equal("Clase editada", item.Title);
    }

    [Fact]
    public async Task ImportCandidatesSkipsRealDuplicatesOnConfirm()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeMediaRepository();
        using var folder = TestFolder.Create();
        folder.WriteFile("lesson.mp4", "duplicate on confirm");
        var service = CreateService(repository, folder.ManagedMediaPath);
        await service.ImportFolderAsync(userId, folder.Path);
        var preview = await service.ScanFolderAsync(userId, folder.Path);
        preview.Candidates[0].IsSelected = true;

        var result = await service.ImportCandidatesAsync(userId, preview.Candidates);

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Single(await repository.GetByUserAsync(userId));
        Assert.Single(Directory.GetFiles(folder.ManagedMediaPath));
    }

    private static MediaImportService CreateService(
        FakeMediaRepository repository,
        string managedMediaDirectory,
        IFileHashService? hashService = null)
    {
        return new MediaImportService(
            repository,
            hashService ?? new TextFileHashService(),
            new MediaFileTypeDetector(),
            managedMediaDirectory);
    }

    private sealed class TextFileHashService : IFileHashService
    {
        public int ComputeCount { get; private set; }

        public async Task<string> ComputeHashAsync(string filePath)
        {
            ComputeCount++;
            return await File.ReadAllTextAsync(filePath);
        }
    }

    private sealed class FakeMediaRepository : IMediaRepository
    {
        private readonly List<MediaItem> _items = [];

        public bool ThrowOnCreate { get; init; }

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

        public Task<MediaItem?> GetByOriginalSourcePathAsync(Guid userId, string originalSourcePath)
        {
            return Task.FromResult(_items.FirstOrDefault(item =>
                item.UserId == userId &&
                item.OriginalSourcePath == originalSourcePath));
        }

        public Task CreateAsync(MediaItem media)
        {
            if (ThrowOnCreate)
            {
                throw new InvalidOperationException("Create failed");
            }

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
        private readonly string _rootPath;

        public string Path { get; }
        public string ManagedMediaPath { get; }

        private TestFolder(string rootPath, string sourcePath, string managedMediaPath)
        {
            _rootPath = rootPath;
            Path = sourcePath;
            ManagedMediaPath = managedMediaPath;
        }

        public static TestFolder Create()
        {
            var rootPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "TempoviumImportTests",
                Guid.NewGuid().ToString("N"));
            var sourcePath = System.IO.Path.Combine(rootPath, "source");
            var managedMediaPath = System.IO.Path.Combine(rootPath, "managed-media");

            Directory.CreateDirectory(sourcePath);
            Directory.CreateDirectory(managedMediaPath);
            return new TestFolder(rootPath, sourcePath, managedMediaPath);
        }

        public string WriteFile(string fileName, string content)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(_rootPath, recursive: true);
            }
        }
    }
}
