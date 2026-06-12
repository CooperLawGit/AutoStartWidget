namespace AutoStartWidget.Core;

public sealed class BreakReminderHistory
{
    public int EarlyCloseCount { get; private set; }
    public bool ShouldWarnBeforeEarlyClose => EarlyCloseCount > 0;

    public void RecordBreakCompleted(bool completedNaturally, bool countSkippedBreak = true)
    {
        if (!countSkippedBreak)
        {
            return;
        }

        EarlyCloseCount = completedNaturally ? 0 : EarlyCloseCount + 1;
    }
}
