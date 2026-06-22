using Tempovium.Core.Entities;
using Tempovium.Core.Enums;
using Tempovium.Core.Interfaces.Repositories;
using Tempovium.Core.Services;
using Tempovium.Services;
using Tempovium.ViewModels;

namespace Tempovium.Tests;

public class NotesPanelViewModelTests
{
    [Fact]
    public async Task EditingExistingNoteUpdatesContent()
    {
        var note = CreateNote("Original note");
        var repository = new FakeMediaNoteRepository(note);
        var viewModel = CreateViewModel(repository, note.MediaItemId, note.UserId);
        await viewModel.LoadNotesAsync();

        var item = viewModel.Notes[0];
        viewModel.StartEditNote(item);
        viewModel.NewNoteContent = "Updated note";

        await viewModel.AddNoteAsync();

        Assert.Equal("Updated note", item.Content);
        Assert.Equal("Updated note", note.Content);
        Assert.False(viewModel.IsEditing);
        Assert.Equal(string.Empty, viewModel.NewNoteContent);
    }

    [Fact]
    public async Task EditingExistingNoteSetsUpdatedAt()
    {
        var note = CreateNote("Original note");
        var repository = new FakeMediaNoteRepository(note);
        var viewModel = CreateViewModel(repository, note.MediaItemId, note.UserId);
        await viewModel.LoadNotesAsync();

        viewModel.StartEditNote(viewModel.Notes[0]);
        viewModel.NewNoteContent = "Updated note";

        await viewModel.AddNoteAsync();

        Assert.NotNull(note.UpdatedAt);
    }

    [Fact]
    public async Task CancelEditRestoresAddMode()
    {
        var note = CreateNote("Original note");
        var repository = new FakeMediaNoteRepository(note);
        var viewModel = CreateViewModel(repository, note.MediaItemId, note.UserId);
        await viewModel.LoadNotesAsync();

        viewModel.StartEditNote(viewModel.Notes[0]);
        viewModel.NewNoteContent = "Draft edit";

        viewModel.CancelEdit();

        Assert.False(viewModel.IsEditing);
        Assert.Equal(string.Empty, viewModel.NewNoteContent);
        Assert.StartsWith("Agregar nota", viewModel.AddNoteButtonText);
        Assert.Equal("Original note", note.Content);
        Assert.Equal(0, repository.UpdateCalls);
    }

    private static NotesPanelViewModel CreateViewModel(
        IMediaNoteRepository repository,
        Guid mediaId,
        Guid userId)
    {
        var selectedMedia = new SelectedMediaService
        {
            SelectedMedia = new MediaItem
            {
                Id = mediaId,
                UserId = userId,
                Title = "Test video",
                FilePath = "test.mp4",
                MediaType = MediaType.Video,
                CreatedAt = DateTime.UtcNow
            }
        };

        return new NotesPanelViewModel(
            repository,
            selectedMedia,
            new PlaybackTimelineService(),
            new PlaybackControlService());
    }

    private static MediaNote CreateNote(string content)
    {
        return new MediaNote
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            MediaItemId = Guid.NewGuid(),
            TimestampSeconds = 12.5,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeMediaNoteRepository : IMediaNoteRepository
    {
        private readonly List<MediaNote> _notes;

        public int UpdateCalls { get; private set; }

        public FakeMediaNoteRepository(params MediaNote[] notes)
        {
            _notes = notes.ToList();
        }

        public Task<List<MediaNote>> GetNotesForMediaAsync(Guid mediaId)
        {
            var notes = _notes
                .Where(note => note.MediaItemId == mediaId)
                .OrderBy(note => note.TimestampSeconds)
                .ToList();

            return Task.FromResult(notes);
        }

        public Task AddNoteAsync(MediaNote note)
        {
            _notes.Add(note);
            return Task.CompletedTask;
        }

        public Task<MediaNote?> UpdateNoteAsync(Guid noteId, string content)
        {
            UpdateCalls++;

            var note = _notes.SingleOrDefault(note => note.Id == noteId);
            if (note is null)
            {
                return Task.FromResult<MediaNote?>(null);
            }

            note.Content = content;
            note.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult<MediaNote?>(note);
        }

        public Task DeleteNoteAsync(Guid noteId)
        {
            _notes.RemoveAll(note => note.Id == noteId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }
}
