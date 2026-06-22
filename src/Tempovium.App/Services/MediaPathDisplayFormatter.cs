using System;
using System.IO;

namespace Tempovium.Services;

public static class MediaPathDisplayFormatter
{
    public static string Format(string? filePath, string managedMediaDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return string.Empty;
        }

        if (IsInsideDirectory(filePath, managedMediaDirectory))
        {
            return $"Medio interno: {Path.GetFileNameWithoutExtension(filePath)}";
        }

        return filePath;
    }

    private static bool IsInsideDirectory(string filePath, string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        string fullPath;
        string fullDirectory;

        try
        {
            fullPath = Path.GetFullPath(filePath);
            fullDirectory = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
        }
        catch
        {
            return false;
        }

        return fullPath.StartsWith(fullDirectory, comparison);
    }
}
