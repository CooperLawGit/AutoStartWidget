namespace AutoStartWidget.App.Screenshot;

internal sealed class CapturedImage : IDisposable
{
    public CapturedImage(Bitmap bitmap, Rectangle sourceBounds)
    {
        Bitmap = bitmap;
        SourceBounds = sourceBounds;
    }

    public Bitmap Bitmap { get; }
    public Rectangle SourceBounds { get; }

    public void Dispose()
    {
        Bitmap.Dispose();
    }
}
