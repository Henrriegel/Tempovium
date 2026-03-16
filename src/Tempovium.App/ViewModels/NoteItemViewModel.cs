using System;
using Tempovium.Core.Entities;

namespace Tempovium.ViewModels;

public class NoteItemViewModel : ViewModelBase
{
    private bool _isActive;

    public MediaNote Note { get; }

    public Guid Id => Note.Id;
    public double TimestampSeconds => Note.TimestampSeconds;
    public string Content => Note.Content;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public NoteItemViewModel(MediaNote note)
    {
        Note = note;
    }
}