namespace AutoStartWidget.Core;

public sealed record AutoStartWidgetSettings(
    ModuleSettings Modules,
    EyeCareSettings EyeCare,
    ScreenshotSettings Screenshot)
{
    public static AutoStartWidgetSettings Default { get; } = new(
        ModuleSettings.Default,
        EyeCareSettings.Default,
        ScreenshotSettings.Default);
}

public sealed record ModuleSettings(
    bool ScreenshotEnabled,
    bool EyeCareEnabled)
{
    public static ModuleSettings Default { get; } = new(
        ScreenshotEnabled: true,
        EyeCareEnabled: true);
}

public sealed record ScreenshotSettings(
    int HotKeyData,
    bool HotKeyEnabled,
    bool TopMost,
    string? SaveDirectory)
{
    public static ScreenshotSettings Default { get; } = new(
        HotKeyData: 131121,
        HotKeyEnabled: true,
        TopMost: true,
        SaveDirectory: null);

    public void Validate()
    {
        if (HotKeyData < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(HotKeyData), "Hotkey data must not be negative.");
        }
    }
}
