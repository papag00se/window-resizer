using WindowResizer.Core.Settings;

namespace WindowResizer.App.Startup;

public sealed class RunAtSignInService
{
    private readonly AppSettingsStore _settingsStore;
    private readonly IStartupRegistrationService _startupRegistrationService;

    public RunAtSignInService(
        AppSettingsStore settingsStore,
        IStartupRegistrationService startupRegistrationService)
    {
        _settingsStore = settingsStore;
        _startupRegistrationService = startupRegistrationService;
    }

    public AppSettings SetEnabled(AppSettings currentSettings, bool enabled, string executablePath)
    {
        var updatedSettings = currentSettings with
        {
            RunAtSignIn = enabled
        };

        if (enabled)
        {
            _startupRegistrationService.Enable(executablePath);
        }
        else
        {
            _startupRegistrationService.Disable();
        }

        _settingsStore.Save(updatedSettings);
        return updatedSettings;
    }
}
