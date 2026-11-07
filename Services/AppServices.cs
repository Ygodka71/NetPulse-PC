using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using NetPulse.Models;

namespace NetPulse.Services;

#region StorageService & AppSettings
public class AppSettingsData
{
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    public AppLanguage AppLanguage { get; set; } = AppLanguage.System;
    public bool NotificationEnabled { get; set; } = true;
    public bool NotifShowPing { get; set; } = true;
    public bool NotifShowJitter { get; set; } = true;
    public bool NotifShowSpeed { get; set; } = true;
    public bool NotifShowIp { get; set; } = true;
    public string SelectedServerId { get; set; } = "cf";
    public double UiScale { get; set; } = 1.0;
    public List<MetricModuleItem>? Modules { get; set; }
    public List<PingTarget> CustomServers { get; set; } = [];
}

public class StorageService
{
    private readonly string _appDataFolder;
    private readonly string _settingsPath;
    private readonly string _historyPath;

    public StorageService()
    {
        _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetPulse");
        Directory.CreateDirectory(_appDataFolder);
        _settingsPath = Path.Combine(_appDataFolder, "settings.json");
        _historyPath = Path.Combine(_appDataFolder, "history.json");
    }

    public static List<MetricModuleItem> DefaultModules() =>
    [
        new(MetricModuleId.ConnectionType, true),
        new(MetricModuleId.GeoIp, true),
        new(MetricModuleId.CurrentPing, true),
        new(MetricModuleId.AvgLatency, true),
        new(MetricModuleId.Jitter, true),
        new(MetricModuleId.PacketLoss, true),
        new(MetricModuleId.Download, true),
        new(MetricModuleId.Upload, true)
    ];

    public AppSettingsData LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettingsData>(json);
                if (settings != null)
                {
                    settings.Modules ??= DefaultModules();
                    return settings;
                }
            }
        }
        catch
        {
            // Ignore error and return defaults
        }

        return new AppSettingsData
        {
            Modules = DefaultModules()
        };
    }

    public void SaveSettings(AppSettingsData settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore write error
        }
    }

    public List<TestRecord> LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                string json = File.ReadAllText(_historyPath);
                var list = JsonSerializer.Deserialize<List<TestRecord>>(json);
                if (list != null) return list;
            }
        }
        catch
        {
            // Ignore read error
        }
        return [];
    }

    public void SaveHistory(List<TestRecord> records)
    {
        try
        {
            string json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
        }
        catch
        {
            // Ignore write error
        }
    }
}
#endregion

#region TrayService
public class TrayService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly Action _onOpenWindow;
    private readonly Action _onStartTest;
    private readonly Action _onTogglePulse;
    private readonly Action _onExitApp;

    public bool IsEnabled { get; private set; } = true;

    public TrayService(
        Action onOpenWindow,
        Action onStartTest,
        Action onTogglePulse,
        Action onExitApp)
    {
        _onOpenWindow = onOpenWindow;
        _onStartTest = onStartTest;
        _onTogglePulse = onTogglePulse;
        _onExitApp = onExitApp;

        InitializeTray();
    }

    private void InitializeTray()
    {
        try
        {
            Icon appIcon = SystemIcons.Application;
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "app_icon.ico");
            if (File.Exists(iconPath))
            {
                appIcon = new Icon(iconPath);
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "NetPulse — Умный тест сети",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("Открыть NetPulse", null, (_, _) => _onOpenWindow())
            {
                Font = new Font(contextMenu.Font, System.Drawing.FontStyle.Bold)
            };
            var testItem = new ToolStripMenuItem("Запустить тест скорости", null, (_, _) => _onStartTest());
            var pulseItem = new ToolStripMenuItem("Живой пульс сети", null, (_, _) => _onTogglePulse());
            var exitItem = new ToolStripMenuItem("Выход", null, (_, _) => _onExitApp());

            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(testItem);
            contextMenu.Items.Add(pulseItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (_, _) => _onOpenWindow();
        }
        catch
        {
            // Fallback gracefully if tray creation fails
        }
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = enabled;
        }
    }

    public void UpdateTelemetry(
        PingStatistics pingStats,
        NetworkInfoState networkInfo,
        SpeedState speedState,
        PublicIpInfo? publicIp,
        AppSettingsData settings,
        bool isEn)
    {
        if (_notifyIcon == null || !IsEnabled) return;

        try
        {
            string msUnit = isEn ? "ms" : "мс";
            string mbpsUnit = isEn ? "Mbps" : "Мбит/с";

            string pingText = pingStats.CurrentPingMs > 0 ? $"{pingStats.CurrentPingMs} {msUnit}" : "—";
            string speedText = speedState.CurrentMbps > 0 ? $"{speedState.CurrentMbps:F1} {mbpsUnit}" : "";

            var textParts = new List<string>();

            if (settings.NotifShowPing)
            {
                textParts.Add($"Ping: {pingText}");
            }
            if (settings.NotifShowJitter && pingStats.JitterMs > 0)
            {
                textParts.Add($"Jitter: {pingStats.JitterMs}{msUnit}");
            }
            if (settings.NotifShowSpeed && !string.IsNullOrEmpty(speedText))
            {
                textParts.Add($"Speed: {speedText}");
            }
            if (settings.NotifShowIp && publicIp != null && !string.IsNullOrEmpty(publicIp.Ip))
            {
                textParts.Add($"IP: {publicIp.Ip}");
            }

            string combined = $"NetPulse • " + string.Join(" | ", textParts);
            if (combined.Length > 63)
            {
                combined = combined[..60] + "...";
            }

            _notifyIcon.Text = combined;
        }
        catch
        {
            // Ignore tooltip length or format exceptions
        }
    }

    public void ShowNotification(string title, string message)
    {
        if (_notifyIcon != null && IsEnabled)
        {
            try
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
            catch
            {
                // Ignore balloon tip exceptions
            }
        }
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        GC.SuppressFinalize(this);
    }
}
#endregion
