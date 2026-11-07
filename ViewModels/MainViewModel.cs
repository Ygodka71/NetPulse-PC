using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NetPulse.I18n;
using NetPulse.Models;
using NetPulse.Services;

namespace NetPulse.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly StorageService _storageService = new();
    private readonly PingService _pingService = new();
    private readonly SpeedTestService _speedTestService = new();
    private readonly PublicIpService _publicIpService = new();
    private readonly NetworkMonitorService _networkMonitor = new();

    private TrayService? _trayService;
    private CancellationTokenSource? _monitorCts;
    private CancellationTokenSource? _diagnosticCts;

    // --- State Properties ---
    private NetworkInfoState _networkInfo = new();
    public NetworkInfoState NetworkInfo
    {
        get => _networkInfo;
        set => SetProperty(ref _networkInfo, value);
    }

    private PingStatistics _pingStats = new();
    public PingStatistics PingStats
    {
        get => _pingStats;
        set => SetProperty(ref _pingStats, value);
    }

    private SpeedState _speedState = new();
    public SpeedState SpeedState
    {
        get => _speedState;
        set => SetProperty(ref _speedState, value);
    }

    private NetworkQualityGrade? _qualityGrade;
    public NetworkQualityGrade? QualityGrade
    {
        get => _qualityGrade;
        set => SetProperty(ref _qualityGrade, value);
    }

    private PublicIpInfo? _publicIp;
    public PublicIpInfo? PublicIp
    {
        get => _publicIp;
        set => SetProperty(ref _publicIp, value);
    }

    private ObservableCollection<MetricModuleItem> _modules = [];
    public ObservableCollection<MetricModuleItem> Modules
    {
        get => _modules;
        set => SetProperty(ref _modules, value);
    }

    private ObservableCollection<PingTarget> _availableTargets = [];
    public ObservableCollection<PingTarget> AvailableTargets
    {
        get => _availableTargets;
        set => SetProperty(ref _availableTargets, value);
    }

    private PingTarget _selectedTarget = PingTarget.Cloudflare;
    public PingTarget SelectedTarget
    {
        get => _selectedTarget;
        set => SetProperty(ref _selectedTarget, value);
    }

    private ObservableCollection<TestRecord> _historyRecords = [];
    public ObservableCollection<TestRecord> HistoryRecords
    {
        get => _historyRecords;
        set => SetProperty(ref _historyRecords, value);
    }

    private bool _isRealtimeMonitoring = true;
    public bool IsRealtimeMonitoring
    {
        get => _isRealtimeMonitoring;
        set => SetProperty(ref _isRealtimeMonitoring, value);
    }

    private bool _isDiagnosticRunning = false;
    public bool IsDiagnosticRunning
    {
        get => _isDiagnosticRunning;
        set => SetProperty(ref _isDiagnosticRunning, value);
    }

    private bool _isEditMode = false;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private AppThemeMode _currentThemeMode = AppThemeMode.System;
    public AppThemeMode CurrentThemeMode
    {
        get => _currentThemeMode;
        set
        {
            if (SetProperty(ref _currentThemeMode, value))
            {
                Settings.ThemeMode = value;
                _storageService.SaveSettings(Settings);
            }
        }
    }

    private AppLanguage _currentLanguage = AppLanguage.System;
    public AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (SetProperty(ref _currentLanguage, value))
            {
                Settings.AppLanguage = value;
                _storageService.SaveSettings(Settings);
                OnPropertyChanged(nameof(IsEn));
                UpdateTrayTelemetry();
            }
        }
    }

    public bool IsEn => AppI18n.IsEn(CurrentLanguage);

    private double _uiScale = 1.0;
    public double UiScale
    {
        get => _uiScale;
        set
        {
            double rounded = Math.Round(Math.Clamp(value, 0.75, 1.75), 2);
            if (SetProperty(ref _uiScale, rounded))
            {
                Settings.UiScale = rounded;
                _storageService.SaveSettings(Settings);
            }
        }
    }

    public AppSettingsData Settings { get; private set; }

    // Dialog state properties
    private bool _showSettingsDialog;
    public bool ShowSettingsDialog
    {
        get => _showSettingsDialog;
        set => SetProperty(ref _showSettingsDialog, value);
    }

    private bool _showHistoryDialog;
    public bool ShowHistoryDialog
    {
        get => _showHistoryDialog;
        set => SetProperty(ref _showHistoryDialog, value);
    }

    private bool _showHelpDialog;
    public bool ShowHelpDialog
    {
        get => _showHelpDialog;
        set => SetProperty(ref _showHelpDialog, value);
    }

    private bool _showTestBreakdownDialog;
    public bool ShowTestBreakdownDialog
    {
        get => _showTestBreakdownDialog;
        set => SetProperty(ref _showTestBreakdownDialog, value);
    }

    private MetricHelpInfo? _activeHelpInfo;
    public MetricHelpInfo? ActiveHelpInfo
    {
        get => _activeHelpInfo;
        set => SetProperty(ref _activeHelpInfo, value);
    }

    private MetricModuleId? _activeHelpModuleId;
    public MetricModuleId? ActiveHelpModuleId
    {
        get => _activeHelpModuleId;
        set => SetProperty(ref _activeHelpModuleId, value);
    }

    // --- Commands ---
    public RelayCommand ToggleEditModeCommand { get; }
    public RelayCommand StartTestCommand { get; }
    public RelayCommand StopTestCommand { get; }
    public RelayCommand TogglePulseCommand { get; }
    public RelayCommand ResetPacketLossCommand { get; }
    public RelayCommand RefreshIpCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand CloseSettingsCommand { get; }
    public RelayCommand OpenHistoryCommand { get; }
    public RelayCommand CloseHistoryCommand { get; }
    public RelayCommand OpenBreakdownCommand { get; }
    public RelayCommand CloseBreakdownCommand { get; }
    public RelayCommand CloseHelpCommand { get; }
    public RelayCommand ClearAllHistoryCommand { get; }
    public RelayCommand ResetDefaultModulesCommand { get; }

    public RelayCommand<MetricModuleId> OpenHelpCommandGeneric { get; }
    public RelayCommand<MetricModuleId> RemoveModuleCommand { get; }
    public RelayCommand<MetricModuleId> AddModuleCommand { get; }
    public RelayCommand<TestRecord> DeleteHistoryRecordCommand { get; }

    public MainViewModel()
    {
        Settings = _storageService.LoadSettings();
        _currentThemeMode = Settings.ThemeMode;
        _currentLanguage = Settings.AppLanguage;
        _uiScale = Settings.UiScale > 0 ? Settings.UiScale : 1.0;

        // Initialize lists
        Modules = new ObservableCollection<MetricModuleItem>(Settings.Modules ?? StorageService.DefaultModules());
        HistoryRecords = new ObservableCollection<TestRecord>(_storageService.LoadHistory());

        var targets = new List<PingTarget>(PingTarget.DefaultTargets);
        if (Settings.CustomServers != null && Settings.CustomServers.Count > 0)
        {
            targets.AddRange(Settings.CustomServers);
        }
        AvailableTargets = new ObservableCollection<PingTarget>(targets);
        SelectedTarget = targets.FirstOrDefault(t => t.Id == Settings.SelectedServerId) ?? PingTarget.Cloudflare;

        // Initialize Commands
        ToggleEditModeCommand = new RelayCommand(() => IsEditMode = !IsEditMode);
        StartTestCommand = new RelayCommand(() => StartDiagnosticTest());
        StopTestCommand = new RelayCommand(() => CancelDiagnosticTest());
        TogglePulseCommand = new RelayCommand(() => ToggleRealtimeMonitoring());
        ResetPacketLossCommand = new RelayCommand(() => ResetPacketLoss());
        RefreshIpCommand = new RelayCommand(() => RefreshPublicIp(true));

        OpenSettingsCommand = new RelayCommand(() => ShowSettingsDialog = true);
        CloseSettingsCommand = new RelayCommand(() => ShowSettingsDialog = false);
        OpenHistoryCommand = new RelayCommand(() => ShowHistoryDialog = true);
        CloseHistoryCommand = new RelayCommand(() => ShowHistoryDialog = false);
        OpenBreakdownCommand = new RelayCommand(() => ShowTestBreakdownDialog = true);
        CloseBreakdownCommand = new RelayCommand(() => ShowTestBreakdownDialog = false);
        CloseHelpCommand = new RelayCommand(() => ShowHelpDialog = false);

        ClearAllHistoryCommand = new RelayCommand(() => ClearAllHistory());
        ResetDefaultModulesCommand = new RelayCommand(() => ResetDefaultModules());

        OpenHelpCommandGeneric = new RelayCommand<MetricModuleId>(id => OpenHelp(id));
        RemoveModuleCommand = new RelayCommand<MetricModuleId>(id => RemoveModule(id));
        AddModuleCommand = new RelayCommand<MetricModuleId>(id => AddModule(id));
        DeleteHistoryRecordCommand = new RelayCommand<TestRecord>(r => { if (r != null) DeleteHistoryRecord(r); });

        // Network Monitor
        NetworkInfo = _networkMonitor.GetCurrentState();
        _networkMonitor.NetworkStateChanged += state =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                NetworkInfo = state;
                if (state.IsConnected)
                {
                    RefreshPublicIp(false);
                }
                UpdateTrayTelemetry();
            });
        };

        // Start Initial Background Services
        StartRealtimeMonitoring();
        RefreshPublicIp(false);
    }

    public void SetTrayService(TrayService tray)
    {
        _trayService = tray;
        _trayService.SetEnabled(Settings.NotificationEnabled);
        UpdateTrayTelemetry();
    }

    public void UpdateTrayTelemetry()
    {
        _trayService?.UpdateTelemetry(PingStats, NetworkInfo, SpeedState, PublicIp, Settings, IsEn);
    }

    // --- Ping & Realtime Monitoring ---
    public void ToggleRealtimeMonitoring()
    {
        if (IsRealtimeMonitoring)
        {
            StopRealtimeMonitoring();
        }
        else
        {
            StartRealtimeMonitoring();
        }
    }

    private void StartRealtimeMonitoring()
    {
        StopRealtimeMonitoring();
        IsRealtimeMonitoring = true;
        _monitorCts = new CancellationTokenSource();
        var ct = _monitorCts.Token;

        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested && IsRealtimeMonitoring)
            {
                if (!IsDiagnosticRunning)
                {
                    var target = SelectedTarget;
                    long? latency = await _pingService.MeasureSinglePingAsync(target, ct);
                    var stats = _pingService.RecordPing(latency);

                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        PingStats = stats;
                        UpdateTrayTelemetry();
                    });
                }

                try
                {
                    await Task.Delay(850, ct);
                }
                catch
                {
                    break;
                }
            }
        }, ct);
    }

    private void StopRealtimeMonitoring()
    {
        _monitorCts?.Cancel();
        _monitorCts = null;
        IsRealtimeMonitoring = false;
    }

    public void ResetPacketLoss()
    {
        var updated = _pingService.ResetPacketLoss();
        PingStats = updated;
        UpdateTrayTelemetry();
    }

    // --- Full Diagnostic Speed Test ---
    public void StartDiagnosticTest()
    {
        if (IsDiagnosticRunning) return;

        _diagnosticCts?.Cancel();
        _diagnosticCts = new CancellationTokenSource();
        var ct = _diagnosticCts.Token;

        IsDiagnosticRunning = true;
        QualityGrade = null;
        SpeedState = new SpeedState
        {
            Phase = TestPhase.Pinging,
            StatusText = AppI18n.GetPhaseStatusText(TestPhase.Pinging, IsEn)
        };
        UpdateTrayTelemetry();

        Task.Run(async () =>
        {
            try
            {
                // Phase 1: Ping calibration
                var diagPingService = new PingService();
                var target = SelectedTarget;
                PingStatistics finalPingStats = PingStats;

                for (int i = 1; i <= 6; i++)
                {
                    if (ct.IsCancellationRequested) return;

                    long? latency = await diagPingService.MeasureSinglePingAsync(target, ct);
                    finalPingStats = diagPingService.RecordPing(latency);

                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        PingStats = finalPingStats;
                        SpeedState.ProgressPercent = (i / 6f) * 0.2f;
                        OnPropertyChanged(nameof(SpeedState));
                        UpdateTrayTelemetry();
                    });

                    await Task.Delay(180, ct);
                }

                if (ct.IsCancellationRequested) return;

                // Phase 2: Download Speed
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    SpeedState = new SpeedState
                    {
                        Phase = TestPhase.Downloading,
                        StatusText = AppI18n.GetPhaseStatusText(TestPhase.Downloading, IsEn),
                        ProgressPercent = 0.2f
                    };
                    UpdateTrayTelemetry();
                });

                var downloadResult = await _speedTestService.RunDownloadTestAsync((currentMbps, peakMbps, progress) =>
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        SpeedState.CurrentMbps = currentMbps;
                        SpeedState.PeakMbps = peakMbps;
                        SpeedState.ProgressPercent = 0.2f + (progress * 0.45f);
                        OnPropertyChanged(nameof(SpeedState));
                        UpdateTrayTelemetry();
                    });
                }, ct);

                if (ct.IsCancellationRequested) return;

                // Phase 3: Upload Speed
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    SpeedState = new SpeedState
                    {
                        Phase = TestPhase.Uploading,
                        DownloadResult = downloadResult,
                        DownloadFinalMbps = downloadResult.FinalAvgMbps,
                        CurrentMbps = 0.0,
                        ProgressPercent = 0.65f,
                        StatusText = AppI18n.GetPhaseStatusText(TestPhase.Uploading, IsEn)
                    };
                    UpdateTrayTelemetry();
                });

                var uploadResult = await _speedTestService.RunUploadTestAsync((currentMbps, peakMbps, progress) =>
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        SpeedState.CurrentMbps = currentMbps;
                        SpeedState.PeakMbps = peakMbps;
                        SpeedState.ProgressPercent = 0.65f + (progress * 0.35f);
                        OnPropertyChanged(nameof(SpeedState));
                        UpdateTrayTelemetry();
                    });
                }, ct);

                if (ct.IsCancellationRequested) return;

                // Phase 4: Completed & Quality Score Calculation
                var grade = CalculateQualityGrade(
                    downloadResult.FinalAvgMbps,
                    uploadResult.FinalAvgMbps,
                    finalPingStats.CurrentPingMs,
                    finalPingStats.JitterMs);

                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    IsDiagnosticRunning = false;
                    QualityGrade = grade;
                    SpeedState = new SpeedState
                    {
                        Phase = TestPhase.Completed,
                        DownloadResult = downloadResult,
                        UploadResult = uploadResult,
                        DownloadFinalMbps = downloadResult.FinalAvgMbps,
                        UploadFinalMbps = uploadResult.FinalAvgMbps,
                        CurrentMbps = downloadResult.FinalAvgMbps,
                        PeakMbps = downloadResult.PeakMbps,
                        ProgressPercent = 1f,
                        StatusText = AppI18n.GetPhaseStatusText(TestPhase.Completed, IsEn),
                        CompletedTimestamp = now
                    };

                    var record = new TestRecord
                    {
                        DownloadMbps = downloadResult.FinalAvgMbps,
                        UploadMbps = uploadResult.FinalAvgMbps,
                        PingMs = finalPingStats.CurrentPingMs,
                        AvgLatencyMs = finalPingStats.AvgLatencyMs,
                        JitterMs = finalPingStats.JitterMs,
                        ConnectionType = NetworkInfo.NetworkName,
                        RatingTitle = grade.Title,
                        ServerName = target.Name,
                        Timestamp = now
                    };

                    HistoryRecords.Insert(0, record);
                    _storageService.SaveHistory(HistoryRecords.ToList());

                    UpdateTrayTelemetry();
                    _trayService?.ShowNotification(
                        "NetPulse",
                        IsEn ? $"Test Complete: ↓ {downloadResult.FinalAvgMbps:F1} Mbps | ↑ {uploadResult.FinalAvgMbps:F1} Mbps"
                             : $"Тест завершен: ↓ {downloadResult.FinalAvgMbps:F1} Мбит/с | ↑ {uploadResult.FinalAvgMbps:F1} Мбит/с");
                });
            }
            catch (Exception)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    IsDiagnosticRunning = false;
                    SpeedState = new SpeedState { Phase = TestPhase.Idle };
                });
            }
        }, ct);
    }

    public void CancelDiagnosticTest()
    {
        _diagnosticCts?.Cancel();
        _diagnosticCts = null;
        IsDiagnosticRunning = false;
        SpeedState = new SpeedState { Phase = TestPhase.Idle };
        UpdateTrayTelemetry();
    }

    private NetworkQualityGrade CalculateQualityGrade(
        double downloadMbps,
        double uploadMbps,
        long pingMs,
        long jitterMs)
    {
        if (downloadMbps >= 80.0 && pingMs is >= 1 and <= 35 && jitterMs <= 6)
        {
            return new NetworkQualityGrade
            {
                Title = IsEn ? "Excellent" : "Превосходно",
                Subtitle = AppI18n.GetGradeSubtitle(98, IsEn),
                ScoreOutOf100 = 98,
                ColorHex = 0xFF10B981,
                GradeType = GradeType.Excellent
            };
        }
        if (downloadMbps >= 30.0 && pingMs is >= 1 and <= 70 && jitterMs <= 15)
        {
            return new NetworkQualityGrade
            {
                Title = IsEn ? "Great" : "Отлично",
                Subtitle = AppI18n.GetGradeSubtitle(85, IsEn),
                ScoreOutOf100 = 85,
                ColorHex = 0xFF00E5FF,
                GradeType = GradeType.Great
            };
        }
        if (downloadMbps >= 10.0 && pingMs is >= 1 and <= 120)
        {
            return new NetworkQualityGrade
            {
                Title = IsEn ? "Good" : "Хорошо",
                Subtitle = AppI18n.GetGradeSubtitle(70, IsEn),
                ScoreOutOf100 = 70,
                ColorHex = 0xFFF59E0B,
                GradeType = GradeType.Good
            };
        }
        if (downloadMbps > 0.0)
        {
            return new NetworkQualityGrade
            {
                Title = IsEn ? "Basic" : "Базовое",
                Subtitle = AppI18n.GetGradeSubtitle(50, IsEn),
                ScoreOutOf100 = 50,
                ColorHex = 0xFFF43F5E,
                GradeType = GradeType.Basic
            };
        }

        return new NetworkQualityGrade
        {
            Title = IsEn ? "Unstable" : "Нестабильно",
            Subtitle = AppI18n.GetGradeSubtitle(20, IsEn),
            ScoreOutOf100 = 20,
            ColorHex = 0xFF64748B,
            GradeType = GradeType.Unstable
        };
    }

    // --- Public IP Service ---
    public void RefreshPublicIp(bool force)
    {
        Task.Run(async () =>
        {
            var info = await _publicIpService.GetPublicIpInfoAsync(force);
            if (info != null)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    PublicIp = info;
                    UpdateTrayTelemetry();
                });
            }
        });
    }

    // --- Module Configuration & Ordering ---
    public void RemoveModule(MetricModuleId id)
    {
        var item = Modules.FirstOrDefault(m => m.Id == id);
        if (item != null)
        {
            item.IsVisible = false;
            OnPropertyChanged(nameof(Modules));
            SaveModules();
        }
    }

    public void AddModule(MetricModuleId id)
    {
        var item = Modules.FirstOrDefault(m => m.Id == id);
        if (item != null)
        {
            item.IsVisible = true;
        }
        else
        {
            Modules.Add(new MetricModuleItem(id, true));
        }
        OnPropertyChanged(nameof(Modules));
        SaveModules();
    }

    public void MoveModule(int index, int offset)
    {
        int newIndex = index + offset;
        if (index < 0 || index >= Modules.Count || newIndex < 0 || newIndex >= Modules.Count) return;

        Modules.Move(index, newIndex);
        SaveModules();
    }

    public void ResetDefaultModules()
    {
        Modules = new ObservableCollection<MetricModuleItem>(StorageService.DefaultModules());
        SaveModules();
    }

    private void SaveModules()
    {
        Settings.Modules = Modules.ToList();
        _storageService.SaveSettings(Settings);
    }

    // --- Target Server Selection ---
    public void SelectTarget(PingTarget target)
    {
        SelectedTarget = target;
        Settings.SelectedServerId = target.Id;
        _storageService.SaveSettings(Settings);

        _pingService.Reset();
        PingStats = new PingStatistics();
    }

    public void AddCustomTarget(string name, string host, int port)
    {
        string cleanHost = host.Trim()
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Split('/')[0].Trim();

        if (string.IsNullOrEmpty(cleanHost)) return;

        int validPort = port is >= 1 and <= 65535 ? port : 53;
        string cleanName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : cleanHost;

        var newTarget = new PingTarget
        {
            Id = "custom_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Name = cleanName,
            Host = cleanHost,
            Port = validPort,
            HttpFallbackUrl = $"https://{cleanHost}",
            IsCustom = true
        };

        Settings.CustomServers ??= [];
        Settings.CustomServers.Add(newTarget);
        _storageService.SaveSettings(Settings);

        AvailableTargets.Add(newTarget);
        SelectTarget(newTarget);
    }

    public void DeleteCustomTarget(PingTarget target)
    {
        if (!target.IsCustom) return;

        Settings.CustomServers?.RemoveAll(t => t.Id == target.Id);
        _storageService.SaveSettings(Settings);

        AvailableTargets.Remove(target);
        if (SelectedTarget.Id == target.Id)
        {
            SelectTarget(PingTarget.Cloudflare);
        }
    }

    // --- History Management ---
    public void DeleteHistoryRecord(TestRecord record)
    {
        HistoryRecords.Remove(record);
        _storageService.SaveHistory(HistoryRecords.ToList());
    }

    public void ClearAllHistory()
    {
        HistoryRecords.Clear();
        _storageService.SaveHistory(HistoryRecords.ToList());
    }

    // --- Help Dialog ---
    public void OpenHelp(MetricModuleId id)
    {
        ActiveHelpModuleId = id;
        ActiveHelpInfo = AppI18n.GetMetricHelp(id, IsEn);
        ShowHelpDialog = true;
    }

    // --- Tray / Notification Preferences ---
    public void SetNotificationEnabled(bool enabled)
    {
        Settings.NotificationEnabled = enabled;
        _storageService.SaveSettings(Settings);
        _trayService?.SetEnabled(enabled);
        UpdateTrayTelemetry();
    }

    public void SetNotifOption(string option, bool val)
    {
        switch (option)
        {
            case "ping": Settings.NotifShowPing = val; break;
            case "jitter": Settings.NotifShowJitter = val; break;
            case "speed": Settings.NotifShowSpeed = val; break;
            case "ip": Settings.NotifShowIp = val; break;
        }
        _storageService.SaveSettings(Settings);
        UpdateTrayTelemetry();
    }

    // --- INotifyPropertyChanged Helper ---
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(name);
        return true;
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);
}
