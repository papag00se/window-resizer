using WindowResizer.App.Settings;
using WindowResizer.Core.Settings;

namespace WindowResizer.App.IntegrationTests;

public class SettingsFormIntegrationTests
{
    [Fact]
    public void SettingsFormSeedsTheWindowWidthInputFromCurrentSettings()
    {
        RunInStaThread(() =>
        {
            using var form = new SettingsForm(new AppSettings(WindowWidthPx: 1440, RunAtSignIn: true));

            Assert.Equal(1440, decimal.ToInt32(form.WindowWidthInput.Value));
        });
    }

    [Fact]
    public void SettingsFormSavesUpdatedWidthAndPreservesRunAtSignIn()
    {
        RunInStaThread(() =>
        {
            using var form = new SettingsForm(new AppSettings(WindowWidthPx: 1823, RunAtSignIn: false));

            form.WindowWidthInput.Value = 1366;
            form.ApplyChanges();

            Assert.Equal(new AppSettings(WindowWidthPx: 1366, RunAtSignIn: false), form.SavedSettings);
        });
    }

    [Fact]
    public void SettingsFormRejectsZeroAsAnInteractiveInput()
    {
        RunInStaThread(() =>
        {
            using var form = new SettingsForm(new AppSettings(WindowWidthPx: 1823, RunAtSignIn: true));

            Assert.Equal(1, decimal.ToInt32(form.WindowWidthInput.Minimum));
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
