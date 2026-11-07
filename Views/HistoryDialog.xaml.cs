using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NetPulse.Models;
using NetPulse.ViewModels;

namespace NetPulse.Views;

public partial class HistoryDialog : UserControl
{
    public class HistoryItemViewModel
    {
        public TestRecord Record { get; set; } = new();
        public string FormattedDate { get; set; } = "";
    }

    public HistoryDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateContent();
    }

    public void UpdateContent()
    {
        if (DataContext is not MainViewModel vm) return;

        bool isEn = vm.IsEn;
        TitleText.Text = isEn ? "Test History" : "История проверок";
        ClearAllLabel.Text = isEn ? "Clear All" : "Очистить всё";
        CloseBtn.Content = isEn ? "Close" : "Закрыть";
        EmptyTitle.Text = isEn ? "No test history yet" : "История пока пуста";
        EmptyDesc.Text = isEn ? "Run a speed test to save results here" : "Запустите тест скорости, чтобы сохранить результаты";

        var list = vm.HistoryRecords.ToList();
        if (list.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            ListScrollViewer.Visibility = Visibility.Collapsed;
            ClearAllBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            ListScrollViewer.Visibility = Visibility.Visible;
            ClearAllBtn.Visibility = Visibility.Visible;

            var items = list.Select(r =>
            {
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(r.Timestamp).ToLocalTime();
                string dateStr = isEn
                    ? dt.ToString("MMM dd, HH:mm", CultureInfo.InvariantCulture)
                    : dt.ToString("dd MMM, HH:mm", new CultureInfo("ru-RU"));

                return new HistoryItemViewModel
                {
                    Record = r,
                    FormattedDate = dateStr
                };
            }).ToList();

            RecordsItemsControl.ItemsSource = items;
        }
    }

    private void OnClearAllClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ClearAllHistoryCommand.Execute(null);
            UpdateContent();
        }
    }

    private void OnDeleteRecordClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TestRecord record } && DataContext is MainViewModel vm)
        {
            vm.DeleteHistoryRecord(record);
            UpdateContent();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseHistoryCommand.Execute(null);
        }
    }
}
