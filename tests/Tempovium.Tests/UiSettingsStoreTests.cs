using Tempovium.Services;

namespace Tempovium.Tests;

public class UiSettingsStoreTests
{
    [Fact]
    public void SavesAndLoadsThemePreference()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TempoviumUiSettingsTests", Guid.NewGuid().ToString("N"));
        var settingsPath = Path.Combine(directory, "ui-settings.json");

        try
        {
            var store = new UiSettingsStore(settingsPath);
            store.Save(new UiSettings { ThemePreference = "Dark" });

            var loaded = store.Load();

            Assert.Equal("Dark", loaded.ThemePreference);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
