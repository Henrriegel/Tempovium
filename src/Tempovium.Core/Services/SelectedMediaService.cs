using System.ComponentModel;
using Tempovium.Core.Entities;

namespace Tempovium.Core.Services;

public class SelectedMediaService : INotifyPropertyChanged
{
    private MediaItem? _selectedMedia;

    public MediaItem? SelectedMedia
    {
        get => _selectedMedia;
        set
        {
            _selectedMedia = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedMedia)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}