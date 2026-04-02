using System.Drawing;
using WindowResizer.Core;

namespace WindowResizer.App.Tray;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ToolStripMenuItem _arrangeNowMenuItem;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _runAtSignInMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;
    private readonly Icon _trayIconAsset;
    private readonly Action? _arrangeNowRequested;

    public TrayApplicationContext(TrayApplicationContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _arrangeNowRequested = options.ArrangeNowRequested;

        _arrangeNowMenuItem = new ToolStripMenuItem("Arrange Now");
        _settingsMenuItem = new ToolStripMenuItem("Settings...");
        _runAtSignInMenuItem = new ToolStripMenuItem("Run at Sign-in")
        {
            CheckOnClick = true,
            Checked = options.RunAtSignIn
        };
        _exitMenuItem = new ToolStripMenuItem("Exit");

        _arrangeNowMenuItem.Click += (_, _) => RequestArrangeNow();
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

        _trayIconAsset = LayeredWindowTrayIcon.Create();
        TrayIcon = new NotifyIcon
        {
            Text = ProductDefaults.ApplicationName,
            Icon = _trayIconAsset,
            ContextMenuStrip = Menu,
            Visible = true
        };

        TrayIcon.MouseClick += (_, e) => HandleTrayIconMouseClick(e);
    }

    public ContextMenuStrip Menu { get; }

    public NotifyIcon TrayIcon { get; }

    public ToolStripMenuItem ArrangeNowMenuItem => _arrangeNowMenuItem;

    public ToolStripMenuItem SettingsMenuItem => _settingsMenuItem;

    public ToolStripMenuItem RunAtSignInMenuItem => _runAtSignInMenuItem;

    public ToolStripMenuItem ExitMenuItem => _exitMenuItem;

    internal void HandleTrayIconMouseClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            RequestArrangeNow();
        }
    }

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
        _trayIconAsset.Dispose();
        Menu.Dispose();
        base.ExitThreadCore();
    }

    private void RequestArrangeNow()
    {
        _arrangeNowRequested?.Invoke();
    }
}
