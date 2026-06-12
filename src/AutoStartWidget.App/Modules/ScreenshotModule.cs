using AutoStartWidget.Core;
using AutoStartWidget.App.HotKeys;
using AutoStartWidget.App.Screenshot;

namespace AutoStartWidget.App.Modules;

internal sealed class ScreenshotModule : IDisposable
{
    private const int DustBoxCapacity = 20;
    private ScreenshotSettings settings;
    private bool enabled;
    private HotKeyWindow? hotKeyWindow;
    private readonly List<ScreenshotItemForm> activeItems = new();
    private readonly LinkedList<ScreenshotItemForm> dustBox = new();

    public event EventHandler? SettingsChanged;

    public ScreenshotModule(ScreenshotSettings settings, bool enabled)
    {
        this.settings = settings;
        this.enabled = enabled;
        ApplyHotKeyState();
    }

    public bool Enabled => enabled;
    public ScreenshotSettings Settings => settings;
    public int ActiveCount => activeItems.Count;
    public int DustCount => dustBox.Count;

    public void SetEnabled(bool value)
    {
        if (enabled == value)
        {
            return;
        }

        enabled = value;
        ApplyHotKeyState();
        if (!enabled)
        {
            CloseAll();
            ClearDustBox();
        }
    }

    public void CaptureRegion()
    {
        Capture(CaptureMode.Region);
    }

    public void CaptureWindow()
    {
        Capture(CaptureMode.Window);
    }

    public void RestoreLatest()
    {
        if (!enabled || dustBox.Last is null)
        {
            return;
        }

        var item = dustBox.Last.Value;
        dustBox.RemoveLast();
        activeItems.Add(item);
        item.RestoreFromDustBox();
    }

    public void RestoreFromDustBox(ScreenshotItemForm item)
    {
        if (!enabled || !dustBox.Contains(item))
        {
            return;
        }

        dustBox.Remove(item);
        activeItems.Add(item);
        item.RestoreFromDustBox();
    }

    public void ClearDustBox()
    {
        foreach (var item in dustBox)
        {
            item.ClosePermanently();
        }

        dustBox.Clear();
    }

    public ScreenshotSettings OpenSettings(IWin32Window owner)
    {
        using var form = new ScreenshotSettingsForm(settings);
        if (form.ShowDialog(owner) != DialogResult.OK)
        {
            return settings;
        }

        settings = form.SavedSettings;
        ApplyHotKeyState();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        return settings;
    }

    public IReadOnlyList<ScreenshotItemForm> GetDustBoxItems()
    {
        return dustBox.Reverse().ToArray();
    }

    public void Dispose()
    {
        hotKeyWindow?.Dispose();
        hotKeyWindow = null;
        CloseAll();
        ClearDustBox();
    }

    private void Capture(CaptureMode mode)
    {
        if (!enabled)
        {
            return;
        }

        using var overlay = new CaptureOverlayForm(mode);
        if (overlay.ShowDialog() != DialogResult.OK || overlay.Result is null)
        {
            return;
        }

        using var captured = overlay.Result;
        AddItem(captured);
    }

    private void AddItem(CapturedImage captured)
    {
        var item = new ScreenshotItemForm(captured, settings.TopMost, BuildItemActions());
        item.SentToDustBox += OnItemSentToDustBox;
        activeItems.Add(item);
        item.Show();
    }

    private ScreenshotItemActions BuildItemActions()
    {
        return new ScreenshotItemActions
        {
            RestoreLatestFromDustBox = RestoreLatest,
            ClearDustBox = ClearDustBox,
            GetDustBoxItems = GetDustBoxItems,
            RestoreFromDustBox = RestoreFromDustBox,
            OpenOptions = owner => OpenSettings(owner)
        };
    }

    private void OnItemSentToDustBox(object? sender, EventArgs e)
    {
        if (sender is not ScreenshotItemForm item)
        {
            return;
        }

        activeItems.Remove(item);
        dustBox.AddLast(item);
        while (dustBox.Count > DustBoxCapacity)
        {
            var evicted = dustBox.First!.Value;
            dustBox.RemoveFirst();
            evicted.ClosePermanently();
        }
    }

    private void CloseAll()
    {
        foreach (var item in activeItems.ToArray())
        {
            item.ClosePermanently();
        }

        activeItems.Clear();
    }

    private void ApplyHotKeyState()
    {
        if (enabled && settings.HotKeyEnabled)
        {
            hotKeyWindow ??= CreateHotKeyWindow();
            hotKeyWindow.Register((Keys)settings.HotKeyData, settings.HotKeyEnabled);
            return;
        }

        hotKeyWindow?.Dispose();
        hotKeyWindow = null;
    }

    private HotKeyWindow CreateHotKeyWindow()
    {
        var window = new HotKeyWindow();
        window.Pressed += (_, _) => CaptureRegion();
        return window;
    }
}
