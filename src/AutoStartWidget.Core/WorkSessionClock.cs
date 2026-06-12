namespace AutoStartWidget.Core;

public sealed class WorkSessionClock
{
    private DateTimeOffset workStartedAt;
    private bool locked;

    public WorkSessionClock(DateTimeOffset startedAt)
    {
        workStartedAt = startedAt;
    }

    public void Reset(DateTimeOffset now)
    {
        workStartedAt = now;
        locked = false;
    }

    public void Lock(DateTimeOffset now)
    {
        locked = true;
    }

    public void Unlock(DateTimeOffset now)
    {
        Reset(now);
    }

    public bool IsBreakDue(DateTimeOffset now, EyeCareSettings settings)
    {
        return !locked && EyeCareSchedule.IsBreakDue(workStartedAt, now, settings);
    }
}
