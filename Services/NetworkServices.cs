using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetPulse.Models;

namespace NetPulse.Services;

#region PingService
public class PingService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    private readonly List<long> _samples = [];
    private readonly object _lock = new();
    private int _totalSent = 0;
    private int _lostCount = 0;

    public void Reset()
    {
        lock (_lock)
        {
            _samples.Clear();
            _totalSent = 0;
            _lostCount = 0;
        }
    }

    public async Task<long?> MeasureSinglePingAsync(PingTarget target, CancellationToken ct = default)
    {
        // Attempt 1: TCP Socket to Port (DNS 53 or 80)
        try
        {
            using var tcpClient = new TcpClient();
            var sw = Stopwatch.StartNew();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(2500);

            var connectTask = tcpClient.ConnectAsync(target.Host, target.Port);
            await connectTask.WaitAsync(cts.Token);
            sw.Stop();

            long durationMs = sw.ElapsedMilliseconds;
            return Math.Max(1L, durationMs);
        }
        catch
        {
            // Attempt 2: Fallback to fast HTTP endpoint
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, target.HttpFallbackUrl);
                var sw = Stopwatch.StartNew();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(2500);

                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                sw.Stop();

                long durationMs = sw.ElapsedMilliseconds;
                return Math.Max(1L, durationMs);
            }
            catch
            {
                return null;
            }
        }
    }

    public PingStatistics RecordPing(long? latencyMs, int maxHistoryWindow = 35)
    {
        lock (_lock)
        {
            _totalSent++;
            if (latencyMs == null)
            {
                _lostCount++;
            }
            else
            {
                _samples.Add(latencyMs.Value);
                if (_samples.Count > maxHistoryWindow)
                {
                    _samples.RemoveAt(0);
                }
            }

            return ComputeStatistics(latencyMs);
        }
    }

    public PingStatistics ResetPacketLoss()
    {
        lock (_lock)
        {
            _totalSent = 0;
            _lostCount = 0;
            return ComputeStatistics(_samples.LastOrDefault());
        }
    }

    private PingStatistics ComputeStatistics(long? currentLatency)
    {
        var validSamples = _samples.ToList();
        long current = currentLatency ?? (validSamples.Count > 0 ? validSamples[^1] : 0L);
        long avg = validSamples.Count > 0 ? (long)Math.Round(validSamples.Average()) : 0L;
        long min = validSamples.Count > 0 ? validSamples.Min() : 0L;
        long max = validSamples.Count > 0 ? validSamples.Max() : 0L;

        // Jitter: Average variation between consecutive samples
        long jitter = 0L;
        if (validSamples.Count >= 2)
        {
            long diffSum = 0L;
            for (int i = 1; i < validSamples.Count; i++)
            {
                diffSum += Math.Abs(validSamples[i] - validSamples[i - 1]);
            }
            jitter = (long)Math.Round((double)diffSum / (validSamples.Count - 1));
        }

        int lossPercent = _totalSent > 0 ? (int)Math.Round(((double)_lostCount / _totalSent) * 100) : 0;
        var floatSamples = validSamples.Select(s => (float)s).ToList();

        return new PingStatistics
        {
            CurrentPingMs = current,
            AvgLatencyMs = avg,
            MinLatencyMs = min,
            MaxLatencyMs = max,
            JitterMs = jitter,
            PacketLossPercent = lossPercent,
            HistorySamples = floatSamples,
            TotalPingsSent = _totalSent
        };
    }
}
#endregion

#region SpeedTestService
public class SpeedTestService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(25)
    };

    private readonly string[] _downloadUrls =
    [
        "https://speed.cloudflare.com/__down?bytes=15000000",
        "https://proof.ovh.net/files/10Mb.dat"
    ];

    private readonly string[] _uploadUrls =
    [
        "https://speed.cloudflare.com/__up",
        "https://httpbin.org/post"
    ];

    public async Task<SpeedMeasurementResult> RunDownloadTestAsync(
        Action<double, double, float> onProgress,
        CancellationToken ct = default)
    {
        var finalResult = new SpeedMeasurementResult();

        foreach (var url in _downloadUrls)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");

                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode) continue;

                long totalLength = response.Content.Headers.ContentLength ?? 15_000_000L;
                using var stream = await response.Content.ReadAsStreamAsync(ct);

                byte[] buffer = new byte[32 * 1024];
                long totalBytesRead = 0;
                double peakSpeed = 0.0;
                double minSpeed = double.MaxValue;

                var overallSw = Stopwatch.StartNew();
                var intervalSw = Stopwatch.StartNew();
                long intervalBytesRead = 0;

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    totalBytesRead += bytesRead;
                    intervalBytesRead += bytesRead;

                    double intervalSec = intervalSw.Elapsed.TotalSeconds;
                    if (intervalSec >= 0.1)
                    {
                        double instantMbps = (intervalBytesRead * 8.0) / (intervalSec * 1_000_000.0);
                        if (instantMbps > peakSpeed) peakSpeed = instantMbps;
                        if (instantMbps > 0.1 && instantMbps < minSpeed) minSpeed = instantMbps;

                        float progress = Math.Clamp((float)totalBytesRead / totalLength, 0f, 1f);
                        onProgress(instantMbps, peakSpeed, progress);

                        intervalSw.Restart();
                        intervalBytesRead = 0;
                    }
                }

                overallSw.Stop();
                double overallSec = overallSw.Elapsed.TotalSeconds;

                if (totalBytesRead > 100_000 && overallSec > 0.3)
                {
                    double avgSpeed = (totalBytesRead * 8.0) / (overallSec * 1_000_000.0);
                    if (minSpeed == double.MaxValue) minSpeed = avgSpeed;

                    onProgress(avgSpeed, Math.Max(peakSpeed, avgSpeed), 1f);
                    return new SpeedMeasurementResult
                    {
                        FinalAvgMbps = avgSpeed,
                        PeakMbps = Math.Max(peakSpeed, avgSpeed),
                        MinMbps = Math.Min(minSpeed, avgSpeed)
                    };
                }
            }
            catch when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Try next download URL
            }
        }

        return finalResult;
    }

    public async Task<SpeedMeasurementResult> RunUploadTestAsync(
        Action<double, double, float> onProgress,
        CancellationToken ct = default)
    {
        var finalResult = new SpeedMeasurementResult();
        const long targetBytes = 6_000_000L; // 6 MB payload

        foreach (var url in _uploadUrls)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var overallSw = Stopwatch.StartNew();
                double peakSpeed = 0.0;
                double minSpeed = double.MaxValue;

                using var payloadContent = new ProgressPushStreamContent(
                    targetBytes,
                    onChunkWritten: (bytesWritten, instantMbps) =>
                    {
                        if (instantMbps > peakSpeed) peakSpeed = instantMbps;
                        if (instantMbps > 0.1 && instantMbps < minSpeed) minSpeed = instantMbps;

                        float progress = Math.Clamp((float)bytesWritten / targetBytes, 0f, 1f);
                        onProgress(instantMbps, peakSpeed, progress);
                    },
                    ct);

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = payloadContent
                };

                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                overallSw.Stop();

                double overallSec = overallSw.Elapsed.TotalSeconds;
                if (overallSec > 0.3)
                {
                    double avgSpeed = (targetBytes * 8.0) / (overallSec * 1_000_000.0);
                    if (minSpeed == double.MaxValue) minSpeed = avgSpeed;

                    onProgress(avgSpeed, Math.Max(peakSpeed, avgSpeed), 1f);
                    return new SpeedMeasurementResult
                    {
                        FinalAvgMbps = avgSpeed,
                        PeakMbps = Math.Max(peakSpeed, avgSpeed),
                        MinMbps = Math.Min(minSpeed, avgSpeed)
                    };
                }
            }
            catch when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Try next upload URL
            }
        }

        return finalResult;
    }

    private class ProgressPushStreamContent : HttpContent
    {
        private readonly long _totalBytes;
        private readonly Action<long, double> _onChunkWritten;
        private readonly CancellationToken _ct;

        public ProgressPushStreamContent(long totalBytes, Action<long, double> onChunkWritten, CancellationToken ct)
        {
            _totalBytes = totalBytes;
            _onChunkWritten = onChunkWritten;
            _ct = ct;
            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            Headers.ContentLength = totalBytes;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            const int chunkSize = 32 * 1024;
            byte[] chunk = new byte[chunkSize];
            Array.Fill(chunk, (byte)0x5A);

            long bytesWritten = 0;
            var intervalSw = Stopwatch.StartNew();
            long intervalBytesWritten = 0;

            while (bytesWritten < _totalBytes)
            {
                _ct.ThrowIfCancellationRequested();

                int toWrite = (int)Math.Min((long)chunkSize, _totalBytes - bytesWritten);
                await stream.WriteAsync(chunk.AsMemory(0, toWrite), _ct);

                bytesWritten += toWrite;
                intervalBytesWritten += toWrite;

                double intervalSec = intervalSw.Elapsed.TotalSeconds;
                if (intervalSec >= 0.1)
                {
                    double instantMbps = (intervalBytesWritten * 8.0) / (intervalSec * 1_000_000.0);
                    _onChunkWritten(bytesWritten, instantMbps);
                    intervalSw.Restart();
                    intervalBytesWritten = 0;
                }
            }

            await stream.FlushAsync(_ct);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _totalBytes;
            return true;
        }
    }
}
#endregion

#region PublicIpService
public class PublicIpService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private PublicIpInfo? _cachedInfo;

    public async Task<PublicIpInfo?> GetPublicIpInfoAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && _cachedInfo != null)
        {
            return _cachedInfo;
        }

        // Strategy 1: Cloudflare trace (fastest, highly reliable)
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://cloudflare.com/cdn-cgi/trace");
            request.Headers.TryAddWithoutValidation("User-Agent", "NetPulse-Windows");

            using var response = await HttpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var bodyText = await response.Content.ReadAsStringAsync(ct);
                string ip = "";
                string loc = "";

                foreach (var line in bodyText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.StartsWith("ip=", StringComparison.OrdinalIgnoreCase))
                    {
                        ip = line["ip=".Length..].Trim();
                    }
                    else if (line.StartsWith("loc=", StringComparison.OrdinalIgnoreCase))
                    {
                        loc = line["loc=".Length..].Trim();
                    }
                }

                if (!string.IsNullOrEmpty(ip))
                {
                    string countryCode = !string.IsNullOrEmpty(loc) ? loc : "XX";
                    string countryName = PublicIpInfo.GetCountryNameRu(countryCode);
                    string flag = PublicIpInfo.CountryCodeToFlagEmoji(countryCode);

                    var info = new PublicIpInfo
                    {
                        Ip = ip,
                        CountryCode = countryCode,
                        CountryName = countryName,
                        FlagEmoji = flag,
                        Isp = "Cloudflare Edge"
                    };
                    _cachedInfo = info;
                    return info;
                }
            }
        }
        catch
        {
            // Continue to fallback
        }

        // Strategy 2: ipwho.is fallback
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://ipwho.is/");
            using var response = await HttpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var jsonStr = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                string ip = root.TryGetProperty("ip", out var ipEl) ? ipEl.GetString() ?? "" : "";
                string countryCode = root.TryGetProperty("country_code", out var ccEl) ? ccEl.GetString() ?? "XX" : "XX";
                string country = root.TryGetProperty("country", out var cEl) ? cEl.GetString() ?? PublicIpInfo.GetCountryNameRu(countryCode) : PublicIpInfo.GetCountryNameRu(countryCode);

                string isp = "";
                if (root.TryGetProperty("connection", out var connEl) && connEl.TryGetProperty("isp", out var ispEl))
                {
                    isp = ispEl.GetString() ?? "";
                }

                if (!string.IsNullOrEmpty(ip))
                {
                    string flag = PublicIpInfo.CountryCodeToFlagEmoji(countryCode);
                    var info = new PublicIpInfo
                    {
                        Ip = ip,
                        CountryCode = countryCode,
                        CountryName = country,
                        FlagEmoji = flag,
                        Isp = isp
                    };
                    _cachedInfo = info;
                    return info;
                }
            }
        }
        catch
        {
            // Ignore error
        }

        return _cachedInfo;
    }
}
#endregion

#region NetworkMonitorService
public class NetworkMonitorService : IDisposable
{
    public event Action<NetworkInfoState>? NetworkStateChanged;

    public NetworkMonitorService()
    {
        NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
        NetworkChange.NetworkAddressChanged += OnNetworkChanged;
    }

    private void OnNetworkChanged(object? sender, EventArgs e)
    {
        NetworkStateChanged?.Invoke(GetCurrentState());
    }

    public NetworkInfoState GetCurrentState()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            var active = interfaces.FirstOrDefault(ni =>
            {
                var props = ni.GetIPProperties();
                return props.GatewayAddresses.Any(g => g.Address != null && !g.Address.Equals(IPAddress.Any));
            }) ?? interfaces.FirstOrDefault();

            if (active == null)
            {
                return new NetworkInfoState
                {
                    ConnectionType = ConnectionType.None,
                    IsConnected = false,
                    IsInternetValidated = false,
                    IpAddress = "—",
                    LinkDownstreamSpeedMbps = 0,
                    NetworkName = "Нет сети"
                };
            }

            var connType = active.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Wireless80211 => ConnectionType.WiFi,
                NetworkInterfaceType.Ethernet => ConnectionType.Ethernet,
                _ => ConnectionType.Ethernet
            };

            int linkSpeedMbps = (int)Math.Max(0, active.Speed / 1_000_000);
            string localIp = GetLocalIp(active) ?? "—";
            string netName = !string.IsNullOrWhiteSpace(active.Name) ? active.Name : active.Description;

            return new NetworkInfoState
            {
                ConnectionType = connType,
                IsConnected = true,
                IsInternetValidated = true,
                IpAddress = localIp,
                LinkDownstreamSpeedMbps = linkSpeedMbps,
                NetworkName = netName
            };
        }
        catch
        {
            return new NetworkInfoState
            {
                ConnectionType = ConnectionType.None,
                IsConnected = false,
                IsInternetValidated = false,
                IpAddress = "—",
                LinkDownstreamSpeedMbps = 0,
                NetworkName = "Нет сети"
            };
        }
    }

    private string? GetLocalIp(NetworkInterface ni)
    {
        try
        {
            var ipProps = ni.GetIPProperties();
            var unicast = ipProps.UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork &&
                                     !IPAddress.IsLoopback(u.Address));
            return unicast?.Address.ToString();
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkChanged;
        NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
        GC.SuppressFinalize(this);
    }
}
#endregion
