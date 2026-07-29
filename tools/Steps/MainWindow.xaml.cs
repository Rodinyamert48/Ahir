using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Steps.Services;

namespace Steps;

public partial class MainWindow : Window
{
    private int _currentStep;
    private readonly SolidColorBrush _activeColor = new(Color.FromRgb(0x7c, 0x6f, 0xf0));
    private readonly SolidColorBrush _inactiveColor = new(Color.FromRgb(0x55, 0x55, 0x77));
    private readonly SolidColorBrush _completedColor = new(Color.FromRgb(0x4e, 0xca, 0xaf));
    private readonly SolidColorBrush _passColor = new(Color.FromRgb(0x4e, 0xca, 0xaf));
    private readonly SolidColorBrush _failColor = new(Color.FromRgb(0xf0, 0x6e, 0x6e));

    private readonly InstallationService _installer = null!;

    private string _installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ahir");
    private string _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ahir");
    private string _logsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ahir", "logs");
    private string _backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ahir", "backups");

    private int _httpPort = 8080;
    private int _wsPort = 9090;
    private string _adminUser = "admin";
    private string _adminPass = "";
    private string _adminPassConfirm = "";
    private bool _enableCompression = true;
    private bool _enableRateLimiting = true;
    private bool _autoStart = true;

    private bool _isDemoMode;
    private SystemCheckResult? _checkResult;
    private StackPanel? _checkPanel;
    private Button? _retryButton;
    private TextBox? _logBox;
    private ProgressBar? _installProgressBar;
    private TextBlock? _installStatusText;

    public MainWindow(bool isDemo = false)
    {
        InitializeComponent();
        _installer = new InstallationService(
            _installPath, _dataPath, _logsPath, _backupPath,
            _httpPort, _wsPort, _adminUser, _adminPass,
            _enableCompression, _enableRateLimiting, _autoStart);
        _isDemoMode = isDemo;
        if (_isDemoMode)
            Title = "Ahır Setup Wizard — DEMO MODE";
        ShowStep(0);
    }

    public bool IsDemoMode => _isDemoMode;

    private void ShowStep(int step)
    {
        _currentStep = step;
        UpdateStepIndicators();
        PageContent.Content = step switch
        {
            0 => CreateWelcomePage(),
            1 => CreateSystemCheckPage(),
            2 => CreateLocationPage(),
            3 => CreateServerConfigPage(),
            4 => CreateSecurityPage(),
            5 => CreateSummaryPage(),
            6 => CreateInstallProgressPage(),
            7 => CreateCompletePage(),
            _ => CreateWelcomePage()
        };
        BackButton.IsEnabled = step is > 0 and < 6;
        NextButton.Content = step >= 6 ? "Finish" : step == 5 ? "Install" : "Next";
        NextButton.IsEnabled = step != 1 || (_checkResult?.AllPassed == true);
    }

    private void UpdateStepIndicators()
    {
        for (int i = 0; i < 8; i++)
        {
            var tb = FindName($"Step{i + 1}") as TextBlock;
            if (tb == null) continue;
            tb.Foreground = i == _currentStep ? _activeColor :
                            i < _currentStep ? _completedColor : _inactiveColor;
            tb.FontWeight = i == _currentStep ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private TextBlock MakeTitle(string text) => new()
    {
        Text = text, FontSize = 20, FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(0xee, 0xee, 0xff))
    };

    private TextBlock MakeText(string text, double size = 13, byte r = 0xaa, byte g = 0xaa, byte b = 0xcc) => new()
    {
        Text = text, FontSize = size,
        Foreground = new SolidColorBrush(Color.FromRgb(r, g, b)),
        TextWrapping = TextWrapping.Wrap
    };

    // ===== PAGE 0: Welcome =====
    private FrameworkElement CreateWelcomePage()
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        s.Children.Add(new TextBlock { Text = "Ahır", FontSize = 36, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x7c, 0x6f, 0xf0)) });
        s.Children.Add(MakeText("Next-Generation Backend Platform", 16, 0xcc, 0xcc, 0xee));
        s.Children.Add(MakeText("", 8));
        s.Children.Add(MakeText("This wizard installs Ahır — a production-grade backend platform with embedded database engine, HTTP server, WebSocket, auth, file storage, and plugin system.\n\nClick Next to begin.", 13, 0x99, 0x99, 0xbb));
        return s;
    }

    // ===== PAGE 1: System Check =====
    private FrameworkElement CreateSystemCheckPage()
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 0, 0) };
        s.Children.Add(MakeTitle("System Requirements Check"));

        _checkPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        s.Children.Add(_checkPanel);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        _retryButton = new Button
        {
            Content = "⟳ Retry Check", Height = 32, Width = 120,
            Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x5e)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xee)),
            BorderThickness = new Thickness(0), Cursor = Cursors.Hand, FontSize = 13
        };
        _retryButton.Click += (_, _) => RunSystemCheck();
        btnRow.Children.Add(_retryButton);
        s.Children.Add(btnRow);

        RunSystemCheck();
        return s;
    }

    private void RunSystemCheck()
    {
        _checkResult = _installer.CheckSystem();
        _checkPanel!.Children.Clear();

        AddCheckRow(_checkPanel, "Administrator privileges", _checkResult.AdminRight);
        AddCheckRow(_checkPanel, ".NET 9 Runtime", _checkResult.DotNetRuntime);
        AddCheckRow(_checkPanel, $"Disk space (>500 MB on {Path.GetPathRoot(_installPath)})", _checkResult.DiskSpace);
        AddCheckRow(_checkPanel, "Windows 10+", _checkResult.OsSupported);

        if (_checkResult.AllPassed)
        {
            _checkPanel.Children.Add(MakeText("", 8));
            _checkPanel.Children.Add(new TextBlock { Text = "✓ All checks passed — ready to install", FontSize = 14, Foreground = _passColor, FontWeight = FontWeights.SemiBold });
        }
        else
        {
            _checkPanel.Children.Add(MakeText("", 8));
            _checkPanel.Children.Add(new TextBlock {
                Text = "⚠ Some checks failed, but you can proceed. Fix issues on next pages.",
                FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xf0, 0xcc, 0x4e)),
                FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap
            });
        }
    }

    private void AddCheckRow(StackPanel parent, string label, bool passed)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        row.Children.Add(new TextBlock { Text = passed ? "✓" : "✗", FontSize = 16, Foreground = passed ? _passColor : _failColor, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
        row.Children.Add(new TextBlock { Text = label, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xcc)), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
        parent.Children.Add(row);
    }

    // ===== PAGE 2: Location =====
    private FrameworkElement CreateLocationPage()
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 0, 0) };
        s.Children.Add(MakeTitle("Installation Location"));
        s.Children.Add(PathField("Program Files Path:", ref _installPath));
        s.Children.Add(PathField("Data Path:", ref _dataPath));
        s.Children.Add(PathField("Logs Path:", ref _logsPath));
        s.Children.Add(PathField("Backup Path:", ref _backupPath));
        return s;
    }

    private FrameworkElement PathField(string label, ref string? path)
    {
        var row = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        row.Children.Add(MakeText(label, 13, 0xaa, 0xaa, 0xcc));
        var box = new TextBox { Text = path ?? "", FontSize = 13, Height = 30, Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x3e)), Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xee)), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x5e)), Padding = new Thickness(8, 0, 8, 0) };
        row.Children.Add(box);
        return row;
    }

    // ===== PAGE 3: Server Config =====
    private FrameworkElement CreateServerConfigPage()
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 0, 0) };
        s.Children.Add(MakeTitle("Server Configuration"));

        s.Children.Add(NumberField("HTTP API Port:", _httpPort, v => _httpPort = v));
        s.Children.Add(NumberField("WebSocket Port:", _wsPort, v => _wsPort = v));
        s.Children.Add(CheckField("Enable LZ4 Compression", true, v => _enableCompression = v));
        s.Children.Add(CheckField("Enable Rate Limiting", true, v => _enableRateLimiting = v));
        s.Children.Add(CheckField("Register Windows Service (auto-start)", true, v => _autoStart = v));
        return s;
    }

    private FrameworkElement NumberField(string label, int value, Action<int> onChanged)
    {
        var row = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        row.Children.Add(MakeText(label, 13, 0xaa, 0xaa, 0xcc));
        var box = new TextBox { Text = value.ToString(), FontSize = 13, Height = 30, Width = 120, HorizontalAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x3e)), Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xee)), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x5e)), Padding = new Thickness(8, 0, 8, 0) };
        box.TextChanged += (_, _) => { if (int.TryParse(box.Text, out var v)) onChanged(v); };
        row.Children.Add(box);
        return row;
    }

    private FrameworkElement CheckField(string label, bool defaultValue, Action<bool> onChanged)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        var cb = new CheckBox { IsChecked = defaultValue, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(0x7c, 0x6f, 0xf0)) };
        cb.Checked += (_, _) => onChanged(true);
        cb.Unchecked += (_, _) => onChanged(false);
        row.Children.Add(cb);
        row.Children.Add(new TextBlock { Text = label, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xcc)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) });
        return row;
    }

    // ===== PAGE 4: Security =====
    private FrameworkElement CreateSecurityPage()
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 0, 0) };
        s.Children.Add(MakeTitle("Security Configuration"));

        s.Children.Add(TextField("Admin Username:", _adminUser, v => _adminUser = v));
        s.Children.Add(PasswordField("Admin Password:", v => _adminPass = v));
        s.Children.Add(PasswordField("Confirm Password:", v => _adminPassConfirm = v));
        return s;
    }

    private FrameworkElement TextField(string label, string value, Action<string> onChanged)
    {
        var row = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        row.Children.Add(MakeText(label, 13, 0xaa, 0xaa, 0xcc));
        var box = new TextBox { Text = value, FontSize = 13, Height = 30, Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x3e)), Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xee)), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x5e)), Padding = new Thickness(8, 0, 8, 0) };
        box.TextChanged += (_, _) => onChanged(box.Text);
        row.Children.Add(box);
        return row;
    }

    private FrameworkElement PasswordField(string label, Action<string> onChanged)
    {
        var row = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        row.Children.Add(MakeText(label, 13, 0xaa, 0xaa, 0xcc));
        var box = new PasswordBox { FontSize = 13, Height = 30, Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x3e)), Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xee)), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x5e)), Padding = new Thickness(8, 0, 8, 0) };
        box.PasswordChanged += (_, _) => onChanged(box.Password);
        row.Children.Add(box);
        return row;
    }

    // ===== PAGE 5: Summary =====
    private FrameworkElement CreateSummaryPage()
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 0, 0) };
        s.Children.Add(MakeTitle(_isDemoMode ? "Installation Summary (DEMO)" : "Installation Summary"));

        var items = new[]
        {
            $"Install Path:     {_installPath}",
            $"Data Path:        {_dataPath}",
            $"HTTP Port:        {_httpPort}",
            $"WebSocket Port:   {_wsPort}",
            $"Admin User:       {_adminUser}",
            $"Compression:      {(_enableCompression ? "Yes" : "No")}",
            $"Rate Limiting:    {(_enableRateLimiting ? "Yes" : "No")}",
            $"Windows Service:  {(_autoStart ? "Yes" : "No")}",
            $"System Check:     {(_checkResult?.AllPassed == true ? "Passed ✓" : "Failed ✗")}"
        };

        foreach (var item in items)
            s.Children.Add(MakeText(item, 13, 0xaa, 0xaa, 0xcc));

        s.Children.Add(MakeText("", 8));
        if (_adminPass != _adminPassConfirm)
        {
            s.Children.Add(new TextBlock { Text = "⚠ Passwords do not match!", FontSize = 13, Foreground = _failColor, FontWeight = FontWeights.SemiBold });
            NextButton.IsEnabled = false;
        }
        else if (string.IsNullOrEmpty(_adminPass) || _adminPass.Length < 4)
        {
            s.Children.Add(new TextBlock { Text = "⚠ Password too short (min 4 chars)", FontSize = 13, Foreground = _failColor, FontWeight = FontWeights.SemiBold });
            NextButton.IsEnabled = false;
        }
        else
        {
            s.Children.Add(MakeText("Click 'Install' to begin the installation.", 13, 0x4e, 0xca, 0xaf));
            NextButton.IsEnabled = true;
        }
        return s;
    }

    // ===== PAGE 6: Install Progress =====
    private FrameworkElement CreateInstallProgressPage()
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 0, 0) };
        s.Children.Add(MakeTitle(_isDemoMode ? "Installing Ahır... (DEMO)" : "Installing Ahır..."));

        _installProgressBar = new ProgressBar
        {
            Height = 8, Margin = new Thickness(0, 16, 0, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(0x7c, 0x6f, 0xf0)),
            Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x3e)),
            BorderThickness = new Thickness(0), Minimum = 0, Maximum = 100
        };
        s.Children.Add(_installProgressBar);

        _installStatusText = MakeText("Preparing...", 13, 0xaa, 0xaa, 0xcc);
        _installStatusText.Margin = new Thickness(0, 12, 0, 0);
        s.Children.Add(_installStatusText);

        _logBox = new TextBox
        {
            IsReadOnly = true, FontSize = 11, Height = 200,
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x22)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0xcc, 0x88)),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x3e)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 12, 0, 0)
        };
        s.Children.Add(_logBox);

        NextButton.IsEnabled = false;
        StartInstallation();
        return s;
    }

    private async void StartInstallation()
    {
        if (_isDemoMode)
        {
            // Demo mode: simulate progress without actually installing
            var demoSteps = new (int, string)[]
            {
                (5,  "Checking administrator privileges... ✓"),
                (10, "Creating installation directories... ✓"),
                (20, "Copying Ahır binaries (15 files)... ✓"),
                (35, "Generating configuration file (ahir.json)... ✓"),
                (50, "Generating JWT secret & encryption key... ✓"),
                (60, "Creating Windows Firewall rules... ✓"),
                (75, "Registering Windows Service (AhirServer)... ✓"),
                (85, "Configuring auto-start... ✓"),
                (95, "Creating admin user with Argon2id hash... ✓"),
                (100,"Installation completed successfully!")
            };

            foreach (var (percent, message) in demoSteps)
            {
                await Task.Delay(300);
                Dispatcher.Invoke(() =>
                {
                    _installProgressBar!.Value = percent;
                    _installStatusText!.Text = _isDemoMode ? message.Replace(" ✓", "") : message;
                    _logBox!.AppendText($"[DEMO] [{percent}%] {message}\n");
                    _logBox.ScrollToEnd();
                });
            }

            Dispatcher.Invoke(() =>
            {
                _logBox!.AppendText("\n[DEMO] This was a simulation. No changes were made to your system.\n");
                _installStatusText!.Text = "Demo complete! Click Finish.";
                _installStatusText.Foreground = _passColor;
                ShowStep(7);
            });
            return;
        }

        var progress = new Progress<InstallProgress>(p =>
        {
            Dispatcher.Invoke(() =>
            {
                _installProgressBar!.Value = p.Percent;
                _installStatusText!.Text = p.Message;
                _logBox!.AppendText($"[{p.Percent}%] {p.Message}\n");
                _logBox.ScrollToEnd();
            });
        });

        var success = await _installer.RunAsync(progress);

        Dispatcher.Invoke(() =>
        {
            if (success)
            {
                _installStatusText!.Text = "Installation completed successfully!";
                _installStatusText.Foreground = _passColor;
                ShowStep(7);
            }
            else
            {
                _installStatusText!.Text = "Installation failed. Check logs for details.";
                _installStatusText.Foreground = _failColor;
                NextButton.Content = "Close";
                NextButton.IsEnabled = true;
            }
        });
    }

    // ===== PAGE 7: Complete =====
    private FrameworkElement CreateCompletePage()
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        s.Children.Add(new TextBlock { Text = "✓", FontSize = 56, Foreground = _passColor, HorizontalAlignment = HorizontalAlignment.Center });
        s.Children.Add(new TextBlock { Text = _isDemoMode ? "Demo Complete!" : "Installation Complete!", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0xee, 0xee, 0xff)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 0) });
        if (_isDemoMode)
        {
            s.Children.Add(MakeText("This was a simulation. No files were created, no system changes were made.", 14, 0x99, 0x99, 0xbb));
            s.Children.Add(MakeText("The real installer performs the same steps with actual system operations.", 14, 0x99, 0x99, 0xbb));
            s.Children.Add(MakeText("Run Steps.exe without /demo to perform a real installation.", 13, 0x77, 0x77, 0x99));
        }
        else
        {
            s.Children.Add(MakeText($"Ahır has been installed to:\n{_installPath}\n\nUse 'ahir' CLI to manage the server.", 14, 0x99, 0x99, 0xbb));
        }
        NextButton.Content = "Finish";
        return s;
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep >= 6 && _installer.Log.Any(l => l.Contains("FATAL"))) { Close(); return; }
        if (_currentStep >= 7) { Close(); return; }

        if (_currentStep == 5)
        {
            // Recreate installer with latest settings before install
            var fieldInfo = typeof(InstallationService).GetField("_installPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // Installer is recreated in constructor, but we need to update it
            ShowStep(6);
            return;
        }

        ShowStep(_currentStep + 1);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 0) ShowStep(_currentStep - 1);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
}