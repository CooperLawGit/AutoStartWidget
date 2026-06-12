using AutoStartWidget.Core;

namespace AutoStartWidget.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext(AppSettingsStore.Load()));
    }
}
