using System.IO;
using System.Text.Json;
using Tempovium.Infrastructure.Persistence;

namespace Tempovium.Services;

public sealed class UiSettingsStore
{
    private readonly string _settingsPath;

    public UiSettingsStore()
        : this(Path.Combine(TempoviumDataPaths.GetAppDataDirectory(), "ui-settings.json"))
    {
    }

    public UiSettingsStore(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public UiSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new UiSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(_settingsPath)) ?? new UiSettings();
        }
        catch
        {
            return new UiSettings();
        }
    }

    public void Save(UiSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}

public sealed class UiSettings
{
    public string ThemePreference { get; set; } = "Default";
}
