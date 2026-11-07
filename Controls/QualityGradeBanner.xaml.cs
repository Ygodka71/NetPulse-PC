using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NetPulse.Models;
using NetPulse.ViewModels;

namespace NetPulse.Controls;

public partial class QualityGradeBanner : UserControl
{
    public static readonly DependencyProperty GradeProperty =
        DependencyProperty.Register(nameof(Grade), typeof(NetworkQualityGrade), typeof(QualityGradeBanner),
            new FrameworkPropertyMetadata(null, OnGradeChanged));

    public NetworkQualityGrade? Grade
    {
        get => (NetworkQualityGrade?)GetValue(GradeProperty);
        set => SetValue(GradeProperty, value);
    }

    public QualityGradeBanner()
    {
        InitializeComponent();
    }

    private static void OnGradeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QualityGradeBanner banner)
        {
            banner.UpdateVisuals(e.NewValue as NetworkQualityGrade);
        }
    }

    private void UpdateVisuals(NetworkQualityGrade? grade)
    {
        if (grade == null)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        Visibility = Visibility.Visible;
        byte a = (byte)((grade.ColorHex >> 24) & 0xFF);
        byte r = (byte)((grade.ColorHex >> 16) & 0xFF);
        byte g = (byte)((grade.ColorHex >> 8) & 0xFF);
        byte b = (byte)(grade.ColorHex & 0xFF);
        var baseColor = Color.FromRgb(r, g, b);

        BannerBorder.Background = new SolidColorBrush(Color.FromArgb(30, r, g, b));
        BannerBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(90, r, g, b));

        TitleBlock.Text = grade.Title;
        TitleBlock.Foreground = new SolidColorBrush(baseColor);
        SubtitleBlock.Text = grade.Subtitle;

        ScoreCircle.Background = new SolidColorBrush(Color.FromArgb(50, r, g, b));
        ScoreBlock.Text = $"{grade.ScoreOutOf100}";
        ScoreBlock.Foreground = new SolidColorBrush(baseColor);
    }

    private void OnBannerClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OpenBreakdownCommand.Execute(null);
        }
    }
}
