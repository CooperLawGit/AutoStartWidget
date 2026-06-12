namespace AutoStartWidget.Core;

public sealed record ScreenshotViewState(
    int ZoomPercent,
    int RotationDegrees,
    bool FlippedHorizontal,
    bool FlippedVertical,
    bool IsCompact)
{
    public static ScreenshotViewState Default { get; } = new(
        ZoomPercent: 100,
        RotationDegrees: 0,
        FlippedHorizontal: false,
        FlippedVertical: false,
        IsCompact: false);

    public ScreenshotViewState ZoomTo(int percent)
    {
        return this with { ZoomPercent = Math.Clamp(percent, 10, 500) };
    }

    public ScreenshotViewState ZoomByWheelDelta(int delta)
    {
        return ZoomTo(ZoomPercent + Math.Sign(delta) * Math.Max(1, Math.Abs(delta) / 120) * 10);
    }

    public ScreenshotViewState Rotate90()
    {
        return this with { RotationDegrees = (RotationDegrees + 90) % 360 };
    }

    public ScreenshotViewState FlipHorizontal()
    {
        return this with { FlippedHorizontal = !FlippedHorizontal };
    }

    public ScreenshotViewState FlipVertical()
    {
        return this with { FlippedVertical = !FlippedVertical };
    }

    public ScreenshotViewState ToggleCompact()
    {
        return this with { IsCompact = !IsCompact };
    }
}
