using Tempovium.Core.Entities;

namespace Tempovium.Core.Models;

public class MediaImportResult
{
    public int TotalFilesScanned { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int UnsupportedCount { get; set; }
    public int MissingCount { get; set; }
    public List<MediaItem> ImportedItems { get; set; } = [];
}