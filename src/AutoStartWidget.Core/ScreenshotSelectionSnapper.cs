using System.Drawing;

namespace AutoStartWidget.Core;

public static class ScreenshotSelectionSnapper
{
    public static Rectangle SnapToNearestWindow(Rectangle selection, IEnumerable<Rectangle> windowBounds, int threshold)
    {
        var best = selection;
        var bestMatches = 0;
        var bestDistance = int.MaxValue;

        foreach (var window in windowBounds)
        {
            var snapped = SnapToWindow(selection, window, threshold);
            if (snapped == selection)
            {
                continue;
            }

            var matches = CountMatchedEdges(selection, snapped);
            var distance = GetSnapDistance(selection, snapped);
            if (matches > bestMatches || (matches == bestMatches && distance < bestDistance))
            {
                best = snapped;
                bestMatches = matches;
                bestDistance = distance;
            }
        }

        return best;
    }

    public static Rectangle SnapToWindow(Rectangle selection, Rectangle windowBounds, int threshold)
    {
        var snapped = Rectangle.FromLTRB(
            Snap(selection.Left, windowBounds.Left, threshold),
            Snap(selection.Top, windowBounds.Top, threshold),
            Snap(selection.Right, windowBounds.Right, threshold),
            Snap(selection.Bottom, windowBounds.Bottom, threshold));

        return snapped.Width > 0 && snapped.Height > 0 ? snapped : selection;
    }

    private static int Snap(int value, int target, int threshold)
    {
        return Math.Abs(value - target) <= threshold ? target : value;
    }

    private static int GetSnapDistance(Rectangle source, Rectangle snapped)
    {
        return Math.Abs(source.Left - snapped.Left)
            + Math.Abs(source.Top - snapped.Top)
            + Math.Abs(source.Right - snapped.Right)
            + Math.Abs(source.Bottom - snapped.Bottom);
    }

    private static int CountMatchedEdges(Rectangle source, Rectangle snapped)
    {
        var count = 0;
        count += source.Left == snapped.Left ? 0 : 1;
        count += source.Top == snapped.Top ? 0 : 1;
        count += source.Right == snapped.Right ? 0 : 1;
        count += source.Bottom == snapped.Bottom ? 0 : 1;
        return count;
    }
}
