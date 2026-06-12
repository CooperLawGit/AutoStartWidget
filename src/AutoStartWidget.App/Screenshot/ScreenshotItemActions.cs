namespace AutoStartWidget.App.Screenshot;

internal sealed class ScreenshotItemActions
{
    public required Action RestoreLatestFromDustBox { get; init; }
    public required Action ClearDustBox { get; init; }
    public required Func<IReadOnlyList<ScreenshotItemForm>> GetDustBoxItems { get; init; }
    public required Action<ScreenshotItemForm> RestoreFromDustBox { get; init; }
    public required Action<IWin32Window> OpenOptions { get; init; }
}
