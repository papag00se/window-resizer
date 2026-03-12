using WindowResizer.App.Arrange;
using WindowResizer.Core.Layout;

namespace WindowResizer.App.IntegrationTests;

public class Win32WindowPositioningServiceIntegrationTests
{
    [Fact]
    public void ApplyWindowRectMovesAndResizesARealWindow()
    {
        RunInStaThread(() =>
        {
            using var form = new Form
            {
                Text = "WindowResizer Positioning Test Window",
                StartPosition = FormStartPosition.Manual,
                Left = 20,
                Top = 20,
                Width = 400,
                Height = 300
            };

            form.Show();

            var service = new Win32WindowPositioningService();
            var target = new WindowLayoutRect(60, 80, 640, 480);

            service.ApplyWindowRect(form.Handle, target);
            Application.DoEvents();

            Assert.Equal(target.X, form.Left);
            Assert.Equal(target.Y, form.Top);
            Assert.Equal(target.Width, form.Width);
            Assert.Equal(target.Height, form.Height);
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
