using System.Text.Json;

namespace WindowResizer.Core.Settings;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public AppSettingsStore(string? settingsFilePath = null)
    {
        SettingsFilePath = string.IsNullOrWhiteSpace(settingsFilePath)
            ? AppSettingsPaths.GetDefaultSettingsFilePath()
            : settingsFilePath;
    }

    public string SettingsFilePath { get; }

    public AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return AppSettings.Default;
        }

        using var stream = File.OpenRead(SettingsFilePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(stream, SerializerOptions);

        if (settings is null)
        {
            throw new InvalidDataException($"Settings file '{SettingsFilePath}' did not contain a valid settings object.");
        }

        AppSettingsValidator.Validate(settings);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        AppSettingsValidator.Validate(settings);

        var directoryPath = Path.GetDirectoryName(SettingsFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException($"Settings file path '{SettingsFilePath}' does not contain a directory.");
        }

        Directory.CreateDirectory(directoryPath);

        var tempFilePath = SettingsFilePath + ".tmp";
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(tempFilePath, json);

        if (File.Exists(SettingsFilePath))
        {
            var backupFilePath = SettingsFilePath + ".bak";
            File.Replace(tempFilePath, SettingsFilePath, backupFilePath, ignoreMetadataErrors: true);
            File.Delete(backupFilePath);
            return;
        }

        File.Move(tempFilePath, SettingsFilePath);
    }
}
