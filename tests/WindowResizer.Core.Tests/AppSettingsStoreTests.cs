using WindowResizer.Core;
using WindowResizer.Core.Settings;

namespace WindowResizer.Core.Tests;

public sealed class AppSettingsStoreTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        Path.GetTempPath(),
        ProductDefaults.ApplicationName,
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadReturnsDefaultSettingsWhenFileDoesNotExist()
    {
        var store = CreateStore();

        var settings = store.Load();

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public void LoadThrowsWhenPersistedWidthIsInvalid()
    {
        Directory.CreateDirectory(_testDirectory);
        var settingsFilePath = GetSettingsFilePath();
        File.WriteAllText(settingsFilePath, """
            {
              "windowWidthPx": 0,
              "runAtSignIn": true
            }
            """);

        var store = new AppSettingsStore(settingsFilePath);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => store.Load());

        Assert.Equal("WindowWidthPx", exception.ParamName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private AppSettingsStore CreateStore() => new(GetSettingsFilePath());

    private string GetSettingsFilePath() => Path.Combine(_testDirectory, "settings.json");
}
