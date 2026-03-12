using WindowResizer.App.Startup;
using WindowResizer.Core.Settings;

namespace WindowResizer.App.IntegrationTests;

public sealed class RunAtSignInServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "WindowResizer", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SetEnabledPersistsUpdatedSettingsAndCallsEnable()
    {
        var store = new AppSettingsStore(Path.Combine(_testDirectory, "settings.json"));
        var startupService = new FakeStartupRegistrationService();
        var service = new RunAtSignInService(store, startupService);

        var updatedSettings = service.SetEnabled(
            new AppSettings(WindowWidthPx: 1823, RunAtSignIn: false),
            enabled: true,
            executablePath: @"C:\Apps\WindowResizer.exe");

        Assert.True(updatedSettings.RunAtSignIn);
        Assert.Equal(@"C:\Apps\WindowResizer.exe", startupService.EnabledExecutablePath);
        Assert.Null(startupService.DisableCallCount);
        Assert.True(store.Load().RunAtSignIn);
    }

    [Fact]
    public void SetEnabledDisablesStartupAndPersistsTheToggle()
    {
        var store = new AppSettingsStore(Path.Combine(_testDirectory, "settings.json"));
        var startupService = new FakeStartupRegistrationService();
        var service = new RunAtSignInService(store, startupService);

        var updatedSettings = service.SetEnabled(
            new AppSettings(WindowWidthPx: 1823, RunAtSignIn: true),
            enabled: false,
            executablePath: @"C:\Apps\WindowResizer.exe");

        Assert.False(updatedSettings.RunAtSignIn);
        Assert.Equal(1, startupService.DisableCallCount);
        Assert.False(store.Load().RunAtSignIn);
    }

    [Fact]
    public void StartupTaskXmlBuilderIncludesRestartOnFailureAndExecutablePath()
    {
        var xml = StartupTaskXmlBuilder.Build(@"C:\Apps\WindowResizer.exe");

        Assert.Contains("<LogonTrigger>", xml);
        Assert.Contains("<RestartOnFailure>", xml);
        Assert.Contains(@"<Command>C:\Apps\WindowResizer.exe</Command>", xml);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private sealed class FakeStartupRegistrationService : IStartupRegistrationService
    {
        public string? EnabledExecutablePath { get; private set; }

        public int? DisableCallCount { get; private set; }

        public void Enable(string executablePath)
        {
            EnabledExecutablePath = executablePath;
        }

        public void Disable()
        {
            DisableCallCount = (DisableCallCount ?? 0) + 1;
        }
    }
}
