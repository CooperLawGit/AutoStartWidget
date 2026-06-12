using System.Diagnostics;

namespace AutoStartWidget.App;

internal static class ScreenshotToolLauncher
{
    public static void Start(IWin32Window owner)
    {
        var setunaPath = FindSetunaExecutable();
        if (setunaPath is null)
        {
            MessageBox.Show(
                owner,
                "未找到 SETUNA.exe。请先构建 src\\Setuna.Legacy\\SETUNA\\SETUNA.csproj，或把 SETUNA.exe 放到 AutoStartWidget 程序目录的 Setuna 文件夹。",
                "AutoStartWidget",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = setunaPath,
            WorkingDirectory = Path.GetDirectoryName(setunaPath)!,
            UseShellExecute = true
        });
    }

    private static string? FindSetunaExecutable()
    {
        foreach (var candidate in GetCandidatePaths())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        var baseDirectory = AppContext.BaseDirectory;

        yield return Path.Combine(baseDirectory, "Setuna", "SETUNA.exe");
        yield return Path.Combine(baseDirectory, "SETUNA.exe");
        yield return Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Setuna.Legacy",
            "SETUNA",
            "bin",
            "x64",
            "Release",
            "SETUNA.exe"));
        yield return Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Setuna.Legacy",
            "SETUNA",
            "bin",
            "x86",
            "Release",
            "SETUNA.exe"));
        yield return Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Setuna.Legacy",
            "SETUNA",
            "bin",
            "Debug",
            "SETUNA.exe"));
    }
}
