namespace WindowResizer.Core.Settings;

public static class AppSettingsValidator
{
    public static void Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.WindowWidthPx <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings.WindowWidthPx),
                settings.WindowWidthPx,
                "Window width must be a whole-number pixel value greater than zero.");
        }
    }
}
