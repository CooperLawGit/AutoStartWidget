using AutoStartWidget.Core;

namespace AutoStartWidget.App.Screenshot;

internal sealed class ScreenshotItemForm : Form
{
    private readonly PictureBox imageBox;
    private readonly ScreenshotItemActions actions;
    private readonly CtrlWheelZoomFilter wheelZoomFilter;
    private readonly Bitmap bitmap;
    private readonly DateTime createdAt = DateTime.Now;
    private ScreenshotViewState viewState = ScreenshotViewState.Default;
    private bool forceClose;
    private Point dragStart;
    private Point compactClickPoint;
    private Size normalClientSize;
    private bool sentToDustBox;

    public event EventHandler? SentToDustBox;

    public string DisplayName => $"{createdAt:HH:mm:ss}  {bitmap.Width}x{bitmap.Height}";

    public ScreenshotItemForm(CapturedImage image, bool topMost, ScreenshotItemActions actions)
    {
        this.actions = actions;
        bitmap = (Bitmap)image.Bitmap.Clone();
        wheelZoomFilter = new CtrlWheelZoomFilter(this, ZoomByWheel);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = new Rectangle(image.SourceBounds.Location, image.SourceBounds.Size);
        normalClientSize = image.SourceBounds.Size;
        TopMost = topMost;
        ShowInTaskbar = false;
        Icon = AppIcons.Main;
        KeyPreview = true;
        ContextMenuStrip = BuildMenu();
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

        imageBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = bitmap,
            SizeMode = PictureBoxSizeMode.StretchImage,
            ContextMenuStrip = ContextMenuStrip,
            TabStop = true
        };
        imageBox.Paint += DrawCapturedIndicator;
        imageBox.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStart = e.Location;
            }
        };
        imageBox.MouseMove += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                Left += e.X - dragStart.X;
                Top += e.Y - dragStart.Y;
            }
        };
        imageBox.MouseEnter += (_, _) =>
        {
            Activate();
            imageBox.Focus();
        };
        imageBox.MouseWheel += OnImageMouseWheel;
        imageBox.MouseDoubleClick += (_, e) => ToggleCompact(e.Location);
        Controls.Add(imageBox);
        Application.AddMessageFilter(wheelZoomFilter);
        MouseDoubleClick += (_, e) => ToggleCompact(e.Location);
        MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStart = e.Location;
            }
        };
        MouseMove += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && viewState.IsCompact)
            {
                Left += e.X - dragStart.X;
                Top += e.Y - dragStart.Y;
            }
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    public void RestoreFromDustBox()
    {
        sentToDustBox = false;
        Show();
        Activate();
    }

    public void ClosePermanently()
    {
        forceClose = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!forceClose)
        {
            e.Cancel = true;
            Hide();
            if (!sentToDustBox)
            {
                sentToDustBox = true;
                SentToDustBox?.Invoke(this, EventArgs.Empty);
            }
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            imageBox.Image = null;
            Application.RemoveMessageFilter(wheelZoomFilter);
            bitmap.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("关闭", null, (_, _) => Close());
        menu.Items.Add(BuildDustBoxMenu());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("复制", null, (_, _) => CopyToClipboard());
        menu.Items.Add("剪切", null, (_, _) => CutToDustBox());
        menu.Items.Add("保存", null, (_, _) => SaveAs());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("还原缩放", null, (_, _) => SetZoom(100));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildTransformMenu());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("选项", null, (_, _) => actions.OpenOptions(this));
        menu.Opening += (_, _) =>
        {
            menu.Items[0].Enabled = true;
        };
        return menu;
    }

    private ToolStripMenuItem BuildDustBoxMenu()
    {
        var item = new ToolStripMenuItem("回收站");
        item.DropDownOpening += (_, _) =>
        {
            item.DropDownItems.Clear();
            var dustItems = actions.GetDustBoxItems();
            if (dustItems.Count == 0)
            {
                item.DropDownItems.Add("空").Enabled = false;
            }
            else
            {
                foreach (var dustItem in dustItems)
                {
                    item.DropDownItems.Add(dustItem.DisplayName, null, (_, _) => actions.RestoreFromDustBox(dustItem));
                }
            }
            item.DropDownItems.Add(new ToolStripSeparator());
            item.DropDownItems.Add("清空回收站", null, (_, _) => actions.ClearDustBox()).Enabled = dustItems.Count > 0;
        };
        return item;
    }

    private ToolStripMenuItem BuildTransformMenu()
    {
        var item = new ToolStripMenuItem("变换");
        item.DropDownItems.Add("旋转90度", null, (_, _) => Rotate90());
        item.DropDownItems.Add("垂直翻转", null, (_, _) => FlipVertical());
        item.DropDownItems.Add("水平翻转", null, (_, _) => FlipHorizontal());
        return item;
    }

    private void CopyToClipboard()
    {
        Clipboard.SetImage(bitmap);
    }

    private void CutToDustBox()
    {
        CopyToClipboard();
        Close();
    }

    private void SaveAs()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png|JPEG 图片|*.jpg|BMP 图片|*.bmp",
            FileName = $"screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.png"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
        var format = ext switch
        {
            ".jpg" or ".jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
            ".bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
            _ => System.Drawing.Imaging.ImageFormat.Png
        };
        bitmap.Save(dialog.FileName, format);
    }

    private void SetZoom(int percent)
    {
        if (viewState.IsCompact)
        {
            return;
        }

        viewState = viewState.ZoomTo(percent);
        ApplyZoom();
    }

    private void ZoomByWheel(int delta)
    {
        viewState = viewState.ZoomByWheelDelta(delta);
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        if (viewState.IsCompact)
        {
            return;
        }

        var width = Math.Max(1, bitmap.Width * viewState.ZoomPercent / 100);
        var height = Math.Max(1, bitmap.Height * viewState.ZoomPercent / 100);
        ClientSize = new Size(width, height);
        normalClientSize = ClientSize;
    }

    private void Rotate90()
    {
        bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
        viewState = viewState.Rotate90();
        RefreshImageAfterTransform();
    }

    private void FlipVertical()
    {
        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
        viewState = viewState.FlipVertical();
        RefreshImageAfterTransform();
    }

    private void FlipHorizontal()
    {
        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
        viewState = viewState.FlipHorizontal();
        RefreshImageAfterTransform();
    }

    private void RefreshImageAfterTransform()
    {
        imageBox.Image = null;
        imageBox.Image = bitmap;
        if (!viewState.IsCompact)
        {
            normalClientSize = new Size(
                Math.Max(1, bitmap.Width * viewState.ZoomPercent / 100),
                Math.Max(1, bitmap.Height * viewState.ZoomPercent / 100));
        }
        ApplyZoom();
        imageBox.Invalidate();
    }

    private void OnImageMouseWheel(object? sender, MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) != Keys.Control)
        {
            return;
        }

        ZoomByWheel(e.Delta);
    }

    private static void DrawCapturedIndicator(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(Color.White, 2);
        e.Graphics.DrawLine(pen, 0, 0, 48, 0);
        e.Graphics.DrawLine(pen, 0, 0, 0, 48);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!viewState.IsCompact)
        {
            return;
        }

        e.Graphics.Clear(Color.Fuchsia);
        TransparencyKey = Color.Fuchsia;
        e.Graphics.DrawImageUnscaled(bitmap, new Point(-compactClickPoint.X + Width / 2, -compactClickPoint.Y + Height / 2));
        e.Graphics.DrawRectangle(Pens.White, new Rectangle(0, 0, Width - 1, Height - 1));
    }

    private void ToggleCompact(Point clickPoint)
    {
        if (viewState.IsCompact)
        {
            RestoreFromCompact();
            return;
        }

        Compact(clickPoint);
    }

    private void Compact(Point clickPoint)
    {
        compactClickPoint = clickPoint == Point.Empty
            ? new Point(Width / 2, Height / 2)
            : clickPoint;
        normalClientSize = ClientSize;
        Left += compactClickPoint.X - 25;
        Top += compactClickPoint.Y - 25;
        viewState = viewState.ToggleCompact();
        imageBox.Visible = false;
        ClientSize = new Size(50, 50);
        TransparencyKey = Color.Fuchsia;
        Invalidate();
    }

    private void RestoreFromCompact()
    {
        Left += Width / 2 - compactClickPoint.X;
        Top += Height / 2 - compactClickPoint.Y;
        viewState = viewState.ToggleCompact();
        TransparencyKey = Color.Empty;
        ClientSize = normalClientSize;
        imageBox.Visible = true;
        imageBox.Invalidate();
    }

    private sealed class CtrlWheelZoomFilter : IMessageFilter
    {
        private const int WmMouseWheel = 0x020A;
        private readonly Form owner;
        private readonly Action<int> zoomByWheel;

        public CtrlWheelZoomFilter(Form owner, Action<int> zoomByWheel)
        {
            this.owner = owner;
            this.zoomByWheel = zoomByWheel;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WmMouseWheel ||
                !owner.Visible ||
                !owner.Bounds.Contains(Cursor.Position) ||
                (Control.ModifierKeys & Keys.Control) != Keys.Control)
            {
                return false;
            }

            var delta = (short)((m.WParam.ToInt64() >> 16) & 0xffff);
            zoomByWheel(delta);
            return true;
        }
    }
}
