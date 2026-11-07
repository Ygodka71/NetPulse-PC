using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NetPulse.I18n;
using NetPulse.Models;
using NetPulse.ViewModels;

namespace NetPulse.Controls;

public partial class MetricCardsContainer : UserControl
{
    public MetricCardsContainer()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateColumns();
    }

    public void UpdateColumns()
    {
        double w = ActualWidth;
        if (w <= 0) return;

        // Breakpoints for card grid:
        // w < 400px     -> 1 column (only on extremely narrow window)
        // 400px - 660px -> 2 columns (standard NetPulse size: ~200-260px per card)
        // 660px - 950px -> 3 columns (medium-wide window: ~210-250px per card)
        // w >= 950px    -> 4 columns (wide/fullscreen: ~220-270px per card)
        int cols = w switch
        {
            < 400 => 1,
            < 660 => 2,
            < 950 => 3,
            _     => 4
        };

        if (CardsGrid.Columns != cols)
        {
            CardsGrid.Columns = cols;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            oldVm.Modules.CollectionChanged -= OnModulesCollectionChanged;
        }

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            newVm.Modules.CollectionChanged += OnModulesCollectionChanged;
            RebuildCards();
        }
    }

    private void OnModulesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildCards();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.Modules) or
            nameof(MainViewModel.IsEditMode) or
            nameof(MainViewModel.IsEn) or
            nameof(MainViewModel.CurrentLanguage))
        {
            RebuildCards();
        }
        else if (e.PropertyName is nameof(MainViewModel.PingStats) or
                 nameof(MainViewModel.SpeedState) or
                 nameof(MainViewModel.NetworkInfo) or
                 nameof(MainViewModel.PublicIp))
        {
            UpdateCardValues();
        }
    }

    public void RebuildCards()
    {
        if (DataContext is not MainViewModel vm) return;

        CardsGrid.Children.Clear();
        var visibleModules = vm.Modules.Where(m => m.IsVisible).ToList();

        for (int i = 0; i < visibleModules.Count; i++)
        {
            var module = visibleModules[i];
            int currentIndex = vm.Modules.IndexOf(module);
            bool canMoveBackward = i > 0;
            bool canMoveForward = i < visibleModules.Count - 1;

            var card = CreateCard(module.Id, currentIndex, canMoveBackward, canMoveForward, vm);
            CardsGrid.Children.Add(card);
        }

        UpdateColumns();
    }

    private Border CreateCard(MetricModuleId id, int indexInAll, bool canMoveBackward, bool canMoveForward, MainViewModel vm)
    {
        var accentBrush = GetAccentBrush(id);
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x13, 0x1B, 0x2B)),
            BorderBrush = vm.IsEditMode
                ? new SolidColorBrush(Color.FromArgb(160, 0x00, 0xE5, 0xFF))
                : new SolidColorBrush(Color.FromRgb(0x24, 0x32, 0x4D)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(3.5),
            MinHeight = 84,
            Tag = id
        };

        var mainPanel = new Grid();
        mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // --- Top Row: Title + Help Button / Edit Mode Actions ---
        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        string titleStr = AppI18n.GetMetricTitle(id, vm.IsEn);

        var titleText = new TextBlock
        {
            Text = titleStr,
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        titleStack.Children.Add(titleText);

        bool hasHelp = id is MetricModuleId.Jitter or MetricModuleId.PacketLoss or MetricModuleId.GeoIp or MetricModuleId.ConnectionType;
        if (hasHelp && !vm.IsEditMode)
        {
            var helpBtn = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(35, 0x00, 0xE5, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 0x00, 0xE5, 0xFF)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(5, 0, 0, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Справка"
            };
            helpBtn.Child = new TextBlock
            {
                Text = "?",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            helpBtn.MouseLeftButtonUp += (_, _) => vm.OpenHelp(id);
            titleStack.Children.Add(helpBtn);
        }
        Grid.SetColumn(titleStack, 0);
        topRow.Children.Add(titleStack);

        // Top Right: Edit actions OR Icon box
        if (vm.IsEditMode)
        {
            var editStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            if (canMoveBackward)
            {
                var moveLeftBtn = CreateActionButton("←", () => vm.MoveModule(indexInAll, -1), Color.FromRgb(0x38, 0xBD, 0xF8));
                editStack.Children.Add(moveLeftBtn);
            }
            if (canMoveForward)
            {
                var moveRightBtn = CreateActionButton("→", () => vm.MoveModule(indexInAll, 1), Color.FromRgb(0x38, 0xBD, 0xF8));
                editStack.Children.Add(moveRightBtn);
            }

            var removeBtn = CreateActionButton("✕", () => vm.RemoveModule(id), Color.FromRgb(0xF4, 0x3F, 0x5E));
            editStack.Children.Add(removeBtn);

            Grid.SetColumn(editStack, 1);
            topRow.Children.Add(editStack);
        }
        else
        {
            var iconBox = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(Color.FromArgb(35, accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B)),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBox.Child = CreateIcon(id, accentBrush.Color);
            Grid.SetColumn(iconBox, 1);
            topRow.Children.Add(iconBox);
        }

        Grid.SetRow(topRow, 0);
        mainPanel.Children.Add(topRow);

        // --- Bottom Row: Value + Unit + SubInfo ---
        var bottomGrid = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var valStack = new StackPanel();

        var valueRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
        var valBlock = new TextBlock
        {
            Name = $"Val_{id}",
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Consolas, Roboto Mono"),
            Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        valueRow.Children.Add(valBlock);

        var unitBlock = new TextBlock
        {
            Name = $"Unit_{id}",
            FontSize = 11,
            FontFamily = new FontFamily("Consolas, Roboto Mono"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            Margin = new Thickness(3, 0, 0, 1),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        valueRow.Children.Add(unitBlock);
        valStack.Children.Add(valueRow);

        var subBlock = new TextBlock
        {
            Name = $"Sub_{id}",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
            Margin = new Thickness(0, 1, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        valStack.Children.Add(subBlock);

        Grid.SetColumn(valStack, 0);
        bottomGrid.Children.Add(valStack);

        // Action button on right of bottom row (e.g. Reset packet loss)
        if (id == MetricModuleId.PacketLoss && !vm.IsEditMode)
        {
            var resetBtn = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 0x10, 0xB9, 0x81)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 0x10, 0xB9, 0x81)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = Cursors.Hand
            };
            resetBtn.Child = new TextBlock
            {
                Text = vm.IsEn ? "Reset" : "Сброс",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81))
            };
            resetBtn.MouseLeftButtonUp += (_, _) => vm.ResetPacketLoss();
            Grid.SetColumn(resetBtn, 1);
            bottomGrid.Children.Add(resetBtn);
        }
        else if (id == MetricModuleId.GeoIp && !vm.IsEditMode)
        {
            border.Cursor = Cursors.Hand;
            border.MouseLeftButtonUp += (_, _) => vm.OpenHelp(MetricModuleId.GeoIp);
        }

        Grid.SetRow(bottomGrid, 2);
        mainPanel.Children.Add(bottomGrid);

        border.Child = mainPanel;
        PopulateCardData(border, id, vm);
        return border;
    }

    private static Border CreateActionButton(string text, Action action, Color color)
    {
        var btn = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            Background = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, color.R, color.G, color.B)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(2, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btn.Child = new TextBlock
        {
            Text = text,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(color),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        btn.MouseLeftButtonUp += (_, _) => action();
        return btn;
    }

    private static UIElement CreateIcon(MetricModuleId id, Color color)
    {
        string symbol = id switch
        {
            MetricModuleId.CurrentPing => "⚡",
            MetricModuleId.AvgLatency => "⏱",
            MetricModuleId.Jitter => "〰",
            MetricModuleId.PacketLoss => "📦",
            MetricModuleId.Download => "↓",
            MetricModuleId.Upload => "↑",
            MetricModuleId.GeoIp => "🌐",
            MetricModuleId.ConnectionType => "🔗",
            _ => "●"
        };

        return new TextBlock
        {
            Text = symbol,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(color),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static SolidColorBrush GetAccentBrush(MetricModuleId id)
    {
        return id switch
        {
            MetricModuleId.CurrentPing => new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)),
            MetricModuleId.AvgLatency => new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8)),
            MetricModuleId.Jitter => new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)), // Cyan
            MetricModuleId.PacketLoss => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)), // Emerald calm
            MetricModuleId.Download => new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)),
            MetricModuleId.Upload => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
            MetricModuleId.GeoIp => new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)),
            MetricModuleId.ConnectionType => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
            _ => new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF))
        };
    }

    public void UpdateCardValues()
    {
        if (DataContext is not MainViewModel vm) return;

        foreach (var child in CardsGrid.Children)
        {
            if (child is Border b && b.Tag is MetricModuleId id)
            {
                PopulateCardData(b, id, vm);
            }
        }
    }

    private static void PopulateCardData(Border border, MetricModuleId id, MainViewModel vm)
    {
        if (border.Child is not Grid mainGrid) return;
        var bottomGrid = mainGrid.Children.OfType<Grid>().LastOrDefault();
        if (bottomGrid == null) return;
        var valStack = bottomGrid.Children.OfType<StackPanel>().FirstOrDefault();
        if (valStack == null) return;

        var valueRow = valStack.Children.OfType<StackPanel>().FirstOrDefault();
        var valBlock = valueRow?.Children.OfType<TextBlock>().FirstOrDefault();
        var unitBlock = valueRow?.Children.OfType<TextBlock>().LastOrDefault();
        var subBlock = valStack.Children.OfType<TextBlock>().LastOrDefault();

        if (valBlock == null || unitBlock == null || subBlock == null) return;

        bool isEn = vm.IsEn;
        string msUnit = isEn ? "ms" : "мс";
        string mbpsUnit = isEn ? "Mbps" : "Мбит/с";

        switch (id)
        {
            case MetricModuleId.CurrentPing:
                valBlock.Text = vm.PingStats.CurrentPingMs > 0 ? $"{vm.PingStats.CurrentPingMs}" : "—";
                unitBlock.Text = vm.PingStats.CurrentPingMs > 0 ? msUnit : "";
                subBlock.Text = isEn
                    ? (vm.PingStats.TotalPingsSent > 0 ? $"Pings sent: {vm.PingStats.TotalPingsSent}" : "Ready")
                    : (vm.PingStats.TotalPingsSent > 0 ? $"Отправлено: {vm.PingStats.TotalPingsSent}" : "Готов");
                break;

            case MetricModuleId.AvgLatency:
                valBlock.Text = vm.PingStats.AvgLatencyMs > 0 ? $"{vm.PingStats.AvgLatencyMs}" : "—";
                unitBlock.Text = vm.PingStats.AvgLatencyMs > 0 ? msUnit : "";
                subBlock.Text = isEn
                    ? $"Min: {vm.PingStats.MinLatencyMs} / Max: {vm.PingStats.MaxLatencyMs} {msUnit}"
                    : $"Мин: {vm.PingStats.MinLatencyMs} / Макс: {vm.PingStats.MaxLatencyMs} {msUnit}";
                break;

            case MetricModuleId.Jitter:
                valBlock.Text = $"{vm.PingStats.JitterMs}";
                unitBlock.Text = msUnit;
                subBlock.Text = isEn ? "Latency variation" : "Колебание задержки";
                break;

            case MetricModuleId.PacketLoss:
                valBlock.Text = $"{vm.PingStats.PacketLossPercent}";
                unitBlock.Text = "%";
                subBlock.Text = isEn ? $"{vm.PingStats.TotalPingsSent} packets" : $"{vm.PingStats.TotalPingsSent} пакетов";
                break;

            case MetricModuleId.Download:
                if (vm.SpeedState.DownloadFinalMbps > 0)
                {
                    valBlock.Text = $"{vm.SpeedState.DownloadFinalMbps:F1}";
                    unitBlock.Text = mbpsUnit;
                    subBlock.Text = isEn
                        ? $"Peak: {vm.SpeedState.DownloadResult.PeakMbps:F1} {mbpsUnit}"
                        : $"Пик: {vm.SpeedState.DownloadResult.PeakMbps:F1} {mbpsUnit}";
                }
                else if (vm.SpeedState.Phase == TestPhase.Downloading)
                {
                    valBlock.Text = $"{vm.SpeedState.CurrentMbps:F1}";
                    unitBlock.Text = mbpsUnit;
                    subBlock.Text = isEn ? "Testing..." : "Тестирование...";
                }
                else
                {
                    valBlock.Text = "—";
                    unitBlock.Text = "";
                    subBlock.Text = isEn ? "Ready" : "Не тестировалось";
                }
                break;

            case MetricModuleId.Upload:
                if (vm.SpeedState.UploadFinalMbps > 0)
                {
                    valBlock.Text = $"{vm.SpeedState.UploadFinalMbps:F1}";
                    unitBlock.Text = mbpsUnit;
                    subBlock.Text = isEn
                        ? $"Peak: {vm.SpeedState.UploadResult.PeakMbps:F1} {mbpsUnit}"
                        : $"Пик: {vm.SpeedState.UploadResult.PeakMbps:F1} {mbpsUnit}";
                }
                else if (vm.SpeedState.Phase == TestPhase.Uploading)
                {
                    valBlock.Text = $"{vm.SpeedState.CurrentMbps:F1}";
                    unitBlock.Text = mbpsUnit;
                    subBlock.Text = isEn ? "Testing..." : "Тестирование...";
                }
                else
                {
                    valBlock.Text = "—";
                    unitBlock.Text = "";
                    subBlock.Text = isEn ? "Ready" : "Не тестировалось";
                }
                break;

            case MetricModuleId.GeoIp:
                string ip = vm.PublicIp?.Ip ?? "—";
                valBlock.Text = ip;
                valBlock.FontSize = ip.Length > 15 ? 11.5 : (ip.Length > 11 ? 13.5 : 15.5);
                unitBlock.Text = "";
                subBlock.Text = vm.PublicIp != null
                    ? $"{vm.PublicIp.FlagEmoji} {vm.PublicIp.GetCountryDisplayName(isEn)} • {vm.PublicIp.Isp}"
                    : (isEn ? "Tap to view full IP" : "Нажмите для просмотра");
                break;

            case MetricModuleId.ConnectionType:
                valBlock.Text = vm.NetworkInfo.GetDisplayName(isEn);
                valBlock.FontSize = 13.5;
                unitBlock.Text = "";
                subBlock.Text = vm.NetworkInfo.LinkDownstreamSpeedMbps > 0
                    ? $"{vm.NetworkInfo.LinkDownstreamSpeedMbps} {mbpsUnit} • {vm.NetworkInfo.IpAddress}"
                    : vm.NetworkInfo.IpAddress;
                break;
        }
    }
}
