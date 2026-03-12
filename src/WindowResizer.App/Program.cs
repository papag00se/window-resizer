using WindowResizer.App.Tray;
using WindowResizer.App.Arrange;
using WindowResizer.App.Startup;
using WindowResizer.Core.Settings;
using WindowResizer.App.Settings;
using WindowResizer.Core.Windows;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settingsStore = new AppSettingsStore();
        var settings = settingsStore.Load();
        var windowOrderResolver = new HeuristicWindowOrderResolver();
        var runAtSignInService = new RunAtSignInService(
            settingsStore,
            new ScheduledTaskStartupRegistrationService());
        var windowEnumerator = new TopLevelWindowEnumerator();
        var arrangeService = new ManualArrangeService(
            new EligibleVsCodeWindowSource(),
            new Win32WindowPositioningService(),
            windowOrderResolver);
        using var autoArrangeController = new AutoArrangeController(
            windowEnumerator,
            arrangeService,
            windowOrderResolver,
            () => settings.WindowWidthPx);
        autoArrangeController.Start();

        TrayApplicationContext? context = null;

        context = new TrayApplicationContext(new TrayApplicationContextOptions
        {
            RunAtSignIn = settings.RunAtSignIn,
            ArrangeNowRequested = () => arrangeService.ArrangeNow(settings.WindowWidthPx),
            SettingsRequested = () =>
            {
                using var settingsForm = new SettingsForm(settings);
                if (settingsForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                settings = settingsForm.SavedSettings;
                settingsStore.Save(settings);
                context!.ShowNotification("Settings saved", "Window width updated.");
            },
            RunAtSignInChanged = enabled =>
            {
                settings = runAtSignInService.SetEnabled(
                    settings,
                    enabled,
                    Environment.ProcessPath ?? Application.ExecutablePath);
                context!.ShowNotification(
                    "Startup updated",
                    enabled ? "Window Resizer will run at sign-in." : "Window Resizer will no longer run at sign-in.");
            }
        });

        Application.Run(context);
    }    
}
