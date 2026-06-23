namespace Tempovium.Core.Models;

public class MediaImportPreviewResult
{
    public List<MediaImportCandidate> Candidates { get; set; } = [];
    public int TotalFilesScanned { get; set; }
    public int SupportedCount => Candidates.Count;
    public int DuplicateCount { get; set; }
    public int UnsupportedCount { get; set; }
    public List<string> ErrorMessages { get; set; } = [];
    public int ErrorCount => ErrorMessages.Count;
}
