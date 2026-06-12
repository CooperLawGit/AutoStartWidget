using AutoStartWidget.Core;

var tests = new (string Name, Action Run)[]
{
    ("default settings use 20 minutes and 20 seconds", DefaultSettingsUseTwentyTwenty),
    ("settings carry editable tips background and countdown", SettingsCarryEditableValues),
    ("settings default to primary screen and can use all screens", SettingsCarryScreenScope),
    ("media kind resolves only images and gifs", MediaKindResolvesSupportedFiles),
    ("early close reminder counts skipped breaks", EarlyCloseReminderCountsSkippedBreaks),
    ("manual break does not change skipped break count", ManualBreakDoesNotChangeSkippedBreakCount),
    ("lock pauses and unlock resets work timer", LockPausesAndUnlockResetsWorkTimer),
    ("work remaining reaches zero at 20 minutes", WorkRemainingReachesZeroAtTwentyMinutes),
    ("break due only after work duration", BreakDueOnlyAfterWorkDuration),
    ("break remaining counts down then clamps to zero", BreakRemainingCountsDownThenClampsToZero)
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

static void DefaultSettingsUseTwentyTwenty()
{
    AssertEqual(TimeSpan.FromMinutes(20), EyeCareSettings.Default.WorkDuration);
    AssertEqual(TimeSpan.FromSeconds(20), EyeCareSettings.Default.BreakDuration);
    AssertEqual("看看 20 英尺（约 6 米）外，放松眼睛。", EyeCareSettings.Default.Tips);
    AssertEqual(null, EyeCareSettings.Default.BackgroundMediaPath);
    AssertEqual(ProtectionScreenScope.PrimaryScreen, EyeCareSettings.Default.ScreenScope);
}

static void SettingsCarryEditableValues()
{
    var settings = EyeCareSettings.Default with
    {
        BreakDuration = TimeSpan.FromSeconds(45),
        Tips = "看窗外",
        BackgroundMediaPath = @"D:\media\eye.gif"
    };

    AssertEqual(TimeSpan.FromSeconds(45), settings.BreakDuration);
    AssertEqual("看窗外", settings.Tips);
    AssertEqual(@"D:\media\eye.gif", settings.BackgroundMediaPath);
}

static void SettingsCarryScreenScope()
{
    var settings = EyeCareSettings.Default with
    {
        ScreenScope = ProtectionScreenScope.AllScreens
    };

    AssertEqual(ProtectionScreenScope.AllScreens, settings.ScreenScope);
}

static void MediaKindResolvesSupportedFiles()
{
    AssertEqual(BackgroundMediaKind.Image, BackgroundMediaKindResolver.Resolve(@"D:\a\photo.JPG"));
    AssertEqual(BackgroundMediaKind.Gif, BackgroundMediaKindResolver.Resolve(@"D:\a\loop.gif"));
    AssertEqual(BackgroundMediaKind.None, BackgroundMediaKindResolver.Resolve(@"D:\a\clip.mp4"));
    AssertEqual(BackgroundMediaKind.None, BackgroundMediaKindResolver.Resolve(@"D:\a\clip.webm"));
    AssertEqual(BackgroundMediaKind.None, BackgroundMediaKindResolver.Resolve(null));
    AssertEqual(BackgroundMediaKind.None, BackgroundMediaKindResolver.Resolve(@"D:\a\file.txt"));
}

static void EarlyCloseReminderCountsSkippedBreaks()
{
    var history = new BreakReminderHistory();

    AssertFalse(history.ShouldWarnBeforeEarlyClose);
    AssertEqual(0, history.EarlyCloseCount);
    history.RecordBreakCompleted(completedNaturally: false);
    AssertTrue(history.ShouldWarnBeforeEarlyClose);
    AssertEqual(1, history.EarlyCloseCount);
    history.RecordBreakCompleted(completedNaturally: false);
    AssertEqual(2, history.EarlyCloseCount);
    history.RecordBreakCompleted(completedNaturally: true);
    AssertFalse(history.ShouldWarnBeforeEarlyClose);
    AssertEqual(0, history.EarlyCloseCount);
}

static void ManualBreakDoesNotChangeSkippedBreakCount()
{
    var history = new BreakReminderHistory();

    history.RecordBreakCompleted(completedNaturally: false, countSkippedBreak: false);
    AssertEqual(0, history.EarlyCloseCount);
    history.RecordBreakCompleted(completedNaturally: false);
    AssertEqual(1, history.EarlyCloseCount);
    history.RecordBreakCompleted(completedNaturally: false, countSkippedBreak: false);
    AssertEqual(1, history.EarlyCloseCount);
}

static void LockPausesAndUnlockResetsWorkTimer()
{
    var startedAt = DateTimeOffset.UnixEpoch;
    var clock = new WorkSessionClock(startedAt);

    clock.Lock(startedAt.AddMinutes(19));
    AssertFalse(clock.IsBreakDue(startedAt.AddHours(2), EyeCareSettings.Default));
    clock.Unlock(startedAt.AddHours(2));
    AssertFalse(clock.IsBreakDue(startedAt.AddHours(2).AddMinutes(19), EyeCareSettings.Default));
    AssertTrue(clock.IsBreakDue(startedAt.AddHours(2).AddMinutes(20), EyeCareSettings.Default));
}

static void WorkRemainingReachesZeroAtTwentyMinutes()
{
    var startedAt = DateTimeOffset.UnixEpoch;

    var beforeDue = EyeCareSchedule.GetWorkRemaining(
        startedAt,
        startedAt.AddMinutes(19).AddSeconds(59),
        EyeCareSettings.Default);
    var atDue = EyeCareSchedule.GetWorkRemaining(
        startedAt,
        startedAt.AddMinutes(20),
        EyeCareSettings.Default);

    AssertEqual(TimeSpan.FromSeconds(1), beforeDue);
    AssertEqual(TimeSpan.Zero, atDue);
}

static void BreakDueOnlyAfterWorkDuration()
{
    var startedAt = DateTimeOffset.UnixEpoch;

    AssertFalse(EyeCareSchedule.IsBreakDue(
        startedAt,
        startedAt.AddMinutes(19).AddSeconds(59),
        EyeCareSettings.Default));
    AssertTrue(EyeCareSchedule.IsBreakDue(
        startedAt,
        startedAt.AddMinutes(20),
        EyeCareSettings.Default));
}

static void BreakRemainingCountsDownThenClampsToZero()
{
    var startedAt = DateTimeOffset.UnixEpoch;

    AssertEqual(
        TimeSpan.FromSeconds(13),
        EyeCareSchedule.GetBreakRemaining(startedAt, startedAt.AddSeconds(7), EyeCareSettings.Default));
    AssertEqual(
        TimeSpan.Zero,
        EyeCareSchedule.GetBreakRemaining(startedAt, startedAt.AddSeconds(30), EyeCareSettings.Default));
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void AssertTrue(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true.");
    }
}

static void AssertFalse(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false.");
    }
}
