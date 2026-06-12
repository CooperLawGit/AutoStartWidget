using System.Runtime.InteropServices;

namespace AutoStartWidget.App.HotKeys;

internal static class WindowsAPI
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
