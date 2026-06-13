using AutoStartWidget.Core;
using System.Drawing;

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
    ("break remaining counts down then clamps to zero", BreakRemainingCountsDownThenClampsToZero),
    ("default app settings enable screenshot and eye care modules", DefaultAppSettingsEnableModules),
    ("screenshot settings carry hotkey topmost and save directory", ScreenshotSettingsCarryEditableValues),
    ("default screenshot hotkey data matches Setuna Ctrl D1", DefaultScreenshotHotKeyMatchesSetuna),
    ("screenshot dust box restores latest closed item first", ScreenshotDustBoxRestoresLatestClosedItemFirst),
    ("screenshot dust box evicts oldest item when capacity is exceeded", ScreenshotDustBoxEvictsOldestItem),
    ("screenshot dust box clear removes all restorable items", ScreenshotDustBoxClearRemovesAllItems),
    ("screenshot selection snaps nearby edges to window bounds", ScreenshotSelectionSnapsNearbyEdges),
    ("screenshot selection snaps to nearby window from candidates", ScreenshotSelectionSnapsToNearbyWindowCandidates),
    ("screenshot selection leaves distant edges unchanged", ScreenshotSelectionLeavesDistantEdges),
    ("screenshot view zoom wheel changes by ten percent per notch", ScreenshotViewZoomWheelChangesByTenPercent),
    ("screenshot view transform tracks rotate and flips", ScreenshotViewTransformTracksRotateAndFlips),
    ("screenshot view compact toggles on double click", ScreenshotViewCompactToggles)
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

static void DefaultAppSettingsEnableModules()
{
    var settings = AutoStartWidgetSettings.Default;

    AssertTrue(settings.Modules.ScreenshotEnabled);
    AssertTrue(settings.Modules.EyeCareEnabled);
    AssertEqual(131121, settings.Screenshot.HotKeyData);
    AssertTrue(settings.Screenshot.HotKeyEnabled);
    AssertTrue(settings.Screenshot.TopMost);
}

static void ScreenshotSettingsCarryEditableValues()
{
    var settings = ScreenshotSettings.Default with
    {
        HotKeyData = 131122,
        HotKeyEnabled = false,
        TopMost = false,
        SaveDirectory = @"D:\shots"
    };

    AssertEqual(131122, settings.HotKeyData);
    AssertFalse(settings.HotKeyEnabled);
    AssertFalse(settings.TopMost);
    AssertEqual(@"D:\shots", settings.SaveDirectory);
}

static void DefaultScreenshotHotKeyMatchesSetuna()
{
    AssertEqual(131121, ScreenshotSettings.Default.HotKeyData);
}

static void ScreenshotDustBoxRestoresLatestClosedItemFirst()
{
    var dustBox = new ScreenshotDustBox<string>(capacity: 3);

    dustBox.Add("first");
    dustBox.Add("second");

    AssertEqual(2, dustBox.Count);
    AssertEqual("second", dustBox.RestoreLatest());
    AssertEqual("first", dustBox.RestoreLatest());
    AssertEqual(0, dustBox.Count);
}

static void ScreenshotDustBoxEvictsOldestItem()
{
    var dustBox = new ScreenshotDustBox<string>(capacity: 2);

    dustBox.Add("first");
    dustBox.Add("second");
    dustBox.Add("third");

    AssertEqual(2, dustBox.Count);
    AssertEqual("third", dustBox.RestoreLatest());
    AssertEqual("second", dustBox.RestoreLatest());
}

static void ScreenshotDustBoxClearRemovesAllItems()
{
    var dustBox = new ScreenshotDustBox<string>(capacity: 3);

    dustBox.Add("first");
    dustBox.Add("second");
    dustBox.Clear();

    AssertEqual(0, dustBox.Count);
    AssertEqual(null, dustBox.RestoreLatest());
}

static void ScreenshotSelectionSnapsNearbyEdges()
{
    var selection = Rectangle.FromLTRB(104, 96, 498, 405);
    var window = Rectangle.FromLTRB(100, 100, 500, 400);

    var snapped = ScreenshotSelectionSnapper.SnapToWindow(selection, window, threshold: 8);

    AssertEqual(window, snapped);
}

static void ScreenshotSelectionSnapsToNearbyWindowCandidates()
{
    var selection = Rectangle.FromLTRB(104, 96, 498, 405);
    var windows = new[]
    {
        Rectangle.FromLTRB(700, 100, 900, 400),
        Rectangle.FromLTRB(100, 100, 500, 400)
    };

    var snapped = ScreenshotSelectionSnapper.SnapToNearestWindow(selection, windows, threshold: 8);

    AssertEqual(Rectangle.FromLTRB(100, 100, 500, 400), snapped);
}

static void ScreenshotSelectionLeavesDistantEdges()
{
    var selection = Rectangle.FromLTRB(80, 120, 540, 380);
    var window = Rectangle.FromLTRB(100, 100, 500, 400);

    var snapped = ScreenshotSelectionSnapper.SnapToWindow(selection, window, threshold: 8);

    AssertEqual(selection, snapped);
}

static void ScreenshotViewZoomWheelChangesByTenPercent()
{
    var state = ScreenshotViewState.Default;

    state = state.ZoomByWheelDelta(120);
    AssertEqual(110, state.ZoomPercent);

    state = state.ZoomByWheelDelta(-240);
    AssertEqual(90, state.ZoomPercent);

    state = state.ZoomTo(150);
    AssertEqual(150, state.ZoomPercent);
}

static void ScreenshotViewTransformTracksRotateAndFlips()
{
    var state = ScreenshotViewState.Default.Rotate90().Rotate90().FlipHorizontal().FlipVertical();

    AssertEqual(180, state.RotationDegrees);
    AssertTrue(state.FlippedHorizontal);
    AssertTrue(state.FlippedVertical);
}

static void ScreenshotViewCompactToggles()
{
    var state = ScreenshotViewState.Default.ToggleCompact();

    AssertTrue(state.IsCompact);
    AssertFalse(state.ToggleCompact().IsCompact);
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
