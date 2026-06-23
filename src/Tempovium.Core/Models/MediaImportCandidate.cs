using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tempovium.Core.Enums;

namespace Tempovium.Core.Models;

public class MediaImportCandidate : INotifyPropertyChanged
{
    private bool _isSelected;

    public string SourcePath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string Extension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? FileHash { get; set; }
    public bool IsDuplicate { get; set; }
    public bool IsPossibleDuplicate { get; set; }
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string StatusText { get; set; } = string.Empty;

    public bool CanSelect => !IsDuplicate;

    public string FileSizeText
    {
        get
        {
            if (FileSizeBytes <= 0)
            {
                return "Sin tamaño";
            }

            var megabytes = FileSizeBytes / 1024d / 1024d;
            return megabytes >= 1
                ? $"{megabytes:0.##} MB"
                : $"{FileSizeBytes / 1024d:0.##} KB";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
