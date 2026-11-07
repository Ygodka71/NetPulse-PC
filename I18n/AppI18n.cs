using System.Globalization;
using NetPulse.Models;

namespace NetPulse.I18n;

public static class AppI18n
{
    public static bool IsEn(AppLanguage language = AppLanguage.System)
    {
        if (language == AppLanguage.En) return true;
        if (language == AppLanguage.Ru) return false;

        var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return code != "ru" && code != "be" && code != "uk" && code != "kk";
    }

    public static string GetMetricTitle(MetricModuleId id, bool isEn)
    {
        if (isEn)
        {
            return id switch
            {
                MetricModuleId.CurrentPing => "Ping",
                MetricModuleId.AvgLatency => "Avg Latency",
                MetricModuleId.Jitter => "Jitter",
                MetricModuleId.PacketLoss => "Packet Loss",
                MetricModuleId.Download => "Download",
                MetricModuleId.Upload => "Upload",
                MetricModuleId.GeoIp => "Public IP",
                MetricModuleId.ConnectionType => "Network Type",
                _ => id.ToString()
            };
        }

        return id switch
        {
            MetricModuleId.CurrentPing => "Пинг",
            MetricModuleId.AvgLatency => "Ср. задержка",
            MetricModuleId.Jitter => "Джиттер",
            MetricModuleId.PacketLoss => "Потери\nпакетов",
            MetricModuleId.Download => "Скачивание",
            MetricModuleId.Upload => "Отдача",
            MetricModuleId.GeoIp => "Внешний IP",
            MetricModuleId.ConnectionType => "Тип сети",
            _ => id.ToString()
        };
    }

    public static MetricHelpInfo? GetMetricHelp(MetricModuleId id, bool isEn)
    {
        if (isEn)
        {
            return id switch
            {
                MetricModuleId.Jitter => new MetricHelpInfo
                {
                    Title = "Jitter",
                    Description = "Variation and instability in latency between consecutive data packets. Indicates how erratic response times are.",
                    ImpactText = "Critical for online competitive gaming (causes micro-stutters and teleportation) and voice calls (audio dropouts, robotic speech).",
                    GoodRange = "• Under 5 ms — Excellent\n• 5–15 ms — Good\n• Over 30 ms — High network instability"
                },
                MetricModuleId.PacketLoss => new MetricHelpInfo
                {
                    Title = "Packet Loss",
                    Description = "Percentage of network packets that failed to reach destination due to buffer overflow, wireless noise, or congestion.",
                    ImpactText = "Even 1–2% loss degrades voice/video call quality and causes noticeable hit registration lag in online gaming.",
                    GoodRange = "• 0% — Normal\n• 1–3% — Moderate RF interference\n• > 5% — Severe channel degradation"
                },
                MetricModuleId.GeoIp => new MetricHelpInfo
                {
                    Title = "Public IP & Country",
                    Description = "Your device's outward-facing network address on the global internet, routed through your ISP's Anycast gateways.",
                    ImpactText = "Verifies whether a VPN or proxy is active and which geographical node processes your internet traffic.",
                    GoodRange = "Should match your physical country and subscribed ISP."
                },
                MetricModuleId.ConnectionType => new MetricHelpInfo
                {
                    Title = "Connection Type & Link Speed",
                    Description = "Physical transmission medium (Wi-Fi, Ethernet) and raw hardware link speed between your PC and router/switch.",
                    ImpactText = "Represents physical hardware link boundary. Real internet throughput cannot exceed this physical capacity.",
                    GoodRange = "Ethernet: 1000 Mbps (1 Gbps)\nWi-Fi 5/6: 300–866+ Mbps\nWi-Fi 2.4 GHz: 72–144 Mbps"
                },
                _ => null
            };
        }

        return id switch
        {
            MetricModuleId.Jitter => new MetricHelpInfo
            {
                Title = "Джиттер (Jitter)",
                Description = "Колебание (нестабильность) времени отклика между последовательными пакетами. Показывает, насколько сильно 'скачет' задержка.",
                ImpactText = "Критичен для онлайн-игр (вызывает микрофризы и телепортацию), а также голосовых звонков (речь заикается, слова выпадают).",
                GoodRange = "• До 5 мс — отлично\n• 5–15 мс — хорошо\n• Свыше 30 мс — высокая нестабильность сети"
            },
            MetricModuleId.PacketLoss => new MetricHelpInfo
            {
                Title = "Потери пакетов (Packet Loss)",
                Description = "Процент сетевых пакетов, которые не дошли до сервера или были отброшены из-за перегрузки или помех.",
                ImpactText = "Даже 1–2% потерь вызывают пропадание фраз при созвонах, дисконнекты в играх и подвисания веб-страниц.",
                GoodRange = "• 0% — норма\n• 1–3% — заметные помехи в радиоэфире\n• > 5% — сильная деградация канала"
            },
            MetricModuleId.GeoIp => new MetricHelpInfo
            {
                Title = "Внешний IP",
                Description = "Уникальный сетевой адрес, под которым ваши устройства видны в глобальном интернете. Определяется через Anycast-маршрутизацию провайдера.",
                ImpactText = "Позволяет проверить, не включен ли VPN или прокси, и через какой узел/страну идет ваш трафик к целевым серверам.",
                GoodRange = "Обычно совпадает с вашей реальной страной и оператором связи."
            },
            MetricModuleId.ConnectionType => new MetricHelpInfo
            {
                Title = "Тип сети",
                Description = "Технология связи (Wi-Fi, Ethernet) и физическая пропускная способность линка между вашим компьютером и роутером/коммутатором.",
                ImpactText = "Показывает ограничение на физическом уровне канала. Реальная скорость интернета не может превышать этот линк.",
                GoodRange = "Ethernet: 1000 Мбит/с (1 Гбит/с)\nWi-Fi 5/6: 300–866+ Мбит/с\nWi-Fi 2.4 ГГц: 72–144 Мбит/с"
            },
            _ => null
        };
    }

    public static string GetGradeTitle(int score, bool isEn)
    {
        if (isEn)
        {
            return score switch
            {
                >= 88 => "Excellent",
                >= 70 => "Good",
                >= 50 => "Fair",
                _ => "Poor"
            };
        }

        return score switch
        {
            >= 88 => "Превосходно",
            >= 70 => "Хорошо",
            >= 50 => "Базовое",
            _ => "Нестабильно"
        };
    }

    public static string GetGradeSubtitle(int score, bool isEn)
    {
        if (isEn)
        {
            return score switch
            {
                >= 88 => "Ideal for competitive gaming and 4K streaming",
                >= 70 => "Great for video calls and HD streaming",
                >= 50 => "Possible latency spikes in real-time gaming",
                _ => "High latency or packet loss detected"
            };
        }

        return score switch
        {
            >= 88 => "Идеально для 4K Ultra HD стриминга и киберспорта",
            >= 70 => "Комфортный просмотр HD видео и стабильная видеосвязь",
            >= 50 => "Возможны задержки при загрузке медиа и звонках",
            _ => "Проверьте сигнал сети или обратитесь к провайдеру"
        };
    }

    public static string GetPhaseLabel(TestPhase phase, bool isEn)
    {
        if (isEn)
        {
            return phase switch
            {
                TestPhase.Idle => "READY TO TEST",
                TestPhase.Pinging => "CALIBRATING PING",
                TestPhase.Downloading => "DOWNLOADING",
                TestPhase.Uploading => "UPLOADING",
                TestPhase.Completed => "RESULT",
                _ => ""
            };
        }

        return phase switch
        {
            TestPhase.Idle => "ГОТОВ К ТЕСТУ",
            TestPhase.Pinging => "ПРОВЕРКА PING",
            TestPhase.Downloading => "СКАЧИВАНИЕ",
            TestPhase.Uploading => "ОТДАЧА",
            TestPhase.Completed => "РЕЗУЛЬТАТ",
            _ => ""
        };
    }

    public static string GetPhaseStatusText(TestPhase phase, bool isEn)
    {
        if (isEn)
        {
            return phase switch
            {
                TestPhase.Idle => "",
                TestPhase.Pinging => "Checking latency and jitter…",
                TestPhase.Downloading => "Testing download speed…",
                TestPhase.Uploading => "Testing upload speed…",
                TestPhase.Completed => "Testing completed",
                _ => ""
            };
        }

        return phase switch
        {
            TestPhase.Idle => "",
            TestPhase.Pinging => "Проверка задержки и джиттера…",
            TestPhase.Downloading => "Тестирование скорости загрузки…",
            TestPhase.Uploading => "Тестирование скорости отдачи…",
            TestPhase.Completed => "Тестирование завершено",
            _ => ""
        };
    }
}
