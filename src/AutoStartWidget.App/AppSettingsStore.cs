using System.Text.Json;
using AutoStartWidget.Core;

namespace AutoStartWidget.App;

internal static class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoStartWidget",
        "settings.json");

    public static AutoStartWidgetSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return AutoStartWidgetSettings.Default;
            }

            var data = JsonSerializer.Deserialize<StoredSettings>(
                File.ReadAllText(SettingsPath),
                JsonOptions);

            var settings = data?.ToSettings() ?? AutoStartWidgetSettings.Default;
            settings.EyeCare.Validate();
            settings.Screenshot.Validate();
            return settings;
        }
        catch
        {
            return AutoStartWidgetSettings.Default;
        }
    }

    public static void Save(AutoStartWidgetSettings settings)
    {
        settings.EyeCare.Validate();
        settings.Screenshot.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(
            SettingsPath,
            JsonSerializer.Serialize(StoredSettings.From(settings), JsonOptions));
    }

    private sealed class StoredSettings
    {
        public StoredModuleSettings? Modules { get; set; }
        public StoredEyeCareSettings? EyeCare { get; set; }
        public StoredScreenshotSettings? Screenshot { get; set; }

        // Legacy flat eye-care settings from the first baseline.
        public double WorkMinutes { get; set; } = 20;
        public double BreakSeconds { get; set; } = 20;
        public string Tips { get; set; } = EyeCareSettings.Default.Tips;
        public string? BackgroundMediaPath { get; set; }
        public ProtectionScreenScope ScreenScope { get; set; } = ProtectionScreenScope.PrimaryScreen;

        public static StoredSettings From(AutoStartWidgetSettings settings)
        {
            return new StoredSettings
            {
                Modules = StoredModuleSettings.From(settings.Modules),
                EyeCare = StoredEyeCareSettings.From(settings.EyeCare),
                Screenshot = StoredScreenshotSettings.From(settings.Screenshot)
            };
        }

        public AutoStartWidgetSettings ToSettings()
        {
            return new AutoStartWidgetSettings(
                Modules?.ToModuleSettings() ?? ModuleSettings.Default,
                EyeCare?.ToEyeCareSettings() ?? ToLegacyEyeCareSettings(),
                Screenshot?.ToScreenshotSettings() ?? ScreenshotSettings.Default);
        }

        private EyeCareSettings ToLegacyEyeCareSettings()
        {
            return new EyeCareSettings(
                TimeSpan.FromMinutes(WorkMinutes),
                TimeSpan.FromSeconds(BreakSeconds),
                Tips,
                BackgroundMediaPath,
                ScreenScope);
        }
    }

    private sealed class StoredModuleSettings
    {
        public bool ScreenshotEnabled { get; set; } = true;
        public bool EyeCareEnabled { get; set; } = true;

        public static StoredModuleSettings From(ModuleSettings settings)
        {
            return new StoredModuleSettings
            {
                ScreenshotEnabled = settings.ScreenshotEnabled,
                EyeCareEnabled = settings.EyeCareEnabled
            };
        }

        public ModuleSettings ToModuleSettings()
        {
            return new ModuleSettings(ScreenshotEnabled, EyeCareEnabled);
        }
    }

    private sealed class StoredEyeCareSettings
    {
        public double WorkMinutes { get; set; } = 20;
        public double BreakSeconds { get; set; } = 20;
        public string Tips { get; set; } = EyeCareSettings.Default.Tips;
        public string? BackgroundMediaPath { get; set; }
        public ProtectionScreenScope ScreenScope { get; set; } = ProtectionScreenScope.PrimaryScreen;

        public static StoredEyeCareSettings From(EyeCareSettings settings)
        {
            return new StoredEyeCareSettings
            {
                WorkMinutes = settings.WorkDuration.TotalMinutes,
                BreakSeconds = settings.BreakDuration.TotalSeconds,
                Tips = settings.Tips,
                BackgroundMediaPath = settings.BackgroundMediaPath,
                ScreenScope = settings.ScreenScope
            };
        }

        public EyeCareSettings ToEyeCareSettings()
        {
            return new EyeCareSettings(
                TimeSpan.FromMinutes(WorkMinutes),
                TimeSpan.FromSeconds(BreakSeconds),
                Tips,
                BackgroundMediaPath,
                ScreenScope);
        }
    }

    private sealed class StoredScreenshotSettings
    {
        public int HotKeyData { get; set; } = ScreenshotSettings.Default.HotKeyData;
        public bool HotKeyEnabled { get; set; } = true;
        public bool TopMost { get; set; } = true;
        public string? SaveDirectory { get; set; }

        public static StoredScreenshotSettings From(ScreenshotSettings settings)
        {
            return new StoredScreenshotSettings
            {
                HotKeyData = settings.HotKeyData,
                HotKeyEnabled = settings.HotKeyEnabled,
                TopMost = settings.TopMost,
                SaveDirectory = settings.SaveDirectory
            };
        }

        public ScreenshotSettings ToScreenshotSettings()
        {
            return new ScreenshotSettings(HotKeyData, HotKeyEnabled, TopMost, SaveDirectory);
        }
    }
}
