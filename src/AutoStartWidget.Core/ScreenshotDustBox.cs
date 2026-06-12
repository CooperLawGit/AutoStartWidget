namespace AutoStartWidget.Core;

public sealed class ScreenshotDustBox<T>
    where T : class
{
    private readonly int capacity;
    private readonly LinkedList<T> items = new();

    public ScreenshotDustBox(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        this.capacity = capacity;
    }

    public int Count => items.Count;

    public void Add(T item)
    {
        items.AddLast(item);
        while (items.Count > capacity)
        {
            items.RemoveFirst();
        }
    }

    public T? RestoreLatest()
    {
        if (items.Last is null)
        {
            return null;
        }

        var item = items.Last.Value;
        items.RemoveLast();
        return item;
    }

    public void Clear()
    {
        items.Clear();
    }
}
