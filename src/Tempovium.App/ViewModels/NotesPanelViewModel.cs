using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces.Repositories;
using Tempovium.Core.Services;
using Tempovium.Services;

namespace Tempovium.ViewModels;

public class NotesPanelViewModel : ViewModelBase
{
    private readonly IMediaNoteRepository _noteRepository;
    private readonly SelectedMediaService _selectedMediaService;
    private readonly PlaybackTimelineService _timelineService;
    private readonly PlaybackControlService _playbackControlService;

    private string _newNoteContent = string.Empty;
    private NoteItemViewModel? _activeNote;
    private NoteItemViewModel? _editingNote;

    public ObservableCollection<NoteItemViewModel> Notes { get; } = new();

    public string NewNoteContent
    {
        get => _newNoteContent;
        set => SetProperty(ref _newNoteContent, value);
    }

    public NoteItemViewModel? ActiveNote
    {
        get => _activeNote;
        set => SetProperty(ref _activeNote, value);
    }

    public bool IsEditing => _editingNote is not null;

    public string AddNoteButtonText
    {
        get
        {
            if (IsEditing)
            {
                return "Guardar cambios";
            }

            var time = TimeSpan.FromSeconds(_timelineService.DisplayPositionSeconds);
            var formatted = time.TotalHours >= 1
                ? time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
                : time.ToString(@"mm\:ss", CultureInfo.InvariantCulture);

            return $"Agregar nota en {formatted}";
        }
    }

    public string AddNoteIcon => IsEditing ? "✓" : "+";

    public NotesPanelViewModel(
        IMediaNoteRepository noteRepository,
        SelectedMediaService selectedMediaService,
        PlaybackTimelineService timelineService,
        PlaybackControlService playbackControlService)
    {
        _noteRepository = noteRepository;
        _selectedMediaService = selectedMediaService;
        _timelineService = timelineService;
        _playbackControlService = playbackControlService;

        _selectedMediaService.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedMediaService.SelectedMedia))
            {
                await LoadNotesAsync();
            }
        };

        _timelineService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlaybackTimelineService.DisplayPositionSeconds) ||
                e.PropertyName == nameof(PlaybackTimelineService.PlaybackPositionSeconds))
            {
                OnPropertyChanged(nameof(AddNoteButtonText));
                OnPropertyChanged(nameof(AddNoteIcon));
                UpdateActiveNote();
            }
        };
    }

    public async Task LoadNotesAsync()
    {
        CancelEdit();
        Notes.Clear();

        var media = _selectedMediaService.SelectedMedia;
        if (media is null)
        {
            ActiveNote = null;
            return;
        }

        var notes = await _noteRepository.GetNotesForMediaAsync(media.Id);
        foreach (var note in notes)
        {
            Notes.Add(new NoteItemViewModel(note));
        }

        UpdateActiveNote();
    }

    public async Task AddNoteAsync()
    {
        if (_editingNote is not null)
        {
            await UpdateEditingNoteAsync();
            return;
        }

        var media = _selectedMediaService.SelectedMedia;
        if (media is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(NewNoteContent))
        {
            return;
        }

        var note = new MediaNote
        {
            Id = Guid.NewGuid(),
            UserId = media.UserId,
            MediaItemId = media.Id,
            TimestampSeconds = _timelineService.DisplayPositionSeconds,
            Content = NewNoteContent.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        await _noteRepository.AddNoteAsync(note);
        await _noteRepository.SaveChangesAsync();

        var item = new NoteItemViewModel(note);

        var index = 0;
        while (index < Notes.Count && Notes[index].TimestampSeconds < item.TimestampSeconds)
        {
            index++;
        }

        Notes.Insert(index, item);
        NewNoteContent = string.Empty;

        UpdateActiveNote();
    }

    public void StartEditNote(NoteItemViewModel note)
    {
        if (note == null)
        {
            return;
        }

        _editingNote = note;
        NewNoteContent = note.Content;
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(AddNoteButtonText));
        OnPropertyChanged(nameof(AddNoteIcon));
    }

    public void CancelEdit()
    {
        if (_editingNote is null && string.IsNullOrEmpty(NewNoteContent))
        {
            return;
        }

        _editingNote = null;
        NewNoteContent = string.Empty;
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(AddNoteButtonText));
        OnPropertyChanged(nameof(AddNoteIcon));
    }

    public void JumpToNote(NoteItemViewModel note)
    {
        if (note == null)
        {
            return;
        }

        _playbackControlService.RequestSeek(note.TimestampSeconds);
    }

    public async Task DeleteNoteAsync(NoteItemViewModel note)
    {
        if (note == null)
        {
            return;
        }

        await _noteRepository.DeleteNoteAsync(note.Id);
        await _noteRepository.SaveChangesAsync();

        Notes.Remove(note);

        if (ReferenceEquals(_editingNote, note))
        {
            CancelEdit();
        }

        if (ReferenceEquals(ActiveNote, note))
        {
            ActiveNote = null;
        }

        UpdateActiveNote();
    }

    private async Task UpdateEditingNoteAsync()
    {
        var note = _editingNote;
        if (note is null)
        {
            return;
        }

        var content = NewNoteContent.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var updated = await _noteRepository.UpdateNoteAsync(note.Id, content);
        if (updated is null)
        {
            return;
        }

        await _noteRepository.SaveChangesAsync();

        note.UpdateContent(updated.Content, updated.UpdatedAt);
        CancelEdit();
        UpdateActiveNote();
    }

    private void UpdateActiveNote()
    {
        if (Notes.Count == 0)
        {
            ActiveNote = null;
            return;
        }

        var currentTime = _timelineService.DisplayPositionSeconds;
        NoteItemViewModel? closest = null;

        foreach (var note in Notes)
        {
            if (note.TimestampSeconds <= currentTime)
            {
                closest = note;
            }
            else
            {
                break;
            }
        }

        foreach (var note in Notes)
        {
            note.IsActive = ReferenceEquals(note, closest);
        }

        ActiveNote = closest;
    }
}
