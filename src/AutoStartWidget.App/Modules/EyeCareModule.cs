using AutoStartWidget.Core;
using Microsoft.Win32;

namespace AutoStartWidget.App.Modules;

internal sealed class EyeCareModule : IDisposable
{
    private readonly BreakReminderHistory breakReminderHistory = new();
    private WorkSessionClock workSessionClock = new(DateTimeOffset.Now);
    private EyeCareSettings settings;
    private bool enabled;
    private bool breakActive;
    private System.Windows.Forms.Timer? timer;
    private bool sessionSwitchSubscribed;

    public EyeCareModule(EyeCareSettings settings, bool enabled)
    {
        this.settings = settings;
        this.enabled = enabled;
        if (enabled)
        {
            StartRuntime();
        }
    }

    public bool Enabled => enabled;
    public EyeCareSettings Settings => settings;

    public void SetEnabled(bool value)
    {
        if (enabled == value)
        {
            return;
        }

        enabled = value;
        workSessionClock = new WorkSessionClock(DateTimeOffset.Now);
        if (enabled)
        {
            StartRuntime();
        }
        else
        {
            StopRuntime();
        }
    }

    public void UpdateSettings(EyeCareSettings value)
    {
        value.Validate();
        settings = value;
        workSessionClock.Reset(DateTimeOffset.Now);
    }

    public void Tick()
    {
        if (!enabled || breakActive)
        {
            return;
        }

        if (workSessionClock.IsBreakDue(DateTimeOffset.Now, settings))
        {
            ShowProtectionScreen(countSkippedBreak: true);
        }
    }

    public void ShowNow()
    {
        if (!enabled)
        {
            return;
        }

        ShowProtectionScreen(countSkippedBreak: false);
    }

    public EyeCareSettings OpenSettings(IWin32Window owner)
    {
        using var form = new SettingsForm(settings);
        if (form.ShowDialog(owner) != DialogResult.OK)
        {
            return settings;
        }

        UpdateSettings(form.SavedSettings);
        return settings;
    }

    public void HandleSessionSwitch(SessionSwitchEventArgs e)
    {
        if (!enabled)
        {
            return;
        }

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

    public void Dispose()
    {
        StopRuntime();
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

    private void StartRuntime()
    {
        if (timer is null)
        {
            timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (_, _) => Tick();
            timer.Start();
        }

        if (!sessionSwitchSubscribed)
        {
            SystemEvents.SessionSwitch += OnSessionSwitch;
            sessionSwitchSubscribed = true;
        }
    }

    private void StopRuntime()
    {
        if (timer is not null)
        {
            timer.Stop();
            timer.Dispose();
            timer = null;
        }

        if (sessionSwitchSubscribed)
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            sessionSwitchSubscribed = false;
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        HandleSessionSwitch(e);
    }
}
