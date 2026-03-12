namespace WindowResizer.App.Tray;

public sealed class TrayApplicationContextOptions
{
    public bool RunAtSignIn { get; init; }

    public Action? ArrangeNowRequested { get; init; }

    public Action? SettingsRequested { get; init; }

    public Action<bool>? RunAtSignInChanged { get; init; }
}
