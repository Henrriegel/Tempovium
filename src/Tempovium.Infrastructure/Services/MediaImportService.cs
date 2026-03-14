using Tempovium.Core.Entities;
using Tempovium.Core.Enums;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Models;
using Tempovium.Core.Services;

namespace Tempovium.Infrastructure.Services;

public class MediaImportService : IMediaImportService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IFileHashService _fileHashService;
    private readonly MediaFileTypeDetector _typeDetector;

    public MediaImportService(
        IMediaRepository mediaRepository,
        IFileHashService fileHashService,
        MediaFileTypeDetector typeDetector)
    {
        _mediaRepository = mediaRepository;
        _fileHashService = fileHashService;
        _typeDetector = typeDetector;
    }

    public async Task<List<MediaItem>> ImportFolderAsync(Guid userId, string folderPath)
    {
        var results = new List<MediaItem>();

        if (!Directory.Exists(folderPath))
            return results;

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var mediaType = _typeDetector.DetectFromPath(file);

            if (mediaType is null)
                continue;

            var hash = await _fileHashService.ComputeHashAsync(file);

            var existing = await _mediaRepository.GetByHashAsync(hash);

            if (existing != null)
                continue;

            var media = new MediaItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = Path.GetFileNameWithoutExtension(file),
                FilePath = file,
                FileHash = hash,
                MediaType = mediaType.Value,
                DurationSeconds = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _mediaRepository.CreateAsync(media);

            results.Add(media);
        }

        return results;
    }
}