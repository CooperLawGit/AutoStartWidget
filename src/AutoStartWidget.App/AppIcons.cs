namespace AutoStartWidget.App;

internal static class AppIcons
{
    public static Icon Main { get; } = LoadMainIcon();

    private static Icon LoadMainIcon()
    {
        const string sourceIconPath = @"D:\WorkSpace\GithubProject\icon.png";
        if (File.Exists(sourceIconPath))
        {
            try
            {
                using var bitmap = new Bitmap(sourceIconPath);
                return Icon.FromHandle(bitmap.GetHicon());
            }
            catch
            {
                // Fall back below.
            }
        }

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }
}
