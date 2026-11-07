using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NetPulse.Models;
using NetPulse.ViewModels;

namespace NetPulse.Views;

public partial class SettingsDialog : UserControl
{
    public class ServerItemViewModel
    {
        public PingTarget Target { get; set; } = new();
        public Brush BorderBrush { get; set; } = Brushes.Transparent;
        public Brush CheckBg { get; set; } = Brushes.Transparent;
        public Brush CheckBorder { get; set; } = Brushes.Transparent;
        public string CheckMark { get; set; } = "";
        public Visibility DeleteVisibility { get; set; } = Visibility.Collapsed;
    }

    public SettingsDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateContent();
    }

    public void UpdateContent()
    {
        if (DataContext is not MainViewModel vm) return;

        bool isEn = vm.IsEn;
        TitleBlock.Text = isEn ? "Settings" : "Настройки";
        ThemeSectionHeader.Text = isEn ? "APPEARANCE THEME" : "ТЕМА ОФОРМЛЕНИЯ";
        ThemeSystemLabel.Text = isEn ? "System" : "Система";
        ThemeLightLabel.Text = isEn ? "Light" : "Светлая";
        ThemeDarkLabel.Text = isEn ? "Dark" : "Тёмная";
        ScaleSectionHeader.Text = isEn ? "INTERFACE SCALE" : "МАСШТАБ ИНТЕРФЕЙСА";
        ServerSectionHeader.Text = isEn ? "TEST SERVER" : "ТЕСТОВЫЙ СЕРВЕР";
        AddServerToggleBtn.Content = isEn ? "+ Custom Server" : "+ Свой сервер";
        TraySectionHeader.Text = isEn ? "SYSTEM TRAY & WINDOWS TELEMETRY" : "СИСТЕМНЫЙ ТРЕЙ И УВЕДОМЛЕНИЯ WINDOWS";
        TrayEnabledCheck.Content = isEn ? "Show icon and telemetry in system tray" : "Отображать значок и телеметрию в системном трее";
        TrayPingCheck.Content = isEn ? "Show current and average ping" : "Показывать текущий и средний пинг";
        TrayJitterCheck.Content = isEn ? "Show jitter and packet loss" : "Показывать джиттер и процент потерь";
        TraySpeedCheck.Content = isEn ? "Show speed during tests" : "Показывать скорость при тесте";
        TrayIpCheck.Content = isEn ? "Show public IP address" : "Показывать внешний IP адрес";
        LangSectionHeader.Text = isEn ? "INTERFACE LANGUAGE" : "ЯЗЫК ИНТЕРФЕЙСА";
        DoneBtn.Content = isEn ? "Done" : "Готово";

        // Update Theme Highlight
        UpdateThemeButtons(vm.CurrentThemeMode);

        // Update Scale UI
        UpdateScaleUi(vm);

        // Update Language Highlight
        UpdateLanguageButtons(vm.CurrentLanguage);

        // Update Tray Checkboxes
        TrayEnabledCheck.IsChecked = vm.Settings.NotificationEnabled;
        TrayPingCheck.IsChecked = vm.Settings.NotifShowPing;
        TrayJitterCheck.IsChecked = vm.Settings.NotifShowJitter;
        TraySpeedCheck.IsChecked = vm.Settings.NotifShowSpeed;
        TrayIpCheck.IsChecked = vm.Settings.NotifShowIp;
        UpdateTraySubOptionsState(vm.Settings.NotificationEnabled);

        // Update Server List
        UpdateServerList(vm);
    }

    private void UpdateThemeButtons(AppThemeMode mode)
    {
        var activeBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
        var inactiveBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x32, 0x4D));

        ThemeSystemBtn.BorderBrush = mode == AppThemeMode.System ? activeBrush : inactiveBrush;
        ThemeLightBtn.BorderBrush = mode == AppThemeMode.Light ? activeBrush : inactiveBrush;
        ThemeDarkBtn.BorderBrush = mode == AppThemeMode.Dark ? activeBrush : inactiveBrush;
    }

    private void UpdateLanguageButtons(AppLanguage lang)
    {
        var activeBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
        var inactiveBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x32, 0x4D));

        LangRuBtn.BorderBrush = lang == AppLanguage.Ru ? activeBrush : inactiveBrush;
        LangEnBtn.BorderBrush = lang == AppLanguage.En ? activeBrush : inactiveBrush;
    }

    private void UpdateServerList(MainViewModel vm)
    {
        var activeBorder = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
        var inactiveBorder = new SolidColorBrush(Color.FromRgb(0x24, 0x32, 0x4D));
        var checkBg = new SolidColorBrush(Color.FromArgb(40, 0x00, 0xE5, 0xFF));

        var items = vm.AvailableTargets.Select(t =>
        {
            bool isSelected = t.Id == vm.SelectedTarget.Id;
            return new ServerItemViewModel
            {
                Target = t,
                BorderBrush = isSelected ? activeBorder : inactiveBorder,
                CheckBg = isSelected ? checkBg : Brushes.Transparent,
                CheckBorder = isSelected ? activeBorder : inactiveBorder,
                CheckMark = isSelected ? "✓" : "",
                DeleteVisibility = t.IsCustom ? Visibility.Visible : Visibility.Collapsed
            };
        }).ToList();

        ServersItemsControl.ItemsSource = items;
    }

    private void OnThemeSystemClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CurrentThemeMode = AppThemeMode.System;
            UpdateThemeButtons(AppThemeMode.System);
        }
    }

    private void OnThemeLightClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CurrentThemeMode = AppThemeMode.Light;
            UpdateThemeButtons(AppThemeMode.Light);
        }
    }

    private void OnThemeDarkClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CurrentThemeMode = AppThemeMode.Dark;
            UpdateThemeButtons(AppThemeMode.Dark);
        }
    }

    private void OnLangRuClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CurrentLanguage = AppLanguage.Ru;
            UpdateContent();
        }
    }

    private void OnLangEnClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CurrentLanguage = AppLanguage.En;
            UpdateContent();
        }
    }

    private void OnToggleAddServerForm(object sender, RoutedEventArgs e)
    {
        AddServerPanel.Visibility = AddServerPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnCancelAddServer(object sender, RoutedEventArgs e)
    {
        AddServerPanel.Visibility = Visibility.Collapsed;
        ServerNameBox.Text = "";
        ServerHostBox.Text = "";
        ServerPortBox.Text = "53";
    }

    private void OnSaveCustomServer(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            string name = ServerNameBox.Text.Trim();
            string host = ServerHostBox.Text.Trim();
            int.TryParse(ServerPortBox.Text.Trim(), out int port);

            if (!string.IsNullOrEmpty(host))
            {
                vm.AddCustomTarget(name, host, port);
                OnCancelAddServer(sender, e);
                UpdateServerList(vm);
            }
        }
    }

    private void OnSelectServerClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: PingTarget target } && DataContext is MainViewModel vm)
        {
            vm.SelectTarget(target);
            UpdateServerList(vm);
        }
    }

    private void OnDeleteCustomServerClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PingTarget target } && DataContext is MainViewModel vm)
        {
            vm.DeleteCustomTarget(target);
            UpdateServerList(vm);
        }
    }

    private void OnTrayCheckChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            bool isTrayEnabled = TrayEnabledCheck.IsChecked is true;
            vm.SetNotificationEnabled(isTrayEnabled);
            vm.SetNotifOption("ping", TrayPingCheck.IsChecked is true);
            vm.SetNotifOption("jitter", TrayJitterCheck.IsChecked is true);
            vm.SetNotifOption("speed", TraySpeedCheck.IsChecked is true);
            vm.SetNotifOption("ip", TrayIpCheck.IsChecked is true);

            UpdateTraySubOptionsState(isTrayEnabled);
        }
    }

    private void UpdateTraySubOptionsState(bool isTrayEnabled)
    {
        TraySubOptionsPanel.IsEnabled = isTrayEnabled;
        TraySubOptionsPanel.Opacity = isTrayEnabled ? 1.0 : 0.45;
    }

    private bool _isUpdatingScaleUi = false;

    private void UpdateScaleUi(MainViewModel vm)
    {
        _isUpdatingScaleUi = true;
        try
        {
            int percent = (int)Math.Round(vm.UiScale * 100);
            ScaleValueLabel.Text = $"{percent}%";
            ScaleSlider.Value = percent;

            ScaleHintLabel.Text = percent switch
            {
                100 => (vm.IsEn ? "(Default)" : "(По умолчанию)"),
                < 100 => (vm.IsEn ? "(Compact)" : "(Компактный)"),
                _ => (vm.IsEn ? "(Enlarged)" : "(Увеличенный)")
            };

            var activeBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            var inactiveBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x32, 0x4D));
            var textActive = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            var textInactive = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));

            Border[] presetBorders = [Scale80Btn, Scale90Btn, Scale100Btn, Scale115Btn, Scale130Btn, Scale150Btn];
            foreach (var b in presetBorders)
            {
                if (b == null) continue;
                if (double.TryParse(b.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double presetVal))
                {
                    bool isSelected = Math.Abs(presetVal - vm.UiScale) < 0.02;
                    b.BorderBrush = isSelected ? activeBrush : inactiveBrush;
                    if (b.Child is TextBlock tb)
                    {
                        tb.Foreground = isSelected ? textActive : textInactive;
                    }
                }
            }
        }
        finally
        {
            _isUpdatingScaleUi = false;
        }
    }

    private void OnScaleMinusClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            int current = (int)Math.Round(vm.UiScale * 100);
            int next = Math.Max(80, (int)(Math.Ceiling((current - 5) / 5.0) * 5));
            vm.UiScale = next / 100.0;
            UpdateScaleUi(vm);
        }
    }

    private void OnScalePlusClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            int current = (int)Math.Round(vm.UiScale * 100);
            int next = Math.Min(150, (int)(Math.Floor((current + 5) / 5.0) * 5));
            vm.UiScale = next / 100.0;
            UpdateScaleUi(vm);
        }
    }

    private void OnScaleSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingScaleUi) return;
        if (DataContext is MainViewModel vm)
        {
            double newScale = Math.Round(e.NewValue) / 100.0;
            if (Math.Abs(vm.UiScale - newScale) >= 0.01)
            {
                vm.UiScale = newScale;
                UpdateScaleUi(vm);
            }
        }
    }

    private void OnScalePresetClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && double.TryParse(b.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
            if (DataContext is MainViewModel vm)
            {
                vm.UiScale = val;
                UpdateScaleUi(vm);
            }
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseSettingsCommand.Execute(null);
        }
    }
}
