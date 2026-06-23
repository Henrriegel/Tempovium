using Tempovium.Core.Models;

namespace Tempovium.Core.Interfaces;

public interface IMediaImportService
{
    Task<MediaImportPreviewResult> ScanFolderAsync(Guid userId, string folderPath);

    Task<MediaImportResult> ImportCandidatesAsync(Guid userId, IEnumerable<MediaImportCandidate> candidates);

    Task<MediaImportResult> ImportFolderAsync(Guid userId, string folderPath);

    Task<MediaImportResult> ImportFileAsync(Guid userId, string filePath);
}
