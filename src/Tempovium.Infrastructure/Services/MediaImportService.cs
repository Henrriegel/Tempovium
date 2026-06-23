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
        var preview = await ScanFolderAsync(userId, folderPath);
        foreach (var candidate in preview.Candidates)
        {
            candidate.IsSelected = true;
        }

        var result = await ImportCandidatesAsync(userId, preview.Candidates);
        result.TotalFilesScanned = preview.TotalFilesScanned;
        result.UnsupportedCount += preview.UnsupportedCount;
        result.ErrorMessages.AddRange(preview.ErrorMessages);
        return result;
    }

    public async Task<MediaImportPreviewResult> ScanFolderAsync(Guid userId, string folderPath)
    {
        var result = new MediaImportPreviewResult();

        if (!Directory.Exists(folderPath))
        {
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
            var mediaType = _typeDetector.DetectFromPath(file);

            if (mediaType is null)
            {
                result.UnsupportedCount++;
                continue;
            }

            try
            {
                var fileInfo = new FileInfo(file);
                var existingBySource = await _mediaRepository.GetByOriginalSourcePathAsync(userId, file);
                var isExactSourceDuplicate = IsExactSourceDuplicate(existingBySource, fileInfo);
                var isPossibleDuplicate = existingBySource is not null;

                if (isPossibleDuplicate)
                {
                    result.DuplicateCount++;
                }

                result.Candidates.Add(new MediaImportCandidate
                {
                    SourcePath = file,
                    DisplayName = Path.GetFileNameWithoutExtension(file),
                    MediaType = mediaType.Value,
                    Extension = Path.GetExtension(file),
                    FileSizeBytes = fileInfo.Length,
                    SourceLastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
                    FileHash = null,
                    ExistingMediaId = existingBySource?.Id,
                    IsDuplicate = false,
                    IsPossibleDuplicate = isPossibleDuplicate,
                    IsExactSourceDuplicate = isExactSourceDuplicate,
                    DuplicateReason = isExactSourceDuplicate
                        ? "Misma ruta, tamaño y fecha de modificación"
                        : isPossibleDuplicate
                            ? "Misma ruta de origen"
                            : string.Empty,
                    IsSelected = !isPossibleDuplicate,
                    StatusText = isExactSourceDuplicate
                        ? "Duplicado exacto"
                        : isPossibleDuplicate
                            ? "Posible duplicado"
                            : "Listo para revisar"
                });
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<MediaImportResult> ImportCandidatesAsync(Guid userId, IEnumerable<MediaImportCandidate> candidates)
    {
        var candidateList = candidates.ToList();
        var result = new MediaImportResult
        {
            TotalFilesScanned = candidateList.Count
        };

        foreach (var candidate in candidateList)
        {
            if (!candidate.IsSelected)
            {
                continue;
            }

            await ImportExistingFileAsync(userId, candidate.SourcePath, result, candidate.DisplayName, candidate.FileHash);
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

    private async Task ImportExistingFileAsync(
        Guid userId,
        string filePath,
        MediaImportResult result,
        string? displayName = null,
        string? knownHash = null)
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
            var existingBySource = await _mediaRepository.GetByOriginalSourcePathAsync(userId, filePath);
            if (IsExactSourceDuplicate(existingBySource, fileInfo))
            {
                result.DuplicateCount++;
                return;
            }

            var hash = string.IsNullOrWhiteSpace(knownHash)
                ? await _fileHashService.ComputeHashAsync(filePath)
                : knownHash;

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
                Title = string.IsNullOrWhiteSpace(displayName)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : displayName.Trim(),
                FilePath = managedPath,
                OriginalSourcePath = filePath,
                OriginalSourceLastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
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

    private static bool IsExactSourceDuplicate(MediaItem? existing, FileInfo sourceFile)
    {
        return existing?.OriginalSourceLastWriteTimeUtc is DateTime sourceLastWriteTimeUtc &&
               existing.FileSizeBytes == sourceFile.Length &&
               sourceLastWriteTimeUtc == sourceFile.LastWriteTimeUtc;
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
