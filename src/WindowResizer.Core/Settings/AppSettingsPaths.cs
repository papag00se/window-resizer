namespace WindowResizer.Core.Settings;

public static class AppSettingsPaths
{
    public static string GetDefaultSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, ProductDefaults.ApplicationName, "settings.json");
    }
}
