using System.Runtime.InteropServices;
namespace AutoStartWidget.App.HotKeys;

internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private const int WmHotKey = 0x0312;
    private bool registered;

    public event EventHandler? Pressed;

    public HotKeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public bool Register(Keys hotKey, bool enabled)
    {
        Unregister();
        if (hotKey == Keys.None || !enabled)
        {
            return true;
        }

        var num = 0;
        if ((hotKey & Keys.Shift) == Keys.Shift)
        {
            num |= 4;
        }
        if ((hotKey & Keys.Alt) == Keys.Alt)
        {
            num |= 1;
        }
        if ((hotKey & Keys.Control) == Keys.Control)
        {
            num |= 2;
        }
        var key = hotKey & Keys.KeyCode;
        registered = RegisterHotKey(Handle, (int)HotKeyID.Capture, num, (int)key) != 0;
        return registered;
    }

    public void Unregister()
    {
        if (!registered)
        {
            return;
        }

        UnregisterHotKey(Handle, (int)HotKeyID.Capture);
        registered = false;
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WmHotKey)
        {
            var id = (HotKeyID)m.WParam;
            switch (id)
            {
                case HotKeyID.Capture:
                    Pressed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }

    [DllImport("user32.dll")]
    private static extern int RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern int UnregisterHotKey(IntPtr hWnd, int id);
}
