using AutoStartWidget.Core;

namespace AutoStartWidget.App.Screenshot;

internal sealed class CaptureOverlayForm : Form
{
    private const int WindowSnapThreshold = 12;
    private readonly Rectangle virtualBounds;
    private readonly Bitmap screenImage;
    private readonly IReadOnlyList<Rectangle> windowBounds;
    private Point dragStart;
    private Point dragCurrent;
    private bool dragging;

    public CapturedImage? Result { get; private set; }

    public CaptureOverlayForm()
    {
        virtualBounds = SystemInformation.VirtualScreen;
        windowBounds = WindowTargetFinder.FindVisibleWindowBounds(virtualBounds);
        screenImage = ScreenCapture.Capture(virtualBounds);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = virtualBounds;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        KeyPreview = true;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0.98;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        dragging = true;
        var screenPoint = PointToScreen(e.Location);
        dragStart = screenPoint;
        dragCurrent = dragStart;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var screenPoint = PointToScreen(e.Location);
        if (dragging)
        {
            dragCurrent = screenPoint;
            Invalidate();
            return;
        }

        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!dragging || e.Button != MouseButtons.Left)
        {
            return;
        }

        dragging = false;
        dragCurrent = PointToScreen(e.Location);
        var selection = GetSelection();
        if (selection.Width < 2 || selection.Height < 2)
        {
            Invalidate();
            return;
        }

        CompleteCapture(selection);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImage(screenImage, ClientRectangle);
        using var shade = new SolidBrush(Color.FromArgb(90, Color.Black));
        e.Graphics.FillRectangle(shade, ClientRectangle);

        Rectangle? target = dragging ? GetSelection() : null;
        if (target is not { } bounds)
        {
            DrawHint(e.Graphics);
            return;
        }

        var local = ToLocal(bounds);
        e.Graphics.DrawImage(screenImage, local, ToLocal(bounds), GraphicsUnit.Pixel);
        using var pen = new Pen(Color.FromArgb(255, 38, 160, 255), 2);
        e.Graphics.DrawRectangle(pen, local);
        DrawSize(e.Graphics, local, bounds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            screenImage.Dispose();
        }

        base.Dispose(disposing);
    }

    private void CompleteCapture(Rectangle bounds)
    {
        Result = new CapturedImage(ScreenCapture.Crop(screenImage, virtualBounds, bounds), bounds);
        DialogResult = DialogResult.OK;
        Close();
    }

    private Rectangle ToLocal(Rectangle screenBounds)
    {
        return new Rectangle(
            screenBounds.X - virtualBounds.X,
            screenBounds.Y - virtualBounds.Y,
            screenBounds.Width,
            screenBounds.Height);
    }

    private static Rectangle Normalize(Point start, Point end)
    {
        return Rectangle.FromLTRB(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Max(start.X, end.X),
            Math.Max(start.Y, end.Y));
    }

    private static void DrawHint(Graphics graphics)
    {
        using var font = new Font("Microsoft YaHei", 14F, FontStyle.Regular);
        using var brush = new SolidBrush(Color.White);
        graphics.DrawString("左键拖拽选择区域；靠近窗口边界自动吸附；Esc 取消", font, brush, 24, 24);
    }

    private Rectangle GetSelection()
    {
        var selection = Normalize(dragStart, dragCurrent);
        return ScreenshotSelectionSnapper.SnapToNearestWindow(selection, windowBounds, WindowSnapThreshold);
    }

    private static void DrawSize(Graphics graphics, Rectangle local, Rectangle screenBounds)
    {
        using var font = new Font("Microsoft YaHei", 10F, FontStyle.Regular);
        using var back = new SolidBrush(Color.FromArgb(180, Color.Black));
        using var text = new SolidBrush(Color.White);
        var label = $"{screenBounds.Width} x {screenBounds.Height}";
        var size = graphics.MeasureString(label, font);
        var rect = new RectangleF(local.Left, Math.Max(0, local.Top - size.Height - 6), size.Width + 10, size.Height + 4);
        graphics.FillRectangle(back, rect);
        graphics.DrawString(label, font, text, rect.Left + 5, rect.Top + 2);
    }
}
