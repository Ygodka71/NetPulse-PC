using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetPulse.ViewModels;

namespace NetPulse.Views;

public partial class TestBreakdownDialog : UserControl
{
    public TestBreakdownDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateContent();
    }

    public void UpdateContent()
    {
        if (DataContext is not MainViewModel vm) return;

        bool isEn = vm.IsEn;
        string msUnit = isEn ? "ms" : "мс";
        string mbpsUnit = isEn ? "Mbps" : "Мбит/с";

        TitleText.Text = isEn ? "Speed Test Breakdown" : "Детали теста скорости";
        DownloadSectionTitle.Text = isEn ? "DOWNLOAD" : "СКАЧИВАНИЕ (DOWNLOAD)";
        UploadSectionTitle.Text = isEn ? "UPLOAD" : "ОТДАЧА (UPLOAD)";
        LatencySectionTitle.Text = isEn ? "LATENCY & NETWORK" : "ЗАДЕРЖКА И СЕТЬ";
        ShareLabel.Text = isEn ? "Copy Report" : "Копировать отчёт";
        CloseBtn.Content = isEn ? "Close" : "Закрыть";

        long ts = vm.SpeedState.CompletedTimestamp > 0
            ? vm.SpeedState.CompletedTimestamp
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var dt = DateTimeOffset.FromUnixTimeMilliseconds(ts).ToLocalTime();
        DateText.Text = isEn
            ? dt.ToString("MMMM dd, yyyy, HH:mm", CultureInfo.InvariantCulture)
            : dt.ToString("dd MMMM yyyy, HH:mm", new CultureInfo("ru-RU"));

        // Grade Card
        if (vm.QualityGrade != null)
        {
            var g = vm.QualityGrade;
            byte r = (byte)((g.ColorHex >> 16) & 0xFF);
            byte gr = (byte)((g.ColorHex >> 8) & 0xFF);
            byte b = (byte)(g.ColorHex & 0xFF);
            var color = Color.FromRgb(r, gr, b);

            GradeCard.Background = new SolidColorBrush(Color.FromArgb(30, r, gr, b));
            GradeCard.BorderBrush = new SolidColorBrush(Color.FromArgb(90, r, gr, b));

            GradeTitle.Text = isEn ? $"Result: {g.Title}" : $"Итог: {g.Title}";
            GradeTitle.Foreground = new SolidColorBrush(color);
            GradeSubtitle.Text = g.Subtitle;

            GradeScoreCircle.Background = new SolidColorBrush(Color.FromArgb(50, r, gr, b));
            GradeScoreText.Text = $"{g.ScoreOutOf100}";
            GradeScoreText.Foreground = new SolidColorBrush(color);
        }

        // Speeds
        var dl = vm.SpeedState.DownloadResult;
        DlAvgText.Text = $"{dl.FinalAvgMbps:F1} {mbpsUnit}";
        DlPeakText.Text = $"{dl.PeakMbps:F1} {mbpsUnit}";
        DlMinText.Text = $"{dl.MinMbps:F1} {mbpsUnit}";

        var ul = vm.SpeedState.UploadResult;
        UlAvgText.Text = $"{ul.FinalAvgMbps:F1} {mbpsUnit}";
        UlPeakText.Text = $"{ul.PeakMbps:F1} {mbpsUnit}";
        UlMinText.Text = $"{ul.MinMbps:F1} {mbpsUnit}";

        // Latency
        PingValText.Text = $"{vm.PingStats.CurrentPingMs} {msUnit}";
        JitterValText.Text = $"{vm.PingStats.JitterMs} {msUnit}";
        LossValText.Text = $"{vm.PingStats.PacketLossPercent}%";
        LossValText.Foreground = vm.PingStats.PacketLossPercent > 0
            ? new SolidColorBrush(Color.FromRgb(0xF4, 0x3F, 0x5E))
            : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));

        ServerDetailsText.Text = isEn ? $"Server: {vm.SelectedTarget.Name} ({vm.SelectedTarget.Host})" : $"Сервер: {vm.SelectedTarget.Name} ({vm.SelectedTarget.Host})";
        NetworkDetailsText.Text = isEn ? $"Network: {vm.NetworkInfo.GetDisplayName(isEn)}" : $"Сеть: {vm.NetworkInfo.GetDisplayName(isEn)}";
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseBreakdownCommand.Execute(null);
        }
    }

    private async void OnShareClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            try
            {
                bool isEn = vm.IsEn;
                string mbps = isEn ? "Mbps" : "Мбит/с";
                string ms = isEn ? "ms" : "мс";

                string report = $@"⚡ NetPulse Speed Test Report
--------------------------------------
Result: {vm.QualityGrade?.Title} ({vm.QualityGrade?.ScoreOutOf100}/100)
↓ Download: {vm.SpeedState.DownloadResult.FinalAvgMbps:F1} {mbps} (Peak: {vm.SpeedState.DownloadResult.PeakMbps:F1} {mbps})
↑ Upload: {vm.SpeedState.UploadResult.FinalAvgMbps:F1} {mbps} (Peak: {vm.SpeedState.UploadResult.PeakMbps:F1} {mbps})
Ping: {vm.PingStats.CurrentPingMs} {ms} | Jitter: {vm.PingStats.JitterMs} {ms} | Loss: {vm.PingStats.PacketLossPercent}%
Server: {vm.SelectedTarget.Name}
Network: {vm.NetworkInfo.GetDisplayName(isEn)}
Date: {DateTime.Now:yyyy-MM-dd HH:mm}
Tested with NetPulse for Windows";

                Clipboard.SetText(report);
                ShareLabel.Text = isEn ? "Copied!" : "Скопировано!";
                ShareIcon.Text = "✓ ";
                await Task.Delay(2000);
                ShareLabel.Text = isEn ? "Copy Report" : "Копировать отчёт";
                ShareIcon.Text = "📋 ";
            }
            catch
            {
                // Clipboard exceptions caught
            }
        }
    }
}
