using System.Runtime.InteropServices;

namespace AutoStartWidget.App.Screenshot;

internal static class WindowTargetFinder
{
    private const uint GaRoot = 2;

    public static Rectangle? FindWindowBounds(Point screenPoint)
    {
        var handle = WindowFromPoint(screenPoint);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        var root = GetAncestor(handle, GaRoot);
        if (root == IntPtr.Zero || !IsWindowVisible(root) || !GetWindowRect(root, out var rect))
        {
            return null;
        }

        var bounds = rect.ToRectangle();
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return null;
        }

        return bounds;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeRect
    {
        private readonly int left;
        private readonly int top;
        private readonly int right;
        private readonly int bottom;

        public Rectangle ToRectangle()
        {
            return Rectangle.FromLTRB(left, top, right, bottom);
        }
    }
}
