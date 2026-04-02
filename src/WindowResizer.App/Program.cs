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
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var runAtSignInService = new RunAtSignInService(
            settingsStore,
            new ScheduledTaskStartupRegistrationService());
        var windowEnumerator = new TopLevelWindowEnumerator();
        var windowSource = new EligibleVsCodeWindowSource();
        var arrangeService = new ManualArrangeService(
            windowSource,
            new Win32WindowPositioningService(),
            windowOrderResolver,
            new Win32WindowVisibilityOrderSynchronizer(),
            arrangeOperationTracker);
        TrayApplicationContext? context = null;
        using var windowObservationController = new AutoArrangeController(
            windowEnumerator,
            windowOrderResolver,
            arrangeOperationTracker,
            startupWindowsProvider: windowEnumerator.EnumerateTrackableVsCodeWindows,
            orderingUniverseProvider: windowEnumerator.EnumerateTrackableVsCodeWindows);

        void RequestArrangeNow()
        {
            try
            {
                _ = arrangeService.ArrangeNow(
                    settings.WindowWidthPx,
                    synchronizeTaskbarOrder: true,
                    preferCurrentScreenOrder: true,
                    normalizeZOrder: true);
            }
            catch (Exception ex)
            {
                context?.ShowNotification(
                    "Arrange failed",
                    ex.Message,
                    ToolTipIcon.Error);
            }
        }

        context = new TrayApplicationContext(new TrayApplicationContextOptions
        {
            RunAtSignIn = settings.RunAtSignIn,
            ArrangeNowRequested = RequestArrangeNow,
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

        windowObservationController.Start();
        Application.Run(context);
    }    
}
