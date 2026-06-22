using Tempovium.Core.Entities;
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

    public async Task<MediaImportResult> ImportFolderAsync(Guid userId, string folderPath)
    {
        var result = new MediaImportResult();

        if (!Directory.Exists(folderPath))
        {
            result.MissingCount = 1;
            result.ErrorMessages.Add($"Folder not found: {folderPath}");
            return result;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            result.ErrorMessages.Add(ex.Message);
            return result;
        }

        result.TotalFilesScanned = files.Length;

        foreach (var file in files)
        {
            await ImportExistingFileAsync(userId, file, result);
        }

        return result;
    }

    public async Task<MediaImportResult> ImportFileAsync(Guid userId, string filePath)
    {
        var result = new MediaImportResult
        {
            TotalFilesScanned = 1
        };

        if (!File.Exists(filePath))
        {
            result.MissingCount = 1;
            result.ErrorMessages.Add($"File not found: {filePath}");
            return result;
        }

        await ImportExistingFileAsync(userId, filePath, result);

        return result;
    }

    private async Task ImportExistingFileAsync(Guid userId, string filePath, MediaImportResult result)
    {
        var mediaType = _typeDetector.DetectFromPath(filePath);

        if (mediaType is null)
        {
            result.UnsupportedCount++;
            return;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            var hash = await _fileHashService.ComputeHashAsync(filePath);

            var existing = await _mediaRepository.GetByHashAsync(userId, hash);

            if (existing != null)
            {
                result.DuplicateCount++;
                return;
            }

            var media = new MediaItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                OriginalSourcePath = filePath,
                FileSizeBytes = fileInfo.Length,
                FileHash = hash,
                MediaType = mediaType.Value,
                DurationSeconds = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _mediaRepository.CreateAsync(media);

            result.ImportedItems.Add(media);
            result.ImportedCount++;
        }
        catch (Exception ex)
        {
            result.ErrorMessages.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
        }
    }
}
