using System.IO;
using System.Windows;

namespace Steps;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var exePath = Environment.ProcessPath ?? "";
        var exeName = Path.GetFileNameWithoutExtension(exePath);
        var dirName = Path.GetFileName(Path.GetDirectoryName(exePath) ?? "");
        var isDemo = e.Args.Contains("/demo") || e.Args.Contains("-demo") || e.Args.Contains("--demo")
                     || exeName.StartsWith("Demo", StringComparison.OrdinalIgnoreCase)
                     || dirName.Equals("Demo", StringComparison.OrdinalIgnoreCase);
        var mainWindow = new MainWindow(isDemo);
        mainWindow.Show();
    }
}

