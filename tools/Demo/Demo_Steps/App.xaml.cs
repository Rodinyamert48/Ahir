using System.Windows;

namespace Demo_Steps;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var mainWindow = new Steps.MainWindow(isDemo: true);
        mainWindow.Title = "Ahır Setup — DEMO MODE";
        mainWindow.Show();
    }
}