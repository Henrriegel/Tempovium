using System.Collections.ObjectModel;
using Tempovium.Core.Entities;
using Tempovium.Core.Enums;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Models;
using Tempovium.Core.Services;
using Tempovium.Media.Abstractions.Backends;
using Tempovium.Services;
using Tempovium.ViewModels;

namespace Tempovium.Tests;

public class LibraryViewModelImportReviewTests
{
    [Fact]
    public void ImportReviewSelectionCountFollowsCandidateCheckboxState()
    {
        var viewModel = CreateViewModel(out _, out _);
        var first = CreateCandidate("first.mp4", isSelected: true);
        var second = CreateCandidate("second.mp4", isSelected: false);

        viewModel.ImportReviewCandidates = new ObservableCollection<MediaImportCandidate>
        {
            first,
            second
        };

        Assert.Equal(1, viewModel.SelectedImportReviewCount);
        Assert.True(viewModel.CanConfirmImport);

        first.IsSelected = false;

        Assert.Equal(0, viewModel.SelectedImportReviewCount);
        Assert.False(viewModel.CanConfirmImport);

        second.IsSelected = true;

        Assert.Equal(1, viewModel.SelectedImportReviewCount);
        Assert.True(viewModel.CanConfirmImport);
    }

    [Fact]
    public void BulkSelectionCommandsUpdateSelectedCount()
    {
        var viewModel = CreateViewModel(out _, out _);
        viewModel.ImportReviewCandidates = new ObservableCollection<MediaImportCandidate>
        {
            CreateCandidate("first.mp4", isSelected: false),
            CreateCandidate("second.mp4", isSelected: false),
            CreateCandidate("duplicate.mp4", isSelected: false, isDuplicate: true)
        };

        viewModel.SelectAllImportReviewCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedImportReviewCount);
        Assert.True(viewModel.CanConfirmImport);

        viewModel.ClearImportReviewSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedImportReviewCount);
        Assert.False(viewModel.CanConfirmImport);
    }

    [Fact]
    public void ConfirmImportWithNoSelectionDoesNotCallImport()
    {
        var viewModel = CreateViewModel(out var session, out var importService);
        session.SetUser(CreateUser());
        viewModel.ImportReviewCandidates = new ObservableCollection<MediaImportCandidate>
        {
            CreateCandidate("first.mp4", isSelected: false)
        };

        viewModel.ConfirmImportCommand.Execute(null);

        Assert.Equal(0, importService.ImportCandidatesCallCount);
        Assert.Equal("Selecciona al menos un archivo para importar.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ConfirmImportPassesOnlySelectedCandidates()
    {
        var viewModel = CreateViewModel(out var session, out var importService);
        session.SetUser(CreateUser());
        var selected = CreateCandidate("selected.mp4", isSelected: true);
        var unselected = CreateCandidate("unselected.mp4", isSelected: false);
        viewModel.ImportReviewCandidates = new ObservableCollection<MediaImportCandidate>
        {
            selected,
            unselected
        };

        viewModel.ConfirmImportCommand.Execute(null);
        await WaitForAsync(() => importService.ImportCandidatesCallCount == 1);

        var candidate = Assert.Single(importService.LastCandidates);
        Assert.Same(selected, candidate);
    }

    private static LibraryViewModel CreateViewModel(
        out UserSessionService userSessionService,
        out FakeMediaImportService importService)
    {
        userSessionService = new UserSessionService();
        importService = new FakeMediaImportService();
        var selectedMediaService = new SelectedMediaService();
        var mediaPlayerViewModel = new MediaPlayerViewModel(
            selectedMediaService,
            new UnsupportedMediaBackend(),
            new PlaybackTimelineService(),
            new PlaybackControlService());

        var shellViewModel = new MainWindowViewModel(
            new NavigationService(),
            userSessionService,
            selectedMediaService,
            new FakeUserRepository(),
            new UiSettingsStore(Path.Combine(Path.GetTempPath(), $"tempovium-ui-{Guid.NewGuid():N}.json")));

        return new LibraryViewModel(
            new FakeMediaRepository(),
            importService,
            userSessionService,
            selectedMediaService,
            mediaPlayerViewModel,
            shellViewModel);
    }

    private static MediaImportCandidate CreateCandidate(
        string sourcePath,
        bool isSelected,
        bool isDuplicate = false)
    {
        return new MediaImportCandidate
        {
            SourcePath = sourcePath,
            DisplayName = Path.GetFileNameWithoutExtension(sourcePath),
            MediaType = MediaType.Video,
            FileSizeBytes = 1024,
            IsDuplicate = isDuplicate,
            IsSelected = isSelected,
            StatusText = isDuplicate ? "Duplicado" : "Listo para revisar"
        };
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = "Profile",
            Email = "profile@tempovium.local",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    private sealed class FakeMediaImportService : IMediaImportService
    {
        public int ImportCandidatesCallCount { get; private set; }
        public List<MediaImportCandidate> LastCandidates { get; private set; } = [];

        public Task<MediaImportPreviewResult> ScanFolderAsync(Guid userId, string folderPath)
        {
            return Task.FromResult(new MediaImportPreviewResult());
        }

        public Task<MediaImportResult> ImportCandidatesAsync(
            Guid userId,
            IEnumerable<MediaImportCandidate> candidates)
        {
            ImportCandidatesCallCount++;
            LastCandidates = candidates.ToList();

            return Task.FromResult(new MediaImportResult
            {
                ImportedCount = LastCandidates.Count
            });
        }

        public Task<MediaImportResult> ImportFolderAsync(Guid userId, string folderPath)
        {
            return Task.FromResult(new MediaImportResult());
        }

        public Task<MediaImportResult> ImportFileAsync(Guid userId, string filePath)
        {
            return Task.FromResult(new MediaImportResult());
        }
    }

    private sealed class FakeMediaRepository : IMediaRepository
    {
        public Task<MediaItem?> GetByIdAsync(Guid id)
        {
            return Task.FromResult<MediaItem?>(null);
        }

        public Task<List<MediaItem>> GetByUserAsync(Guid user)
        {
            return Task.FromResult(new List<MediaItem>());
        }

        public Task<MediaItem?> GetByHashAsync(Guid userId, string hash)
        {
            return Task.FromResult<MediaItem?>(null);
        }

        public Task<MediaItem?> GetByOriginalSourcePathAsync(Guid userId, string originalSourcePath)
        {
            return Task.FromResult<MediaItem?>(null);
        }

        public Task CreateAsync(MediaItem media)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MediaItem media)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<List<User>> GetAllAsync()
        {
            return Task.FromResult(new List<User>());
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            return Task.FromResult<User?>(null);
        }

        public Task CreateAsync(User user)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            return Task.CompletedTask;
        }
    }
}
