using AutoStartWidget.Core;
using AutoStartWidget.App.Modules;

namespace AutoStartWidget.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon notifyIcon;
    private readonly EyeCareModule eyeCareModule;
    private readonly ScreenshotModule screenshotModule;
    private AutoStartWidgetSettings settings;

    public TrayApplicationContext(AutoStartWidgetSettings settings)
    {
        settings.EyeCare.Validate();
        settings.Screenshot.Validate();
        this.settings = settings;
        eyeCareModule = new EyeCareModule(settings.EyeCare, settings.Modules.EyeCareEnabled);
        screenshotModule = new ScreenshotModule(settings.Screenshot, settings.Modules.ScreenshotEnabled);
        screenshotModule.SettingsChanged += (_, _) => SaveSettings();

        notifyIcon = new NotifyIcon
        {
            Icon = AppIcons.Main,
            Text = "AutoStartWidget",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(BuildScreenshotMenu());
        menu.Items.Add(BuildEyeCareMenu());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildStartupMenuItem());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        return menu;
    }

    private ToolStripMenuItem BuildScreenshotMenu()
    {
        var menu = new ToolStripMenuItem("截图工具") { Checked = screenshotModule.Enabled };
        menu.DropDownItems.Add("启用", null, (_, _) => ToggleScreenshot(menu));
        menu.DropDownItems.Add("截图", null, (_, _) => screenshotModule.CaptureRegion());
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add("恢复最近关闭", null, (_, _) => screenshotModule.RestoreLatest());
        menu.DropDownItems.Add("清空废纸篓", null, (_, _) => screenshotModule.ClearDustBox());
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add("设置", null, (_, _) => OpenScreenshotSettings());
        menu.DropDownOpening += (_, _) =>
        {
            menu.Checked = screenshotModule.Enabled;
            menu.DropDownItems[0].Text = screenshotModule.Enabled ? "禁用" : "启用";
            menu.DropDownItems[1].Enabled = screenshotModule.Enabled;
            menu.DropDownItems[3].Enabled = screenshotModule.Enabled && screenshotModule.DustCount > 0;
            menu.DropDownItems[4].Enabled = screenshotModule.Enabled && screenshotModule.DustCount > 0;
        };
        return menu;
    }

    private ToolStripMenuItem BuildEyeCareMenu()
    {
        var menu = new ToolStripMenuItem("护眼助手") { Checked = eyeCareModule.Enabled };
        menu.DropDownItems.Add("启用", null, (_, _) => ToggleEyeCare(menu));
        menu.DropDownItems.Add("立即护眼", null, (_, _) => eyeCareModule.ShowNow());
        menu.DropDownItems.Add("设置", null, (_, _) => OpenEyeCareSettings());
        menu.DropDownOpening += (_, _) =>
        {
            menu.Checked = eyeCareModule.Enabled;
            menu.DropDownItems[0].Text = eyeCareModule.Enabled ? "禁用" : "启用";
            menu.DropDownItems[1].Enabled = eyeCareModule.Enabled;
        };
        return menu;
    }

    private static ToolStripMenuItem BuildStartupMenuItem()
    {
        var item = new ToolStripMenuItem("开机自启")
        {
            Checked = AutoStartupManager.IsEnabled()
        };

        item.Click += (_, _) =>
        {
            var enabled = !AutoStartupManager.IsEnabled();
            if (!AutoStartupManager.SetEnabled(enabled))
            {
                MessageBox.Show(
                    "无法修改开机自启设置。",
                    "AutoStartWidget",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            item.Checked = enabled;
        };

        return item;
    }

    private void ToggleScreenshot(ToolStripMenuItem menu)
    {
        screenshotModule.SetEnabled(!screenshotModule.Enabled);
        menu.Checked = screenshotModule.Enabled;
        SaveSettings();
    }

    private void ToggleEyeCare(ToolStripMenuItem menu)
    {
        eyeCareModule.SetEnabled(!eyeCareModule.Enabled);
        menu.Checked = eyeCareModule.Enabled;
        SaveSettings();
    }

    private void OpenScreenshotSettings()
    {
        screenshotModule.OpenSettings(null!);
        SaveSettings();
    }

    private void OpenEyeCareSettings()
    {
        eyeCareModule.UpdateSettings(eyeCareModule.OpenSettings(null!));
        SaveSettings();
    }

    private void SaveSettings()
    {
        settings = settings with
        {
            Modules = new ModuleSettings(screenshotModule.Enabled, eyeCareModule.Enabled),
            EyeCare = eyeCareModule.Settings,
            Screenshot = screenshotModule.Settings
        };
        AppSettingsStore.Save(settings);
    }

    protected override void ExitThreadCore()
    {
        eyeCareModule.Dispose();
        screenshotModule.Dispose();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
