using Microsoft.Data.Sqlite;

namespace Tempovium.Infrastructure.Persistence;

public static class TempoviumDataPaths
{
    public const string AppDirectoryName = "Tempovium";
    public const string DatabaseFileName = "tempovium.db";

    public static string GetAppDataDirectory()
    {
        return GetAppDataDirectory(GetAppDataRoot());
    }

    public static string GetAppDataDirectory(string appDataRoot)
    {
        var path = Path.Combine(appDataRoot, AppDirectoryName);
        Directory.CreateDirectory(path);
        return path;
    }

    public static string GetDatabasePath()
    {
        return GetDatabasePath(GetAppDataRoot());
    }

    public static string GetDatabasePath(string appDataRoot)
    {
        return Path.Combine(GetAppDataDirectory(appDataRoot), DatabaseFileName);
    }

    public static string GetSqliteConnectionString()
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath()
        }.ToString();
    }

    public static void CopyLegacyDatabaseIfNeeded()
    {
        CopyLegacyDatabaseIfNeeded(
            Path.Combine(Directory.GetCurrentDirectory(), DatabaseFileName),
            GetDatabasePath());
    }

    public static void CopyLegacyDatabaseIfNeeded(string legacyPath, string targetPath)
    {
        if (!File.Exists(legacyPath) || File.Exists(targetPath))
        {
            return;
        }

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        File.Copy(legacyPath, targetPath, overwrite: false);
    }

    private static string GetAppDataRoot()
    {
        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support");
        }

        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
