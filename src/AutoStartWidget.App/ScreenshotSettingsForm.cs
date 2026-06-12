using AutoStartWidget.Core;
using AutoStartWidget.App.HotKeys;

namespace AutoStartWidget.App;

internal sealed class ScreenshotSettingsForm : Form
{
    private readonly CheckBox hotKeyEnabled;
    private readonly HotkeyControl hotKeyControl;
    private readonly CheckBox topMost;
    private readonly TextBox saveDirectoryTextBox;

    public ScreenshotSettings SavedSettings { get; private set; }

    public ScreenshotSettingsForm(ScreenshotSettings current)
    {
        current.Validate();
        SavedSettings = current;

        Text = "截图工具设置";
        Icon = AppIcons.Main;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 250);

        hotKeyEnabled = new CheckBox
        {
            Text = "启用截图热键",
            Location = new Point(20, 24),
            AutoSize = true,
            Checked = current.HotKeyEnabled
        };

        var hotKeyLabel = new Label
        {
            Text = "热键",
            Location = new Point(20, 65),
            AutoSize = true
        };
        hotKeyControl = new HotkeyControl
        {
            Location = new Point(120, 62),
            Size = new Size(220, 24),
            Hotkey = (Keys)current.HotKeyData
        };

        topMost = new CheckBox
        {
            Text = "截图贴片默认置顶",
            Location = new Point(20, 104),
            AutoSize = true,
            Checked = current.TopMost
        };

        var saveLabel = new Label
        {
            Text = "保存目录",
            Location = new Point(20, 146),
            AutoSize = true
        };
        saveDirectoryTextBox = new TextBox
        {
            Location = new Point(120, 142),
            Size = new Size(280, 24),
            Text = current.SaveDirectory ?? string.Empty
        };
        var browseButton = new Button
        {
            Text = "浏览",
            Location = new Point(410, 141),
            Size = new Size(70, 27)
        };
        browseButton.Click += (_, _) => BrowseSaveDirectory();

        var saveButton = new Button
        {
            Text = "保存",
            Location = new Point(320, 198),
            Size = new Size(78, 30),
            DialogResult = DialogResult.OK
        };
        saveButton.Click += (_, _) =>
        {
            if (!TrySave())
            {
                DialogResult = DialogResult.None;
            }
        };

        var cancelButton = new Button
        {
            Text = "取消",
            Location = new Point(410, 198),
            Size = new Size(78, 30),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[]
        {
            hotKeyEnabled,
            hotKeyLabel,
            hotKeyControl,
            topMost,
            saveLabel,
            saveDirectoryTextBox,
            browseButton,
            saveButton,
            cancelButton
        });

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void BrowseSaveDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择截图保存目录",
            SelectedPath = Directory.Exists(saveDirectoryTextBox.Text) ? saveDirectoryTextBox.Text : string.Empty
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            saveDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private bool TrySave()
    {
        var hotKey = hotKeyControl.Hotkey;
        var saveDirectory = string.IsNullOrWhiteSpace(saveDirectoryTextBox.Text)
            ? null
            : saveDirectoryTextBox.Text.Trim();

        if (saveDirectory is not null && !Directory.Exists(saveDirectory))
        {
            MessageBox.Show(this, "保存目录不存在。", "AutoStartWidget", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var settings = new ScreenshotSettings((int)hotKey, hotKeyEnabled.Checked, topMost.Checked, saveDirectory);
        try
        {
            settings.Validate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "AutoStartWidget", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        SavedSettings = settings;
        return true;
    }

}
