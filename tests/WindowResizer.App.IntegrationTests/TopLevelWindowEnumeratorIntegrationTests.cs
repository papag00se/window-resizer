using System.Collections.Concurrent;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.IntegrationTests;

public class TopLevelWindowEnumeratorIntegrationTests
{
    [Fact]
    public void EnumerateAllIncludesARealVisibleTopLevelWindow()
    {
        using var ready = new ManualResetEventSlim(false);
        using var completed = new ManualResetEventSlim(false);
        using var threadStarted = new ManualResetEventSlim(false);
        var capturedHandles = new ConcurrentQueue<nint>();

        var thread = new Thread(() =>
        {
            threadStarted.Set();

            using var form = new Form
            {
                Text = "WindowResizer Integration Test Window",
                StartPosition = FormStartPosition.Manual,
                Left = -2000,
                Top = 0
            };

            form.Shown += (_, _) =>
            {
                capturedHandles.Enqueue(form.Handle);
                ready.Set();
            };

            Application.Run(form);
            completed.Set();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        threadStarted.Wait(TimeSpan.FromSeconds(5));
        ready.Wait(TimeSpan.FromSeconds(5));

        var enumerator = new TopLevelWindowEnumerator();
        var windows = enumerator.EnumerateAll();

        Assert.True(capturedHandles.TryPeek(out var handle));

        var window = Assert.Single(windows.Where(window => window.Handle == handle));
        Assert.True(window.IsVisible);
        Assert.Equal("WindowResizer Integration Test Window", window.Title);

        NativeMethodsForTests.PostCloseMessage(handle);
        completed.Wait(TimeSpan.FromSeconds(5));
        thread.Join();
    }

    private static class NativeMethodsForTests
    {
        private const uint WmClose = 0x0010;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

        public static void PostCloseMessage(nint handle)
        {
            _ = PostMessage(handle, WmClose, nint.Zero, nint.Zero);
        }
    }
}
