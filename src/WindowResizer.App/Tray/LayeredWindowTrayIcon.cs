using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WindowResizer.App.Tray;

internal static class LayeredWindowTrayIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        DrawWindow(graphics, new Rectangle(1, 5, 8, 8), Color.FromArgb(160, 214, 224, 235), Color.FromArgb(122, 138, 160));
        DrawWindow(graphics, new Rectangle(4, 3, 8, 8), Color.FromArgb(192, 107, 168, 255), Color.FromArgb(58, 92, 141));
        DrawWindow(graphics, new Rectangle(7, 1, 8, 8), Color.FromArgb(255, 45, 120, 216), Color.FromArgb(29, 78, 151));

        var handle = bitmap.GetHicon();

        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void DrawWindow(Graphics graphics, Rectangle bounds, Color fillColor, Color borderColor)
    {
        using var fillBrush = new SolidBrush(fillColor);
        using var titleBrush = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
        using var borderPen = new Pen(borderColor);

        var windowBounds = new Rectangle(bounds.X, bounds.Y + 1, bounds.Width - 1, bounds.Height - 1);
        var titleBarBounds = new Rectangle(windowBounds.X + 1, windowBounds.Y + 1, windowBounds.Width - 2, 2);

        graphics.FillRectangle(fillBrush, windowBounds);
        graphics.DrawRectangle(borderPen, windowBounds);
        graphics.FillRectangle(titleBrush, titleBarBounds);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);
}
