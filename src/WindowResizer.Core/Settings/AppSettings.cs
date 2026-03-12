namespace WindowResizer.Core.Settings;

public sealed record AppSettings(int WindowWidthPx, bool RunAtSignIn)
{
    public static AppSettings Default { get; } = new(ProductDefaults.DefaultWindowWidthPx, true);
}
