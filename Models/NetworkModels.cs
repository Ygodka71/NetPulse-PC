using System;
using System.Collections.Generic;
using System.Globalization;

namespace NetPulse.Models;

#region Enums
public enum ConnectionType
{
    WiFi,
    Cellular,
    Ethernet,
    None
}

public enum TestPhase
{
    Idle,
    Pinging,
    Downloading,
    Uploading,
    Completed
}

public enum GradeType
{
    Excellent,
    Great,
    Good,
    Basic,
    Unstable
}

public enum MetricModuleId
{
    CurrentPing,
    AvgLatency,
    Jitter,
    PacketLoss,
    Download,
    Upload,
    GeoIp,
    ConnectionType
}

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public enum AppLanguage
{
    System,
    Ru,
    En
}
#endregion

#region Network State & Targets
public class NetworkInfoState
{
    public ConnectionType ConnectionType { get; set; } = ConnectionType.None;
    public bool IsConnected { get; set; } = false;
    public bool IsInternetValidated { get; set; } = false;
    public string IpAddress { get; set; } = "—";
    public int LinkDownstreamSpeedMbps { get; set; } = 0;
    public string NetworkName { get; set; } = "Нет сети";

    public string GetDisplayName(bool isEn)
    {
        return ConnectionType switch
        {
            ConnectionType.WiFi => isEn ? "Wi-Fi" : "Wi-Fi сеть",
            ConnectionType.Cellular => isEn ? "Cellular" : "Мобильная сеть",
            ConnectionType.Ethernet => "Ethernet",
            _ => isEn ? "Offline" : "Не подключено"
        };
    }
}

public class PingTarget
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 53;
    public string HttpFallbackUrl { get; set; } = "https://1.1.1.1/cdn-cgi/trace";
    public bool IsCustom { get; set; } = false;

    public static readonly PingTarget Cloudflare = new()
    {
        Id = "cf",
        Name = "Cloudflare Edge",
        Host = "1.1.1.1",
        Port = 53,
        HttpFallbackUrl = "https://1.1.1.1/cdn-cgi/trace",
        IsCustom = false
    };

    public static readonly PingTarget Google = new()
    {
        Id = "google",
        Name = "Google DNS",
        Host = "8.8.8.8",
        Port = 53,
        HttpFallbackUrl = "https://www.google.com/generate_204",
        IsCustom = false
    };

    public static readonly PingTarget Quad9 = new()
    {
        Id = "quad9",
        Name = "Quad9 Secure",
        Host = "9.9.9.9",
        Port = 53,
        HttpFallbackUrl = "https://dns.quad9.net/dns-query",
        IsCustom = false
    };

    public static List<PingTarget> DefaultTargets => [Cloudflare, Google, Quad9];
}

public class PingStatistics
{
    public long CurrentPingMs { get; set; } = 0;
    public long AvgLatencyMs { get; set; } = 0;
    public long MinLatencyMs { get; set; } = 0;
    public long MaxLatencyMs { get; set; } = 0;
    public long JitterMs { get; set; } = 0;
    public int PacketLossPercent { get; set; } = 0;
    public List<float> HistorySamples { get; set; } = [];
    public int TotalPingsSent { get; set; } = 0;
}

public class SpeedMeasurementResult
{
    public double FinalAvgMbps { get; set; } = 0.0;
    public double PeakMbps { get; set; } = 0.0;
    public double MinMbps { get; set; } = 0.0;
}

public class SpeedState
{
    public TestPhase Phase { get; set; } = TestPhase.Idle;
    public double CurrentMbps { get; set; } = 0.0;
    public double PeakMbps { get; set; } = 0.0;
    public SpeedMeasurementResult DownloadResult { get; set; } = new();
    public SpeedMeasurementResult UploadResult { get; set; } = new();
    public double DownloadFinalMbps { get; set; } = 0.0;
    public double UploadFinalMbps { get; set; } = 0.0;
    public float ProgressPercent { get; set; } = 0f;
    public string StatusText { get; set; } = "";
    public long CompletedTimestamp { get; set; } = 0L;
}

public class NetworkQualityGrade
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public int ScoreOutOf100 { get; set; } = 0;
    public uint ColorHex { get; set; } = 0xFF10B981;
    public GradeType GradeType { get; set; } = GradeType.Excellent;
}

public class MetricModuleItem
{
    public MetricModuleId Id { get; set; }
    public bool IsVisible { get; set; } = true;

    public MetricModuleItem() { }

    public MetricModuleItem(MetricModuleId id, bool isVisible = true)
    {
        Id = id;
        IsVisible = isVisible;
    }
}

public class MetricHelpInfo
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImpactText { get; set; } = "";
    public string GoodRange { get; set; } = "";
}
#endregion

#region Public IP & Geolocation
public class PublicIpInfo
{
    public string Ip { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string CountryName { get; set; } = "";
    public string FlagEmoji { get; set; } = "🌐";
    public string Isp { get; set; } = "";

    public string GetCountryDisplayName(bool isEn)
    {
        var code = CountryCode?.Trim().ToUpperInvariant() ?? "";
        if (string.IsNullOrEmpty(code) || code == "XX")
        {
            return CountryName;
        }

        if (isEn)
        {
            try
            {
                var region = new RegionInfo(code);
                return !string.IsNullOrWhiteSpace(region.EnglishName) ? region.EnglishName : CountryName;
            }
            catch
            {
                return CountryName;
            }
        }

        return GetCountryNameRu(code);
    }

    public static string CountryCodeToFlagEmoji(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode) || countryCode.Length != 2) return "🌐";

        char first = char.ToUpperInvariant(countryCode[0]);
        char second = char.ToUpperInvariant(countryCode[1]);
        if (first < 'A' || first > 'Z' || second < 'A' || second > 'Z') return "🌐";

        int firstUnicode = 0x1F1E6 + (first - 'A');
        int secondUnicode = 0x1F1E6 + (second - 'A');
        return char.ConvertFromUtf32(firstUnicode) + char.ConvertFromUtf32(secondUnicode);
    }

    public static string GetCountryNameRu(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return "Неизвестно";

        return countryCode.ToUpperInvariant() switch
        {
            "RU" => "Россия",
            "KZ" => "Казахстан",
            "BY" => "Беларусь",
            "UA" => "Украина",
            "UZ" => "Узбекистан",
            "KG" => "Кыргызстан",
            "AM" => "Армения",
            "GE" => "Грузия",
            "AZ" => "Азербайджан",
            "US" => "США",
            "DE" => "Германия",
            "FR" => "Франция",
            "GB" or "UK" => "Великобритания",
            "NL" => "Нидерланды",
            "FI" => "Финляндия",
            "SE" => "Швеция",
            "TR" => "Турция",
            "AE" => "ОАЭ",
            "CN" => "Китай",
            "JP" => "Япония",
            "KR" => "Южная Корея",
            "PL" => "Польша",
            "IT" => "Италия",
            "ES" => "Испания",
            "CA" => "Канада",
            "CH" => "Швейцария",
            "AT" => "Австрия",
            "CZ" => "Чехия",
            "NO" => "Норвегия",
            "DK" => "Дания",
            "EE" => "Эстония",
            "LV" => "Латвия",
            "LT" => "Литва",
            _ => countryCode.ToUpperInvariant()
        };
    }
}
#endregion

#region Test History Record
public class TestRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public double DownloadMbps { get; set; }
    public double UploadMbps { get; set; }
    public long PingMs { get; set; }
    public long AvgLatencyMs { get; set; }
    public long JitterMs { get; set; }
    public string ConnectionType { get; set; } = "";
    public string RatingTitle { get; set; } = "";
    public string ServerName { get; set; } = "";
}
#endregion
