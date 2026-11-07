using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NetPulse.Models;
using NetPulse.ViewModels;

namespace NetPulse.Views;

public partial class HelpDialog : UserControl
{
    public HelpDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateContent();
    }

    public void UpdateContent()
    {
        if (DataContext is not MainViewModel vm) return;

        bool isEn = vm.IsEn;
        var info = vm.ActiveHelpInfo;
        var modId = vm.ActiveHelpModuleId;

        if (info != null)
        {
            DialogTitle.Text = info.Title;
            DescBlock.Text = info.Description;
            ImpactBlock.Text = info.ImpactText;
            GoodRangeBlock.Text = info.GoodRange;

            ImpactHeader.Text = isEn ? "Network Impact" : "Влияние на соединение";
            NormHeader.Text = isEn ? "Expected Range" : "Нормальные значения";
        }

        if (modId == MetricModuleId.GeoIp && vm.PublicIp != null)
        {
            IpCard.Visibility = Visibility.Visible;
            string ip = vm.PublicIp.Ip;
            IpAddressText.Text = ip;
            IpHeaderLabel.Text = isEn ? "YOUR PUBLIC IP ADDRESS" : "ВАШ ПУБЛИЧНЫЙ IP-АДРЕС";
            IpBadgeText.Text = ip.Contains(':') ? "IPv6" : "IPv4";
            CopyIpText.Text = isEn ? "Copy IP" : "Копировать IP";
        }
        else
        {
            IpCard.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseHelpCommand.Execute(null);
        }
    }

    private async void OnCopyIpClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.PublicIp != null)
        {
            try
            {
                Clipboard.SetText(vm.PublicIp.Ip);
                CopyIpText.Text = vm.IsEn ? "Copied!" : "Скопировано!";
                CopyIpIcon.Text = "✓ ";
                await Task.Delay(2000);
                CopyIpText.Text = vm.IsEn ? "Copy IP" : "Копировать IP";
                CopyIpIcon.Text = "📋 ";
            }
            catch
            {
                // Clipboard exceptions safely caught
            }
        }
    }
}
