using System.Runtime.InteropServices;

namespace AutoStartWidget.App.Screenshot;

internal static class WindowTargetFinder
{
    public static IReadOnlyList<Rectangle> FindVisibleWindowBounds(Rectangle virtualBounds)
    {
        var windows = new List<Rectangle>();
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || !GetWindowRect(handle, out var rect))
            {
                return true;
            }

            var bounds = rect.ToRectangle();
            if (IsUsableWindowBounds(bounds, virtualBounds))
            {
                windows.Add(bounds);
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static bool IsUsableWindowBounds(Rectangle bounds, Rectangle virtualBounds)
    {
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return false;
        }

        if (!bounds.IntersectsWith(virtualBounds))
        {
            return false;
        }

        return bounds != virtualBounds;
    }

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

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
