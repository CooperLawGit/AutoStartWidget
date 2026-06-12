using AutoStartWidget.Core;
using Microsoft.Win32;

namespace AutoStartWidget.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon notifyIcon;
    private readonly System.Windows.Forms.Timer timer;
    private readonly BreakReminderHistory breakReminderHistory = new();
    private readonly WorkSessionClock workSessionClock;
    private EyeCareSettings settings;
    private bool breakActive;

    public TrayApplicationContext(EyeCareSettings settings)
    {
        settings.Validate();
        this.settings = settings;
        workSessionClock = new WorkSessionClock(DateTimeOffset.Now);

        notifyIcon = new NotifyIcon
        {
            Icon = AppIcons.Main,
            Text = "AutoStartWidget",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        timer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };
        timer.Tick += OnTick;
        timer.Start();
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("截图", null, (_, _) => ScreenshotToolLauncher.Start(null!));
        menu.Items.Add("设置", null, (_, _) => OpenSettings());
        menu.Items.Add("立即护眼", null, (_, _) => ShowProtectionScreen(countSkippedBreak: false));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildStartupMenuItem());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        return menu;
    }

    private static ToolStripMenuItem BuildStartupMenuItem()
    {
        var item = new ToolStripMenuItem("开机自启")
        {
            Checked = AutoStartupManager.IsEnabled()
        };

        item.Click += (_, _) =>
        {
            var enabled = !AutoStartupManager.IsEnabled();
            if (!AutoStartupManager.SetEnabled(enabled))
            {
                MessageBox.Show(
                    "无法修改开机自启设置。",
                    "AutoStartWidget",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            item.Checked = enabled;
        };

        return item;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (breakActive)
        {
            return;
        }

        if (!workSessionClock.IsBreakDue(DateTimeOffset.Now, settings))
        {
            return;
        }

        ShowProtectionScreen(countSkippedBreak: true);
    }

    private void ShowProtectionScreen(bool countSkippedBreak)
    {
        if (breakActive)
        {
            return;
        }

        breakActive = true;
        using var screen = new ProtectionScreenForm(settings, breakReminderHistory.EarlyCloseCount);
        screen.ShowDialog();
        breakReminderHistory.RecordBreakCompleted(screen.CompletedNaturally, countSkippedBreak);
        workSessionClock.Reset(DateTimeOffset.Now);
        breakActive = false;
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(settings);
        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        settings = form.SavedSettings;
        AppSettingsStore.Save(settings);
        workSessionClock.Reset(DateTimeOffset.Now);
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
        {
            workSessionClock.Lock(DateTimeOffset.Now);
            return;
        }

        if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.SessionLogon)
        {
            workSessionClock.Unlock(DateTimeOffset.Now);
        }
    }

    protected override void ExitThreadCore()
    {
        timer.Stop();
        timer.Dispose();
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
