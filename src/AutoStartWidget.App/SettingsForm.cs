using AutoStartWidget.Core;

namespace AutoStartWidget.App;

internal sealed class SettingsForm : Form
{
    private readonly NumericUpDown countdownSeconds;
    private readonly TextBox tipsTextBox;
    private readonly TextBox mediaPathTextBox;
    private readonly RadioButton primaryScreenRadio;
    private readonly RadioButton allScreensRadio;
    private readonly TimeSpan workDuration;

    public EyeCareSettings SavedSettings { get; private set; }

    public SettingsForm(EyeCareSettings current)
    {
        current.Validate();
        SavedSettings = current;
        workDuration = current.WorkDuration;

        Text = "AutoStartWidget 设置";
        Icon = AppIcons.Main;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 350);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "护眼屏设置",
            Font = new Font(FontFamily.GenericSansSerif, 14F, FontStyle.Bold),
            Location = new Point(18, 18)
        };

        var countdownLabel = new Label
        {
            AutoSize = true,
            Text = "倒计时秒数",
            Location = new Point(20, 68)
        };
        countdownSeconds = new NumericUpDown
        {
            Location = new Point(120, 64),
            Width = 100,
            Minimum = 5,
            Maximum = 3600,
            Value = (decimal)Math.Clamp(current.BreakDuration.TotalSeconds, 5, 3600)
        };

        var tipsLabel = new Label
        {
            AutoSize = true,
            Text = "Tips",
            Location = new Point(20, 108)
        };
        tipsTextBox = new TextBox
        {
            Location = new Point(120, 104),
            Size = new Size(410, 72),
            Multiline = true,
            Text = current.Tips
        };

        var mediaLabel = new Label
        {
            AutoSize = true,
            Text = "背景图片",
            Location = new Point(20, 200)
        };
        mediaPathTextBox = new TextBox
        {
            Location = new Point(120, 196),
            Size = new Size(300, 24),
            Text = current.BackgroundMediaPath ?? string.Empty
        };
        var browseButton = new Button
        {
            Text = "浏览",
            Location = new Point(430, 195),
            Size = new Size(54, 27)
        };
        browseButton.Click += (_, _) => BrowseMedia();

        var clearButton = new Button
        {
            Text = "清空",
            Location = new Point(490, 195),
            Size = new Size(54, 27)
        };
        clearButton.Click += (_, _) => mediaPathTextBox.Clear();

        var scopeLabel = new Label
        {
            AutoSize = true,
            Text = "显示屏幕",
            Location = new Point(20, 238)
        };
        primaryScreenRadio = new RadioButton
        {
            AutoSize = true,
            Text = "仅主屏幕",
            Location = new Point(120, 236),
            Checked = current.ScreenScope == ProtectionScreenScope.PrimaryScreen
        };
        allScreensRadio = new RadioButton
        {
            AutoSize = true,
            Text = "所有屏幕",
            Location = new Point(220, 236),
            Checked = current.ScreenScope == ProtectionScreenScope.AllScreens
        };

        var saveButton = new Button
        {
            Text = "保存",
            Location = new Point(370, 300),
            Size = new Size(78, 30),
            DialogResult = DialogResult.OK
        };
        saveButton.Click += (_, e) =>
        {
            if (!TrySave())
            {
                DialogResult = DialogResult.None;
            }
        };

        var cancelButton = new Button
        {
            Text = "取消",
            Location = new Point(460, 300),
            Size = new Size(78, 30),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[]
        {
            titleLabel,
            countdownLabel,
            countdownSeconds,
            tipsLabel,
            tipsTextBox,
            mediaLabel,
            mediaPathTextBox,
            browseButton,
            clearButton,
            scopeLabel,
            primaryScreenRadio,
            allScreensRadio,
            saveButton,
            cancelButton
        });

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void BrowseMedia()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择背景图片/GIF",
            Filter = "图片/GIF|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            mediaPathTextBox.Text = dialog.FileName;
        }
    }

    private bool TrySave()
    {
        var mediaPath = string.IsNullOrWhiteSpace(mediaPathTextBox.Text)
            ? null
            : mediaPathTextBox.Text.Trim();

        if (mediaPath is not null)
        {
            if (!File.Exists(mediaPath))
            {
                MessageBox.Show(this, "背景媒体文件不存在。", "AutoStartWidget", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (BackgroundMediaKindResolver.Resolve(mediaPath) == BackgroundMediaKind.None)
            {
                MessageBox.Show(this, "只支持 jpg、png、bmp、gif。", "AutoStartWidget", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        SavedSettings = new EyeCareSettings(
            workDuration,
            TimeSpan.FromSeconds((double)countdownSeconds.Value),
            tipsTextBox.Text.Trim(),
            mediaPath,
            allScreensRadio.Checked ? ProtectionScreenScope.AllScreens : ProtectionScreenScope.PrimaryScreen);
        return true;
    }
}
