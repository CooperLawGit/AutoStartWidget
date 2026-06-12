using AutoStartWidget.Core;

namespace AutoStartWidget.App;

internal sealed class ProtectionScreenForm : Form
{
    private readonly EyeCareSettings settings;
    private readonly DateTimeOffset breakStartedAt;
    private readonly ProtectionScreenView view;
    private readonly System.Windows.Forms.Timer countdownTimer;
    private Image? backgroundImage;
    private int lastShownSeconds = -1;

    public bool CompletedNaturally { get; private set; }

    public ProtectionScreenForm(EyeCareSettings settings, int earlyCloseCount)
    {
        this.settings = settings;
        breakStartedAt = DateTimeOffset.Now;

        FormBorderStyle = FormBorderStyle.None;
        Icon = AppIcons.Main;
        StartPosition = FormStartPosition.Manual;
        Bounds = settings.ScreenScope == ProtectionScreenScope.AllScreens
            ? SystemInformation.VirtualScreen
            : Screen.PrimaryScreen?.Bounds ?? SystemInformation.VirtualScreen;
        BackColor = Color.Black;
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        DoubleClick += (_, _) => CloseEarly();
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

        LoadBackgroundImage();

        view = new ProtectionScreenView(settings.Tips, BuildHintText(earlyCloseCount), backgroundImage)
        {
            Dock = DockStyle.Fill
        };
        view.DoubleClick += (_, _) => CloseEarly();
        Controls.Add(view);

        countdownTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };
        countdownTimer.Tick += (_, _) => RefreshCountdown();

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                CloseEarly();
            }
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RefreshCountdown();
        countdownTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        countdownTimer.Stop();
        countdownTimer.Dispose();
        backgroundImage?.Dispose();
        base.OnFormClosed(e);
    }

    private void RefreshCountdown()
    {
        var remaining = EyeCareSchedule.GetBreakRemaining(breakStartedAt, DateTimeOffset.Now, settings);
        if (remaining == TimeSpan.Zero)
        {
            CompletedNaturally = true;
            Close();
            return;
        }

        var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
        if (seconds == lastShownSeconds)
        {
            return;
        }

        lastShownSeconds = seconds;
        view.SetCountdown(seconds.ToString("0"));
    }

    private void CloseEarly()
    {
        Close();
    }

    private static string BuildHintText(int earlyCloseCount)
    {
        return earlyCloseCount > 0
            ? $"鼠标双击关闭{Environment.NewLine}上次没有休息满 20 秒，已提前关闭 {earlyCloseCount} 次"
            : "鼠标双击关闭";
    }

    private void LoadBackgroundImage()
    {
        var path = settings.BackgroundMediaPath;
        var kind = BackgroundMediaKindResolver.Resolve(path);
        if (kind == BackgroundMediaKind.None || path is null || !File.Exists(path))
        {
            return;
        }

        try
        {
            backgroundImage = Image.FromFile(path);
        }
        catch
        {
            backgroundImage = null;
        }
    }
}

internal sealed class ProtectionScreenView : Control
{
    private readonly string tips;
    private readonly string hint;
    private readonly Image? backgroundImage;
    private readonly Font countdownFont = new(FontFamily.GenericSansSerif, 104F, FontStyle.Bold);
    private readonly Font tipsFont = new("Microsoft YaHei", 34F, FontStyle.Regular);
    private readonly Font hintFont = new("Microsoft YaHei", 18F, FontStyle.Regular);
    private string countdown = string.Empty;

    public ProtectionScreenView(string tips, string hint, Image? backgroundImage)
    {
        this.tips = tips;
        this.hint = hint;
        this.backgroundImage = backgroundImage;
        BackColor = Color.Black;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);
    }

    public void SetCountdown(string value)
    {
        if (countdown == value)
        {
            return;
        }

        countdown = value;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            countdownFont.Dispose();
            tipsFont.Dispose();
            hintFont.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Color.Black);
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (backgroundImage is not null)
        {
            graphics.DrawImage(backgroundImage, ClientRectangle);
            using var shade = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
            graphics.FillRectangle(shade, ClientRectangle);
        }

        DrawCenteredText(graphics);
        DrawHint(graphics);
    }

    private void DrawCenteredText(Graphics graphics)
    {
        var contentWidth = Math.Max(600, ClientSize.Width - 160);
        var countdownSize = graphics.MeasureString(countdown, countdownFont, contentWidth);
        var tipsSize = graphics.MeasureString(tips, tipsFont, contentWidth);
        var totalHeight = countdownSize.Height + 24 + tipsSize.Height;
        var top = (ClientSize.Height - totalHeight) / 2F;
        var left = (ClientSize.Width - contentWidth) / 2F;
        using var white = new SolidBrush(Color.White);
        using var centerFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString(
            countdown,
            countdownFont,
            white,
            new RectangleF(left, top, contentWidth, countdownSize.Height),
            centerFormat);
        graphics.DrawString(
            tips,
            tipsFont,
            white,
            new RectangleF(left, top + countdownSize.Height + 24, contentWidth, tipsSize.Height),
            centerFormat);
    }

    private void DrawHint(Graphics graphics)
    {
        using var hintBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        using var rightFormat = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Far
        };
        graphics.DrawString(
            hint,
            hintFont,
            hintBrush,
            new RectangleF(28, 22, ClientSize.Width - 56, ClientSize.Height - 44),
            rightFormat);
    }
}
