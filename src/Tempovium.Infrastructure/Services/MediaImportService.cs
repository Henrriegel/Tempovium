using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Models;
using Tempovium.Core.Services;
using Tempovium.Infrastructure.Persistence;

namespace Tempovium.Infrastructure.Services;

public class MediaImportService : IMediaImportService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IFileHashService _fileHashService;
    private readonly MediaFileTypeDetector _typeDetector;
    private readonly string _managedMediaDirectory;

    public MediaImportService(
        IMediaRepository mediaRepository,
        IFileHashService fileHashService,
        MediaFileTypeDetector typeDetector)
        : this(
            mediaRepository,
            fileHashService,
            typeDetector,
            TempoviumDataPaths.GetManagedMediaDirectory())
    {
    }

    public MediaImportService(
        IMediaRepository mediaRepository,
        IFileHashService fileHashService,
        MediaFileTypeDetector typeDetector,
        string managedMediaDirectory)
    {
        _mediaRepository = mediaRepository;
        _fileHashService = fileHashService;
        _typeDetector = typeDetector;
        _managedMediaDirectory = managedMediaDirectory;
        Directory.CreateDirectory(_managedMediaDirectory);
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

            var mediaId = Guid.NewGuid();
            var managedPath = GetManagedMediaPath(mediaId, filePath);

            try
            {
                Directory.CreateDirectory(_managedMediaDirectory);
                File.Copy(filePath, managedPath, overwrite: false);
            }
            catch
            {
                TryDeleteFile(managedPath);
                throw;
            }

            var media = new MediaItem
            {
                Id = mediaId,
                UserId = userId,
                Title = Path.GetFileNameWithoutExtension(filePath),
                FilePath = managedPath,
                OriginalSourcePath = filePath,
                FileSizeBytes = fileInfo.Length,
                FileHash = hash,
                MediaType = mediaType.Value,
                DurationSeconds = 0,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _mediaRepository.CreateAsync(media);
            }
            catch
            {
                TryDeleteFile(managedPath);
                throw;
            }

            result.ImportedItems.Add(media);
            result.ImportedCount++;
        }
        catch (Exception ex)
        {
            result.ErrorMessages.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
        }
    }

    private string GetManagedMediaPath(Guid mediaId, string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return Path.Combine(_managedMediaDirectory, $"{mediaId:N}{extension}");
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Import result still reports the original failure.
        }
    }
}
