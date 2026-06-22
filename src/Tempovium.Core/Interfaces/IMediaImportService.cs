using Tempovium.Core.Models;

namespace Tempovium.Core.Interfaces;

public interface IMediaImportService
{
    Task<MediaImportResult> ImportFolderAsync(Guid userId, string folderPath);

    Task<MediaImportResult> ImportFileAsync(Guid userId, string filePath);
}
