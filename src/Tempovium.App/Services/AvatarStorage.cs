using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Tempovium.Infrastructure.Persistence;

namespace Tempovium.Services;

public static class AvatarStorage
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    public static string CopyToManagedAvatar(string sourcePath, Guid userId, string? avatarDirectory = null)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("No se encontró el avatar seleccionado.", sourcePath);
        }

        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Formato de avatar no compatible.");
        }

        avatarDirectory ??= TempoviumDataPaths.GetAvatarDirectory();
        Directory.CreateDirectory(avatarDirectory);

        var destination = Path.Combine(avatarDirectory, $"{userId:N}{extension}");
        var temp = destination + ".tmp";

        try
        {
            File.Copy(sourcePath, temp, overwrite: true);
            File.Move(temp, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }

        return destination;
    }

    public static Bitmap? LoadImage(string? avatarPath)
    {
        if (string.IsNullOrWhiteSpace(avatarPath) || !File.Exists(avatarPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(avatarPath);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}
