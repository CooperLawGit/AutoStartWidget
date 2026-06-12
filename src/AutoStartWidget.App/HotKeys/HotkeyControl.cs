namespace AutoStartWidget.App.HotKeys;

internal sealed class HotkeyControl : UserControl
{
    private Keys hotKey;

    public HotkeyControl()
    {
        SetStyle(ControlStyles.UserPaint, false);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var baseParams = base.CreateParams;
            baseParams.ClassName = "msctls_hotkey32";
            return baseParams;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        Hotkey = hotKey;
    }

    public Keys Hotkey
    {
        get
        {
            if (IsHandleCreated)
            {
                var num = WindowsAPI.SendMessage(Handle, 1026, IntPtr.Zero, IntPtr.Zero);
                var num2 = num;
                var keys = (Keys)(num2 & 255);
                num2 >>= 8;
                if ((num2 & 4) != 0)
                {
                    keys |= Keys.Alt;
                }
                if ((num2 & 2) != 0)
                {
                    keys |= Keys.Control;
                }
                if ((num2 & 1) != 0)
                {
                    keys |= Keys.Shift;
                }
                return keys;
            }
            return hotKey;
        }
        set
        {
            if (IsHandleCreated)
            {
                var num = 0;
                if ((value & Keys.Alt) != Keys.None)
                {
                    num |= 4;
                }
                if ((value & Keys.Control) != Keys.None)
                {
                    num |= 2;
                }
                if ((value & Keys.Shift) != Keys.None)
                {
                    num |= 1;
                }
                WindowsAPI.SendMessage(Handle, 1025, (IntPtr)(num << 8 | (int)(value & Keys.KeyCode)), IntPtr.Zero);
                return;
            }
            hotKey = value;
        }
    }
}
