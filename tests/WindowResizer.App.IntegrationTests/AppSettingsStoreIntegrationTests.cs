using WindowResizer.Core;
using WindowResizer.Core.Settings;

namespace WindowResizer.App.IntegrationTests;

public sealed class AppSettingsStoreIntegrationTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        Path.GetTempPath(),
        ProductDefaults.ApplicationName,
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveThenLoadRoundTripsSettingsThroughTheFilesystem()
    {
        var store = CreateStore();
        var expected = new AppSettings(WindowWidthPx: 1440, RunAtSignIn: false);

        store.Save(expected);
        var actual = store.Load();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SaveReplacesExistingSettingsWithoutLeavingTempFilesBehind()
    {
        var store = CreateStore();

        store.Save(new AppSettings(WindowWidthPx: 1823, RunAtSignIn: true));
        store.Save(new AppSettings(WindowWidthPx: 1366, RunAtSignIn: false));

        var savedFiles = Directory.GetFiles(_testDirectory);

        Assert.Contains(Path.Combine(_testDirectory, "settings.json"), savedFiles);
        Assert.DoesNotContain(Path.Combine(_testDirectory, "settings.json.tmp"), savedFiles);
        Assert.DoesNotContain(Path.Combine(_testDirectory, "settings.json.bak"), savedFiles);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private AppSettingsStore CreateStore() => new(Path.Combine(_testDirectory, "settings.json"));
}
