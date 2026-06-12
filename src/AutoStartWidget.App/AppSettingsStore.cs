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

    public static EyeCareSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return EyeCareSettings.Default;
            }

            var data = JsonSerializer.Deserialize<StoredSettings>(
                File.ReadAllText(SettingsPath),
                JsonOptions);

            var settings = data?.ToEyeCareSettings() ?? EyeCareSettings.Default;
            settings.Validate();
            return settings;
        }
        catch
        {
            return EyeCareSettings.Default;
        }
    }

    public static void Save(EyeCareSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(
            SettingsPath,
            JsonSerializer.Serialize(StoredSettings.From(settings), JsonOptions));
    }

    private sealed class StoredSettings
    {
        public double WorkMinutes { get; set; } = 20;
        public double BreakSeconds { get; set; } = 20;
        public string Tips { get; set; } = EyeCareSettings.Default.Tips;
        public string? BackgroundMediaPath { get; set; }
        public ProtectionScreenScope ScreenScope { get; set; } = ProtectionScreenScope.PrimaryScreen;

        public static StoredSettings From(EyeCareSettings settings)
        {
            return new StoredSettings
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
}
