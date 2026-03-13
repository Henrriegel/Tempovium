using Tempovium.Core.Entities;

namespace Tempovium.Core.Interfaces;

public interface IMediaImportService
{
    Task<List<MediaItem>> ImportFolderAsync(Guid userId, string folderPath);
}