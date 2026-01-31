using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoidPulse.Agent.Capture;

namespace VoidPulse.Agent.Api;

public class VoidPulseClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _apiUrl;
    private readonly string _dnsApiUrl;
    private readonly string _packetApiUrl;
    private readonly int _batchSize;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public VoidPulseClient(string apiUrl, string dnsApiUrl, string packetApiUrl, string apiKey, int batchSize)
    {
        _apiUrl = apiUrl;
        _dnsApiUrl = dnsApiUrl;
        _packetApiUrl = packetApiUrl;
        _batchSize = batchSize;

        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<(int sent, int failed)> SendFlowsAsync(List<FlowRecord> flows)
    {
        int sent = 0, failed = 0;

        // Send in batches
        for (int i = 0; i < flows.Count; i += _batchSize)
        {
            var batch = flows.Skip(i).Take(_batchSize).Select(f => new
            {
                sourceIp = f.SourceIp,
                destinationIp = f.DestinationIp,
                sourcePort = f.SourcePort,
                destinationPort = f.DestinationPort,
                protocol = f.Protocol,
                bytesSent = f.BytesSent,
                bytesReceived = f.BytesReceived,
                packetsSent = f.PacketsSent,
                packetsReceived = f.PacketsReceived,
                startedAt = f.StartedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                endedAt = f.LastActivity.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                processName = f.ProcessName,
                hostname = f.Hostname,
                tlsSni = f.TlsSni
            }).ToList();

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var response = await _http.PostAsJsonAsync(_apiUrl, batch, JsonOptions);
                    if (response.IsSuccessStatusCode)
                    {
                        sent += batch.Count;
                        break;
                    }

                    if (attempt == 2)
                        failed += batch.Count;
                    else
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
                catch
                {
                    if (attempt == 2)
                        failed += batch.Count;
                    else
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }
        }

        return (sent, failed);
    }

    public async Task SendDnsResolutionsAsync(List<DnsEntry> entries)
    {
        if (entries.Count == 0) return;

        try
        {
            var payload = entries.Select(e => new
            {
                queriedHostname = e.QueriedHostname,
                resolvedIp = e.ResolvedIp,
                queryType = e.QueryType,
                ttl = e.Ttl,
                resolvedAt = e.ResolvedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToList();

            await _http.PostAsJsonAsync(_dnsApiUrl, payload, JsonOptions);
        }
        catch
        {
            // DNS resolution sending is best-effort
        }
    }

    public async Task<(int sent, int failed)> SendPacketsAsync(List<PacketRecord> packets)
    {
        if (packets.Count == 0) return (0, 0);

        int sent = 0, failed = 0;

        for (int i = 0; i < packets.Count; i += _batchSize)
        {
            var batch = packets.Skip(i).Take(_batchSize).Select(p => new
            {
                capturedAt = p.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                sourceIp = p.SourceIp,
                destinationIp = p.DestinationIp,
                sourcePort = p.SourcePort,
                destinationPort = p.DestinationPort,
                protocol = p.Protocol,
                packetLength = p.Length,
                headerBytesBase64 = Convert.ToBase64String(p.HeaderBytes),
                protocolStackJson = System.Text.Json.JsonSerializer.Serialize(p.ProtocolStack, JsonOptions),
                info = p.Info
            }).ToList();

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var response = await _http.PostAsJsonAsync(_packetApiUrl, batch, JsonOptions);
                    if (response.IsSuccessStatusCode)
                    {
                        sent += batch.Count;
                        break;
                    }

                    if (attempt == 2)
                        failed += batch.Count;
                    else
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
                catch
                {
                    if (attempt == 2)
                        failed += batch.Count;
                    else
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }
        }

        return (sent, failed);
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
