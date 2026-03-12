using WindowResizer.App.Tray;
using WindowResizer.Core.Settings;
using WindowResizer.App.Settings;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settingsStore = new AppSettingsStore();
        var settings = settingsStore.Load();

        TrayApplicationContext? context = null;

        context = new TrayApplicationContext(new TrayApplicationContextOptions
        {
            RunAtSignIn = settings.RunAtSignIn,
            ArrangeNowRequested = () => { },
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
            }
        });

        Application.Run(context);
    }    
}
