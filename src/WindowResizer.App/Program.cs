using WindowResizer.App.Tray;
using WindowResizer.Core.Settings;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settingsStore = new AppSettingsStore();
        var settings = settingsStore.Load();

        var context = new TrayApplicationContext(new TrayApplicationContextOptions
        {
            RunAtSignIn = settings.RunAtSignIn,
            ArrangeNowRequested = () => { },
            SettingsRequested = () => { }
        });

        Application.Run(context);
    }    
}
