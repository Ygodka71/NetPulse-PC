using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NetPulse.I18n;
using NetPulse.Models;

namespace NetPulse.Controls;

#region PulseWaveformControl
public class PulseWaveformControl : FrameworkElement
{
    public static readonly DependencyProperty SamplesProperty =
        DependencyProperty.Register(nameof(Samples), typeof(List<float>), typeof(PulseWaveformControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CurrentPingMsProperty =
        DependencyProperty.Register(nameof(CurrentPingMs), typeof(long), typeof(PulseWaveformControl),
            new FrameworkPropertyMetadata(0L, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AvgLatencyMsProperty =
        DependencyProperty.Register(nameof(AvgLatencyMs), typeof(long), typeof(PulseWaveformControl),
            new FrameworkPropertyMetadata(0L, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsLiveProperty =
        DependencyProperty.Register(nameof(IsLive), typeof(bool), typeof(PulseWaveformControl),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsEnProperty =
        DependencyProperty.Register(nameof(IsEn), typeof(bool), typeof(PulseWaveformControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public List<float>? Samples
    {
        get => (List<float>?)GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public long CurrentPingMs
    {
        get => (long)GetValue(CurrentPingMsProperty);
        set => SetValue(CurrentPingMsProperty, value);
    }

    public long AvgLatencyMs
    {
        get => (long)GetValue(AvgLatencyMsProperty);
        set => SetValue(AvgLatencyMsProperty, value);
    }

    public bool IsLive
    {
        get => (bool)GetValue(IsLiveProperty);
        set => SetValue(IsLiveProperty, value);
    }

    public bool IsEn
    {
        get => (bool)GetValue(IsEnProperty);
        set => SetValue(IsEnProperty, value);
    }

    private double _pulseAlpha = 1.0;
    private bool _pulseIncreasing = false;
    private readonly System.Windows.Threading.DispatcherTimer _pulseTimer;

    public PulseWaveformControl()
    {
        Height = 135;
        Margin = new Thickness(0, 2, 0, 8);
        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;

        _pulseTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _pulseTimer.Tick += (_, _) =>
        {
            if (_pulseIncreasing)
            {
                _pulseAlpha += 0.05;
                if (_pulseAlpha >= 1.0) { _pulseAlpha = 1.0; _pulseIncreasing = false; }
            }
            else
            {
                _pulseAlpha -= 0.05;
                if (_pulseAlpha <= 0.35) { _pulseAlpha = 0.35; _pulseIncreasing = true; }
            }
            InvalidateVisual();
        };
        _pulseTimer.Start();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth > 0 ? ActualWidth : 480;
        double h = ActualHeight > 0 ? ActualHeight : 175;

        // Card Container Background
        var cardRect = new Rect(0, 0, w, h);
        var surfaceBrush = new SolidColorBrush(Color.FromRgb(0x13, 0x1B, 0x2B));
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0x24, 0x32, 0x4D)), 1);
        dc.DrawRoundedRectangle(surfaceBrush, borderPen, cardRect, 18, 18);

        // Header
        double headerY = 16;
        var typeFace = new Typeface(new FontFamily("Segoe UI, Plus Jakarta Sans"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var monoTypeface = new Typeface(new FontFamily("Consolas, Roboto Mono"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        // Pulsing dot
        Color dotColor = IsLive
            ? Color.FromArgb((byte)(_pulseAlpha * 255), 0x10, 0xB9, 0x81)
            : Color.FromRgb(0x64, 0x74, 0x8B);
        dc.DrawEllipse(new SolidColorBrush(dotColor), null, new Point(22, headerY + 7), 4.5, 4.5);

        // Title text
        string titleText = IsLive
            ? (IsEn ? "NETWORK PULSE (LIVE)" : "ПУЛЬС СЕТИ (LIVE)")
            : (IsEn ? "NETWORK PULSE (PAUSED)" : "ПУЛЬС СЕТИ (ПАУЗА)");

        var titleFormatted = new FormattedText(
            titleText,
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeFace,
            11,
            new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            1.0);
        dc.DrawText(titleFormatted, new Point(34, headerY));

        // Avg Latency on the right
        string avgLabel = IsEn ? "Avg: " : "Средн: ";
        string avgVal = $"{AvgLatencyMs} {(IsEn ? "ms" : "мс")}";

        var avgLabelFormatted = new FormattedText(
            avgLabel,
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeFace,
            11,
            new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            1.0);

        var avgValFormatted = new FormattedText(
            avgVal,
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            monoTypeface,
            12,
            new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)),
            1.0);

        double rightOffset = w - 20 - avgValFormatted.Width;
        dc.DrawText(avgValFormatted, new Point(rightOffset, headerY));
        dc.DrawText(avgLabelFormatted, new Point(rightOffset - avgLabelFormatted.Width - 4, headerY));

        // Waveform Graph inner area
        double graphX = 16;
        double graphY = 42;
        double graphW = w - 32;
        double graphH = h - 56;

        var innerRect = new Rect(graphX, graphY, graphW, graphH);
        dc.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromArgb(120, 0x1C, 0x27, 0x3D)),
            new Pen(new SolidColorBrush(Color.FromArgb(40, 0x24, 0x32, 0x4D)), 1),
            innerRect, 10, 10);

        // Grid lines (3 horizontal guide lines)
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), 1);
        for (int i = 1; i <= 3; i++)
        {
            double gy = graphY + (graphH * (i / 4.0));
            dc.DrawLine(gridPen, new Point(graphX + 6, gy), new Point(graphX + graphW - 6, gy));
        }

        // Draw waveform path
        var samples = Samples;
        if (samples == null || samples.Count == 0)
        {
            var baselinePen = new Pen(new SolidColorBrush(Color.FromArgb(70, 0x00, 0xE5, 0xFF)), 2);
            dc.DrawLine(baselinePen, new Point(graphX + 6, graphY + graphH * 0.7), new Point(graphX + graphW - 6, graphY + graphH * 0.7));
            return;
        }

        float maxSample = Math.Max(samples.Max(), 60f) * 1.25f;
        float minSample = 0f;
        float range = Math.Max(maxSample - minSample, 1f);

        var points = new List<Point>();
        for (int i = 0; i < samples.Count; i++)
        {
            double px = samples.Count > 1
                ? graphX + 8 + (i * ((graphW - 16) / (samples.Count - 1)))
                : graphX + graphW / 2.0;

            double norm = Math.Clamp((samples[i] - minSample) / range, 0.0, 1.0);
            double py = graphY + graphH - 8 - (norm * (graphH - 18));
            points.Add(new Point(px, py));
        }

        // Build filled geometry
        var fillFigure = new PathFigure { StartPoint = new Point(points[0].X, graphY + graphH - 4) };
        fillFigure.Segments.Add(new LineSegment(points[0], false));

        for (int i = 1; i < points.Count; i++)
        {
            var pPrev = points[i - 1];
            var pCur = points[i];
            Point control1 = new(pPrev.X + (pCur.X - pPrev.X) / 2.0, pPrev.Y);
            Point control2 = new(pPrev.X + (pCur.X - pPrev.X) / 2.0, pCur.Y);
            fillFigure.Segments.Add(new BezierSegment(control1, control2, pCur, true));
        }

        fillFigure.Segments.Add(new LineSegment(new Point(points[^1].X, graphY + graphH - 4), false));
        fillFigure.IsClosed = true;

        var fillGeo = new PathGeometry();
        fillGeo.Figures.Add(fillFigure);

        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(75, 0x00, 0xE5, 0xFF), 0.0));
        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(5, 0x00, 0xE5, 0xFF), 1.0));
        dc.DrawGeometry(gradientBrush, null, fillGeo);

        // Build stroke path
        var strokeFigure = new PathFigure { StartPoint = points[0] };
        for (int i = 1; i < points.Count; i++)
        {
            var pPrev = points[i - 1];
            var pCur = points[i];
            Point control1 = new(pPrev.X + (pCur.X - pPrev.X) / 2.0, pPrev.Y);
            Point control2 = new(pPrev.X + (pCur.X - pPrev.X) / 2.0, pCur.Y);
            strokeFigure.Segments.Add(new BezierSegment(control1, control2, pCur, true));
        }

        var strokeGeo = new PathGeometry();
        strokeGeo.Figures.Add(strokeFigure);

        var curvePen = new Pen(new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)), 2.2);
        dc.DrawGeometry(null, curvePen, strokeGeo);

        // Highlight latest point
        var lastPt = points[^1];
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)), null, lastPt, 4, 4);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(90, 0x00, 0xE5, 0xFF)), null, lastPt, 8, 8);
    }
}
#endregion

#region SpeedGaugeControl
public class SpeedGaugeControl : FrameworkElement
{
    public static readonly DependencyProperty CurrentMbpsProperty =
        DependencyProperty.Register(nameof(CurrentMbps), typeof(double), typeof(SpeedGaugeControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnCurrentMbpsChanged));

    public static readonly DependencyProperty PeakMbpsProperty =
        DependencyProperty.Register(nameof(PeakMbps), typeof(double), typeof(SpeedGaugeControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PhaseProperty =
        DependencyProperty.Register(nameof(Phase), typeof(TestPhase), typeof(SpeedGaugeControl),
            new FrameworkPropertyMetadata(TestPhase.Idle, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(float), typeof(SpeedGaugeControl),
            new FrameworkPropertyMetadata(0f, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsEnProperty =
        DependencyProperty.Register(nameof(IsEn), typeof(bool), typeof(SpeedGaugeControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public double CurrentMbps
    {
        get => (double)GetValue(CurrentMbpsProperty);
        set => SetValue(CurrentMbpsProperty, value);
    }

    public double PeakMbps
    {
        get => (double)GetValue(PeakMbpsProperty);
        set => SetValue(PeakMbpsProperty, value);
    }

    public TestPhase Phase
    {
        get => (TestPhase)GetValue(PhaseProperty);
        set => SetValue(PhaseProperty, value);
    }

    public float Progress
    {
        get => (float)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsEn
    {
        get => (bool)GetValue(IsEnProperty);
        set => SetValue(IsEnProperty, value);
    }

    private double _animatedMbps = 0.0;
    private readonly System.Windows.Threading.DispatcherTimer _animTimer;

    public SpeedGaugeControl()
    {
        Width = 240;
        Height = 210;

        _animTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        _animTimer.Tick += (_, _) =>
        {
            double diff = CurrentMbps - _animatedMbps;
            if (Math.Abs(diff) > 0.05)
            {
                _animatedMbps += diff * 0.25;
                InvalidateVisual();
            }
            else if (_animatedMbps != CurrentMbps)
            {
                _animatedMbps = CurrentMbps;
                InvalidateVisual();
            }
        };
        _animTimer.Start();
    }

    private static void OnCurrentMbpsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedGaugeControl control && control.Phase == TestPhase.Idle)
        {
            control._animatedMbps = 0;
            control.InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double width = ActualWidth > 0 ? ActualWidth : Width > 0 ? Width : 240;
        double height = ActualHeight > 0 ? ActualHeight : Height;

        // Scale factor relative to the reference size of 240×210
        double scale = Math.Min(width / 240.0, height / 210.0);
        scale = Math.Max(0.6, Math.Min(scale, 2.0)); // clamp

        Point center = new(width / 2.0, height / 2.0 + 6 * scale);
        double radius = Math.Min(width, height) / 2.0 - 18 * scale;

        // Color resolution
        Color activeColor = Phase switch
        {
            TestPhase.Downloading => Color.FromRgb(0x00, 0xE5, 0xFF), // Cyan
            TestPhase.Uploading   => Color.FromRgb(0x10, 0xB9, 0x81),  // Emerald
            TestPhase.Pinging     => Color.FromRgb(0x38, 0xBD, 0xF8),  // Cyan Glow
            TestPhase.Completed   => Color.FromRgb(0x10, 0xB9, 0x81),  // Emerald
            _                     => Color.FromArgb(120, 0x00, 0xE5, 0xFF)
        };

        const double startAngle = 150.0;
        const double totalSweep = 240.0;

        double arcThickness = Math.Max(5, 10 * scale);

        // Draw background inactive arc
        DrawArc(dc, center, radius, startAngle, totalSweep,
            new Pen(new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)), arcThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap   = PenLineCap.Round
            });

        // Compute normalized progress
        double normalizedProgress = Phase switch
        {
            TestPhase.Idle    => 0.0,
            TestPhase.Pinging => Math.Clamp(Progress, 0.0, 1.0),
            _                 => Math.Clamp(_animatedMbps / 150.0, 0.02, 1.0)
        };

        double activeSweep = totalSweep * normalizedProgress;
        if (activeSweep > 1.0)
        {
            var pen = new Pen(new SolidColorBrush(activeColor), arcThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap   = PenLineCap.Round
            };
            DrawArc(dc, center, radius, startAngle, activeSweep, pen);
        }

        // Adaptive font sizes
        double phaseFontSize  = Math.Max(8.5, 10.5 * scale);
        double speedFontSize  = Math.Max(26,  38 * scale);
        double unitFontSize   = Math.Max(9.5, 11.5 * scale);
        double peakFontSize   = Math.Max(8.5, 10 * scale);

        // Center Text: Phase tag
        string phaseText = AppI18n.GetPhaseLabel(Phase, IsEn);
        var typeFace     = new Typeface(new FontFamily("Segoe UI, Plus Jakarta Sans"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var monoTypeface = new Typeface(new FontFamily("Consolas, Roboto Mono"),       FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        var phaseFormatted = new FormattedText(
            phaseText,
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeFace,
            phaseFontSize,
            new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            1.0);
        dc.DrawText(phaseFormatted, new Point(center.X - phaseFormatted.Width / 2.0, center.Y - 50 * scale));

        // Speed number
        string speedFormattedStr = _animatedMbps switch
        {
            >= 100 => $"{_animatedMbps:F0}",
            >= 10  => $"{_animatedMbps:F1}",
            _      => $"{_animatedMbps:F2}"
        };

        var speedFormatted = new FormattedText(
            speedFormattedStr,
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            monoTypeface,
            speedFontSize,
            new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)),
            1.0);
        dc.DrawText(speedFormatted, new Point(center.X - speedFormatted.Width / 2.0, center.Y - 30 * scale));

        // Speed unit
        string unitStr = IsEn ? "Mbps" : "Мбит / с";
        var unitFormatted = new FormattedText(
            unitStr,
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeFace,
            unitFontSize,
            new SolidColorBrush(activeColor),
            1.0);
        dc.DrawText(unitFormatted, new Point(center.X - unitFormatted.Width / 2.0, center.Y + 18 * scale));

        // Peak speed
        if (PeakMbps > 0)
        {
            string peakText = IsEn ? $"Peak: {PeakMbps:F1} Mbps" : $"Пик: {PeakMbps:F1} Мбит/с";
            var peakFormatted = new FormattedText(
                peakText,
                CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                monoTypeface,
                peakFontSize,
                new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                1.0);
            dc.DrawText(peakFormatted, new Point(center.X - peakFormatted.Width / 2.0, center.Y + 36 * scale));
        }
    }

    private static void DrawArc(DrawingContext dc, Point center, double radius, double startAngleDeg, double sweepAngleDeg, Pen pen)
    {
        double startRad = (startAngleDeg * Math.PI) / 180.0;
        double endRad = ((startAngleDeg + sweepAngleDeg) * Math.PI) / 180.0;

        Point startPoint = new(center.X + radius * Math.Cos(startRad), center.Y + radius * Math.Sin(startRad));
        Point endPoint = new(center.X + radius * Math.Cos(endRad), center.Y + radius * Math.Sin(endRad));

        bool isLargeArc = sweepAngleDeg > 180.0;

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        dc.DrawGeometry(null, pen, geometry);
    }
}
#endregion
