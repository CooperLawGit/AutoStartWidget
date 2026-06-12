namespace AutoStartWidget.App;

internal static class AppIcons
{
    public static Icon Main { get; } =
        Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
}
