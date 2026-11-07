using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using NetPulse.I18n;
using NetPulse.Models;
using NetPulse.Services;
using NetPulse.ViewModels;

namespace NetPulse;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private TrayService? _trayService;

    #region Win32 API for Borderless Sizing and Maximizing
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_SIZE = 0xF000;
    private const int WM_GETMINMAXINFO = 0x0024;
    private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MONITORINFO
    {
        public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        public RECT rcMonitor = new();
        public RECT rcWork = new();
        public int dwFlags = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
    #endregion

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        // Initialize Windows System Tray Service
        _trayService = new TrayService(
            onOpenWindow: () =>
            {
                Dispatcher.Invoke(() =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                });
            },
            onStartTest: () => Dispatcher.Invoke(() => _vm.StartTestCommand.Execute(null)),
            onTogglePulse: () => Dispatcher.Invoke(() => _vm.TogglePulseCommand.Execute(null)),
            onExitApp: () => Dispatcher.Invoke(() =>
            {
                _trayService?.Dispose();
                System.Windows.Application.Current.Shutdown();
            })
        );
        _vm.SetTrayService(_trayService);

        _vm.PropertyChanged += OnViewModelPropertyChanged;

        CardsContainer.DataContext = _vm;
        GradeBannerControl.DataContext = _vm;
        HelpOverlay.DataContext = _vm;
        HistoryOverlay.DataContext = _vm;
        SettingsOverlay.DataContext = _vm;
        BreakdownOverlay.DataContext = _vm;

        StateChanged += OnWindowStateChanged;
        SourceInitialized += OnSourceInitialized;

        ApplyUiScale(_vm.UiScale);
        UpdateUiFromViewModel();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Hook WndProc to correctly constrain borderless maximization to working area (excluding taskbar)
        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(HwndHook);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            WmGetMinMaxInfo(hwnd, lParam);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        try
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                var rcWork = monitorInfo.rcWork;
                var rcMonitor = monitorInfo.rcMonitor;

                mmi.ptMaxPosition.x = Math.Abs(rcWork.left - rcMonitor.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWork.top - rcMonitor.top);
                mmi.ptMaxSize.x = Math.Abs(rcWork.right - rcWork.left);
                mmi.ptMaxSize.y = Math.Abs(rcWork.bottom - rcWork.top);

                // Minimum track size (DPI scaled)
                double dpiX = 1.0, dpiY = 1.0;
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }
                mmi.ptMinTrackSize.x = (int)(MinWidth * dpiX);
                mmi.ptMinTrackSize.y = (int)(MinHeight * dpiY);
            }
            Marshal.StructureToPtr(mmi, lParam, false);
        }
        catch
        {
            // Ignore any Win32 monitor querying errors during rapid resize
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.PropertyName == nameof(_vm.UiScale))
            {
                ApplyUiScale(_vm.UiScale);
            }
            UpdateUiFromViewModel();
        });
    }

    private void ApplyUiScale(double scale)
    {
        if (scale <= 0.1) scale = 1.0;
        if (UiScaleTransform != null)
        {
            UiScaleTransform.ScaleX = scale;
            UiScaleTransform.ScaleY = scale;
        }
        CardsContainer?.UpdateColumns();
    }

    private void UpdateUiFromViewModel()
    {
        bool isEn = _vm.IsEn;

        // 1. Status subtext
        if (_vm.IsEditMode)
        {
            StatusSubtext.Text = isEn ? "Edit Mode" : "Режим настройки";
            StatusSubtext.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
        }
        else if (_vm.NetworkInfo.IsConnected)
        {
            StatusSubtext.Text = isEn ? "Online" : "Сеть активна";
            StatusSubtext.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        }
        else
        {
            StatusSubtext.Text = isEn ? "Offline" : "Офлайн";
            StatusSubtext.Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0x3F, 0xF4));
        }

        // 2. Edit Mode vs Normal Mode Visibility
        if (_vm.IsEditMode)
        {
            EditModeToggleBtn.Background = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            EditModeToggleBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            EditModeIcon.Text = "✓";
            EditModeIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x09, 0x0D, 0x16));

            WidgetsPanel.Visibility = Visibility.Collapsed;
            GradeBannerControl.Visibility = Visibility.Collapsed;
            EditModeBanner.Visibility = Visibility.Visible;
            NormalModeDock.Visibility = Visibility.Collapsed;
            EditModeDock.Visibility = Visibility.Visible;

            EditModeBannerTitle.Text = isEn ? "Customize screen metrics" : "Настройка карточек на экране";
            EditModeBannerDesc.Text = isEn
                ? "Click ✕ to remove metric. Use arrows to reorder. You can restore hidden metrics below."
                : "Нажмите ✕ чтобы убрать показатель. Используйте стрелки для перемещения. Снизу можно вернуть скрытые показатели.";
            AddMetricsHeader.Text = isEn ? "ADD METRICS" : "ДОБАВИТЬ ПОКАЗАТЕЛИ";
            DoneEditingBtn.Content = isEn ? "Done" : "Готово";

            RebuildHiddenChips(isEn);
        }
        else
        {
            EditModeToggleBtn.Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x27, 0x3D));
            EditModeToggleBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x32, 0x4D));
            EditModeIcon.Text = "✏";
            EditModeIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));

            WidgetsPanel.Visibility = Visibility.Visible;
            EditModeBanner.Visibility = Visibility.Collapsed;
            GradeBannerControl.Visibility = _vm.QualityGrade != null ? Visibility.Visible : Visibility.Collapsed;
            NormalModeDock.Visibility = Visibility.Visible;
            EditModeDock.Visibility = Visibility.Collapsed;
        }

        // 3. Waveform radar
        WaveformControl.Samples = _vm.PingStats.HistorySamples;
        WaveformControl.CurrentPingMs = _vm.PingStats.CurrentPingMs;
        WaveformControl.AvgLatencyMs = _vm.PingStats.AvgLatencyMs;
        WaveformControl.IsLive = _vm.IsRealtimeMonitoring && !_vm.IsDiagnosticRunning;
        WaveformControl.IsEn = isEn;

        // 4. Speed Gauge
        GaugeControl.CurrentMbps = _vm.SpeedState.CurrentMbps;
        GaugeControl.PeakMbps = _vm.SpeedState.PeakMbps;
        GaugeControl.Phase = _vm.SpeedState.Phase;
        GaugeControl.Progress = _vm.SpeedState.ProgressPercent;
        GaugeControl.IsEn = isEn;

        // 5. Quality Grade Banner
        GradeBannerControl.Grade = _vm.QualityGrade;
        if (!_vm.IsEditMode)
        {
            GradeBannerControl.Visibility = _vm.QualityGrade != null ? Visibility.Visible : Visibility.Collapsed;
        }

        // 6. Phase Status Text
        PhaseStatusTextBlock.Text = AppI18n.GetPhaseStatusText(_vm.SpeedState.Phase, isEn);

        // 7. Action Buttons
        if (_vm.IsDiagnosticRunning)
        {
            MainActionButton.Background = new SolidColorBrush(Color.FromRgb(0xF4, 0x3F, 0x5E));
            MainActionButton.Foreground = Brushes.White;
            MainActionIcon.Text = "■ ";
            MainActionText.Text = isEn ? "Stop test" : "Остановить тест";
        }
        else
        {
            MainActionButton.Background = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            MainActionButton.Foreground = new SolidColorBrush(Color.FromRgb(0x03, 0x1A, 0x26));
            MainActionIcon.Text = "▶ ";
            MainActionText.Text = isEn ? "Start speed test" : "Запустить тест скорости";
        }

        PulseActionText.Text = _vm.IsRealtimeMonitoring
            ? (isEn ? "Live pulse: Active (Click to pause)" : "Живой пульс: Активен (Нажмите для паузы)")
            : (isEn ? "Live pulse: Resume" : "Живой пульс: Возобновить");

        // 8. Dialog Overlays
        HelpOverlay.Visibility = _vm.ShowHelpDialog ? Visibility.Visible : Visibility.Collapsed;
        if (_vm.ShowHelpDialog) HelpOverlay.UpdateContent();

        HistoryOverlay.Visibility = _vm.ShowHistoryDialog ? Visibility.Visible : Visibility.Collapsed;
        if (_vm.ShowHistoryDialog) HistoryOverlay.UpdateContent();

        SettingsOverlay.Visibility = _vm.ShowSettingsDialog ? Visibility.Visible : Visibility.Collapsed;
        if (_vm.ShowSettingsDialog) SettingsOverlay.UpdateContent();

        BreakdownOverlay.Visibility = _vm.ShowTestBreakdownDialog ? Visibility.Visible : Visibility.Collapsed;
        if (_vm.ShowTestBreakdownDialog) BreakdownOverlay.UpdateContent();
    }

    private void RebuildHiddenChips(bool isEn)
    {
        HiddenChipsPanel.Children.Clear();
        var hidden = _vm.Modules.Where(m => !m.IsVisible).ToList();

        if (hidden.Count == 0)
        {
            var emptyLabel = new TextBlock
            {
                Text = isEn ? "All metrics are added to the screen" : "Все показатели уже добавлены на экран",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                Margin = new Thickness(0, 4, 0, 4)
            };
            HiddenChipsPanel.Children.Add(emptyLabel);
            return;
        }

        foreach (var item in hidden)
        {
            var chip = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x27, 0x3D)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text = "+ ",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF))
            });
            stack.Children.Add(new TextBlock
            {
                Text = AppI18n.GetMetricTitle(item.Id, isEn).Replace("\n", " "),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9))
            });
            chip.Child = stack;

            var modId = item.Id;
            chip.MouseLeftButtonUp += (_, _) =>
            {
                _vm.AddModule(modId);
                UpdateUiFromViewModel();
            };

            HiddenChipsPanel.Children.Add(chip);
        }
    }

    #region Window Control & Border Resizing
    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        bool isMax = WindowState == WindowState.Maximized;

        if (MaximizeBtn != null)
        {
            MaximizeBtn.Content = isMax ? "❐" : "□";
            MaximizeBtn.ToolTip = isMax ? "Восстановить" : "Развернуть";
        }

        if (ResizeGrid != null)
        {
            ResizeGrid.Visibility = isMax ? Visibility.Collapsed : Visibility.Visible;
        }

        if (WindowBorder != null)
        {
            WindowBorder.CornerRadius = isMax ? new CornerRadius(0) : new CornerRadius(16);
            WindowBorder.BorderThickness = isMax ? new Thickness(0) : new Thickness(1);
        }
    }

    private void OnResizeMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || WindowState == WindowState.Maximized) return;

        try
        {
            int direction = (sender as FrameworkElement)?.Tag switch
            {
                "Left"        => 1, // WMSZ_LEFT
                "Right"       => 2, // WMSZ_RIGHT
                "Top"         => 3, // WMSZ_TOP
                "TopLeft"     => 4, // WMSZ_TOPLEFT
                "TopRight"    => 5, // WMSZ_TOPRIGHT
                "Bottom"      => 6, // WMSZ_BOTTOM
                "BottomLeft"  => 7, // WMSZ_BOTTOMLEFT
                "BottomRight" => 8, // WMSZ_BOTTOMRIGHT
                _             => 0
            };

            if (direction > 0)
            {
                var helper = new WindowInteropHelper(this);
                ReleaseCapture();
                SendMessage(helper.Handle, WM_SYSCOMMAND, (IntPtr)(SC_SIZE + direction), IntPtr.Zero);
            }
        }
        catch
        {
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Settings.NotificationEnabled)
        {
            Hide();
            _trayService?.ShowNotification(
                "NetPulse",
                _vm.IsEn ? "NetPulse is running in system tray." : "NetPulse продолжает работу в системном трее.");
        }
        else
        {
            _trayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }
    #endregion

    #region Action Button Handlers
    private void OnToggleEditMode(object sender, RoutedEventArgs e)
    {
        _vm.ToggleEditModeCommand.Execute(null);
    }

    private void OnOpenHistory(object sender, RoutedEventArgs e)
    {
        _vm.OpenHistoryCommand.Execute(null);
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        _vm.OpenSettingsCommand.Execute(null);
    }

    private void OnMainActionClick(object sender, RoutedEventArgs e)
    {
        if (_vm.IsDiagnosticRunning)
        {
            _vm.StopTestCommand.Execute(null);
        }
        else
        {
            _vm.StartTestCommand.Execute(null);
        }
    }

    private void OnTogglePulseAction(object sender, RoutedEventArgs e)
    {
        _vm.TogglePulseCommand.Execute(null);
    }
    #endregion

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _trayService?.Dispose();
    }
}