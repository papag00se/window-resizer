using WindowResizer.App.Tray;

namespace WindowResizer.App.IntegrationTests;

public class TrayApplicationContextIntegrationTests
{
    [Fact]
    public void TrayApplicationContextCreatesTheRequiredMenuItems()
    {
        RunInStaThread(() =>
        {
            using var context = CreateContext(runAtSignIn: true);

            Assert.Equal("Arrange Now", context.ArrangeNowMenuItem.Text);
            Assert.Equal("Settings...", context.SettingsMenuItem.Text);
            Assert.Equal("Run at Sign-in", context.RunAtSignInMenuItem.Text);
            Assert.Equal("Exit", context.ExitMenuItem.Text);
            Assert.True(context.RunAtSignInMenuItem.Checked);
        });
    }

    [Fact]
    public void TrayApplicationContextRoutesMenuActionsToCallbacks()
    {
        var arrangeRequested = 0;
        var settingsRequested = 0;
        bool? toggledValue = null;

        RunInStaThread(() =>
        {
            using var context = CreateContext(
                runAtSignIn: false,
                arrangeNowRequested: () => arrangeRequested++,
                settingsRequested: () => settingsRequested++,
                runAtSignInChanged: value => toggledValue = value);

            context.ArrangeNowMenuItem.PerformClick();
            context.SettingsMenuItem.PerformClick();
            context.RunAtSignInMenuItem.PerformClick();
        });

        Assert.Equal(1, arrangeRequested);
        Assert.Equal(1, settingsRequested);
        Assert.True(toggledValue);
    }

    [Fact]
    public void TrayApplicationContextRoutesPrimaryLeftClickToArrangeNow()
    {
        var arrangeRequested = 0;

        RunInStaThread(() =>
        {
            using var context = CreateContext(
                runAtSignIn: false,
                arrangeNowRequested: () => arrangeRequested++);

            context.HandleTrayIconMouseClick(new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
            context.HandleTrayIconMouseClick(new MouseEventArgs(MouseButtons.Right, 1, 0, 0, 0));
        });

        Assert.Equal(1, arrangeRequested);
    }

    private static TrayApplicationContext CreateContext(
        bool runAtSignIn,
        Action? arrangeNowRequested = null,
        Action? settingsRequested = null,
        Action<bool>? runAtSignInChanged = null)
    {
        return new TrayApplicationContext(new TrayApplicationContextOptions
        {
            RunAtSignIn = runAtSignIn,
            ArrangeNowRequested = arrangeNowRequested,
            SettingsRequested = settingsRequested,
            RunAtSignInChanged = runAtSignInChanged
        });
    }

    private static void RunInStaThread(Action action)
    {
        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }
}
