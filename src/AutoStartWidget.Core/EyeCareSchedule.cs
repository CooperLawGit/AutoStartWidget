namespace AutoStartWidget.Core;

public sealed record EyeCareSettings(
    TimeSpan WorkDuration,
    TimeSpan BreakDuration,
    string Tips = "看看 20 英尺（约 6 米）外，放松眼睛。",
    string? BackgroundMediaPath = null,
    ProtectionScreenScope ScreenScope = ProtectionScreenScope.PrimaryScreen)
{
    public static EyeCareSettings Default { get; } = new(
        WorkDuration: TimeSpan.FromMinutes(20),
        BreakDuration: TimeSpan.FromSeconds(20));

    public void Validate()
    {
        if (WorkDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(WorkDuration), "Work duration must be positive.");
        }

        if (BreakDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(BreakDuration), "Break duration must be positive.");
        }

        if (Tips is null)
        {
            throw new ArgumentNullException(nameof(Tips));
        }
    }
}

public enum ProtectionScreenScope
{
    PrimaryScreen,
    AllScreens
}

public enum BackgroundMediaKind
{
    None,
    Image,
    Gif
}

public static class BackgroundMediaKindResolver
{
    public static BackgroundMediaKind Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BackgroundMediaKind.None;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".bmp" => BackgroundMediaKind.Image,
            ".gif" => BackgroundMediaKind.Gif,
            _ => BackgroundMediaKind.None
        };
    }
}

public static class EyeCareSchedule
{
    public static TimeSpan GetWorkRemaining(
        DateTimeOffset workStartedAt,
        DateTimeOffset now,
        EyeCareSettings settings)
    {
        settings.Validate();
        return ClampPositive(settings.WorkDuration - (now - workStartedAt));
    }

    public static bool IsBreakDue(
        DateTimeOffset workStartedAt,
        DateTimeOffset now,
        EyeCareSettings settings)
    {
        return GetWorkRemaining(workStartedAt, now, settings) == TimeSpan.Zero;
    }

    public static TimeSpan GetBreakRemaining(
        DateTimeOffset breakStartedAt,
        DateTimeOffset now,
        EyeCareSettings settings)
    {
        settings.Validate();
        return ClampPositive(settings.BreakDuration - (now - breakStartedAt));
    }

    private static TimeSpan ClampPositive(TimeSpan value)
    {
        return value > TimeSpan.Zero ? value : TimeSpan.Zero;
    }
}
