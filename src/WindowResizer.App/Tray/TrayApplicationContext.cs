using System.Drawing;
using WindowResizer.Core;

namespace WindowResizer.App.Tray;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ToolStripMenuItem _arrangeNowMenuItem;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _runAtSignInMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;

    public TrayApplicationContext(TrayApplicationContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _arrangeNowMenuItem = new ToolStripMenuItem("Arrange Now");
        _settingsMenuItem = new ToolStripMenuItem("Settings...");
        _runAtSignInMenuItem = new ToolStripMenuItem("Run at Sign-in")
        {
            CheckOnClick = true,
            Checked = options.RunAtSignIn
        };
        _exitMenuItem = new ToolStripMenuItem("Exit");

        _arrangeNowMenuItem.Click += (_, _) => options.ArrangeNowRequested?.Invoke();
        _settingsMenuItem.Click += (_, _) => options.SettingsRequested?.Invoke();
        _runAtSignInMenuItem.Click += (_, _) => options.RunAtSignInChanged?.Invoke(_runAtSignInMenuItem.Checked);
        _exitMenuItem.Click += (_, _) => ExitThread();

        Menu = new ContextMenuStrip();
        Menu.Items.AddRange(
            [
                _arrangeNowMenuItem,
                _settingsMenuItem,
                _runAtSignInMenuItem,
                new ToolStripSeparator(),
                _exitMenuItem
            ]);

        TrayIcon = new NotifyIcon
        {
            Text = ProductDefaults.ApplicationName,
            Icon = SystemIcons.Application,
            ContextMenuStrip = Menu,
            Visible = true
        };

        TrayIcon.DoubleClick += (_, _) => options.ArrangeNowRequested?.Invoke();
    }

    public ContextMenuStrip Menu { get; }

    public NotifyIcon TrayIcon { get; }

    public ToolStripMenuItem ArrangeNowMenuItem => _arrangeNowMenuItem;

    public ToolStripMenuItem SettingsMenuItem => _settingsMenuItem;

    public ToolStripMenuItem RunAtSignInMenuItem => _runAtSignInMenuItem;

    public ToolStripMenuItem ExitMenuItem => _exitMenuItem;

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        TrayIcon.BalloonTipTitle = title;
        TrayIcon.BalloonTipText = message;
        TrayIcon.BalloonTipIcon = icon;
        TrayIcon.ShowBalloonTip(3000);
    }

    protected override void ExitThreadCore()
    {
        TrayIcon.Visible = false;
        TrayIcon.Dispose();
        Menu.Dispose();
        base.ExitThreadCore();
    }
}
