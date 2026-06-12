namespace AutoStartWidget.App.Screenshot;

internal static class ScreenCapture
{
    public static Bitmap Capture(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        return bitmap;
    }

    public static Bitmap Crop(Bitmap source, Rectangle sourceBounds, Rectangle cropBounds)
    {
        var local = new Rectangle(
            cropBounds.X - sourceBounds.X,
            cropBounds.Y - sourceBounds.Y,
            cropBounds.Width,
            cropBounds.Height);
        return source.Clone(local, source.PixelFormat);
    }
}
