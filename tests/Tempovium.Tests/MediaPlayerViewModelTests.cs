using Tempovium.Core.Entities;
using Tempovium.Core.Enums;
using Tempovium.Core.Interfaces.Repositories;
using Tempovium.Core.Services;
using Tempovium.Media.Abstractions.Backends;
using Tempovium.Media.Abstractions.Contracts;
using Tempovium.Services;
using Tempovium.ViewModels;

namespace Tempovium.Tests;

public class MediaPlayerViewModelTests
{
    [Fact]
    public void UnsupportedBackendShowsPlaceholderState()
    {
        var selectedMedia = new SelectedMediaService();
        using var backend = new UnsupportedMediaBackend();
        var viewModel = new MediaPlayerViewModel(
            selectedMedia,
            backend,
            new PlaybackTimelineService(),
            new PlaybackControlService());

        selectedMedia.SelectedMedia = new MediaItem
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Test video",
            FilePath = "test.mp4",
            MediaType = MediaType.Video,
            CreatedAt = DateTime.UtcNow
        };

        Assert.True(viewModel.IsPlaybackUnsupported);
        Assert.True(viewModel.ShowUnsupportedPlaybackPlaceholder);
        Assert.False(viewModel.ShowNativeVideoHost);
        Assert.False(viewModel.ShowAudioTransportControls);
        Assert.Contains(UnsupportedMediaBackend.UnsupportedMessage, viewModel.PlayerStatusText);
    }

    [Fact]
    public void PlaybackControlSeekRequestSeeksThroughPlayerBackend()
    {
        var playbackControl = new PlaybackControlService();
        var backend = new FakeMediaBackend();

        _ = new MediaPlayerViewModel(
            new SelectedMediaService(),
            backend,
            new PlaybackTimelineService(),
            playbackControl);

        playbackControl.RequestSeek(42.5);

        Assert.Equal(TimeSpan.FromSeconds(42.5), backend.LastSeek);
    }

    [Fact]
    public void NoteJumpRequestReachesPlayerSeekPath()
    {
        var playbackControl = new PlaybackControlService();
        var backend = new FakeMediaBackend();

        _ = new MediaPlayerViewModel(
            new SelectedMediaService(),
            backend,
            new PlaybackTimelineService(),
            playbackControl);

        var notesViewModel = new NotesPanelViewModel(
            new FakeMediaNoteRepository(),
            new SelectedMediaService(),
            new PlaybackTimelineService(),
            playbackControl);

        notesViewModel.JumpToNote(new NoteItemViewModel(new MediaNote
        {
            Id = Guid.NewGuid(),
            TimestampSeconds = 17.25,
            Content = "Jump here"
        }));

        Assert.Equal(TimeSpan.FromSeconds(17.25), backend.LastSeek);
    }

    private sealed class FakeMediaBackend : IMediaBackend
    {
        public bool IsLoaded => true;
        public bool IsPlaying => false;
        public TimeSpan Duration => TimeSpan.Zero;
        public TimeSpan Position => TimeSpan.Zero;
        public TimeSpan? LastSeek { get; private set; }

        public event EventHandler? MediaOpened
        {
            add { }
            remove { }
        }

        public event EventHandler? MediaEnded
        {
            add { }
            remove { }
        }

        public event EventHandler<string>? MediaFailed
        {
            add { }
            remove { }
        }

        public event EventHandler<TimeSpan>? PositionChanged
        {
            add { }
            remove { }
        }

        public void Load(string path)
        {
        }

        public void Play()
        {
        }

        public void Pause()
        {
        }

        public void Stop()
        {
        }

        public void Seek(TimeSpan position)
        {
            LastSeek = position;
        }

        public void SetVolume(double volume)
        {
        }

        public void UpdateState()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeMediaNoteRepository : IMediaNoteRepository
    {
        public Task<List<MediaNote>> GetNotesForMediaAsync(Guid mediaId)
        {
            return Task.FromResult(new List<MediaNote>());
        }

        public Task AddNoteAsync(MediaNote note)
        {
            return Task.CompletedTask;
        }

        public Task<MediaNote?> UpdateNoteAsync(Guid noteId, string content)
        {
            return Task.FromResult<MediaNote?>(null);
        }

        public Task DeleteNoteAsync(Guid noteId)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }
}
