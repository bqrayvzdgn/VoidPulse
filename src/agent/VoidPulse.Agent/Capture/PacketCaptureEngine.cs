using System.Collections.Concurrent;
using SharpPcap;
using PacketDotNet;

namespace VoidPulse.Agent.Capture;

public class PacketCaptureEngine : IDisposable
{
    private readonly ConcurrentQueue<PacketRecord> _buffer = new();
    private readonly int _maxBufferSize;
    private readonly int _captureSnapLen;
    private readonly DnsResolver? _dnsResolver;
    private ICaptureDevice? _device;
    private int _packetCount;

    public PacketCaptureEngine(int maxBufferSize = 10000, int captureSnapLen = 128, DnsResolver? dnsResolver = null)
    {
        _maxBufferSize = maxBufferSize;
        _captureSnapLen = captureSnapLen;
        _dnsResolver = dnsResolver;
    }

    public bool IsAvailable
    {
        get
        {
            try
            {
                return CaptureDeviceList.Instance.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public string? DeviceName => _device?.Description ?? _device?.Name;

    public void Start(CancellationToken ct, string? deviceName = null)
    {
        ICaptureDevice? device = null;

        if (!string.IsNullOrEmpty(deviceName))
        {
            device = CaptureDeviceList.Instance.FirstOrDefault(d =>
                d.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase) ||
                (d.Description?.Contains(deviceName, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        device ??= CaptureDeviceList.Instance
            .Where(d => d.Description != null && !d.Description.Contains("Loopback", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault() ?? CaptureDeviceList.Instance.FirstOrDefault();

        if (device is null)
            throw new InvalidOperationException("No capture device found. Is Npcap installed?");

        _device = device;

        device.OnPacketArrival += OnPacketArrival;
        device.Open(DeviceModes.MaxResponsiveness, 1000);
        device.Filter = "ip";
        device.StartCapture();

        ct.Register(() =>
        {
            try
            {
                device.StopCapture();
                device.Close();
            }
            catch { }
        });
    }

    private void OnPacketArrival(object sender, PacketCapture e)
    {
        try
        {
            var rawCapture = e.GetPacket();
            if (rawCapture?.Data is null) return;

            var (protocolStack, info) = ProtocolDissector.Dissect(rawCapture);

            // Extract IPs and ports from dissected layers
            var srcIp = "";
            var dstIp = "";
            var srcPort = 0;
            var dstPort = 0;
            var protocol = "OTHER";

            foreach (var layer in protocolStack)
            {
                switch (layer.Name)
                {
                    case "IPv4" or "IPv6":
                        srcIp = layer.Fields.GetValueOrDefault("Source", "");
                        dstIp = layer.Fields.GetValueOrDefault("Destination", "");
                        break;
                    case "TCP":
                        protocol = "TCP";
                        int.TryParse(layer.Fields.GetValueOrDefault("Source Port", "0"), out srcPort);
                        int.TryParse(layer.Fields.GetValueOrDefault("Destination Port", "0"), out dstPort);
                        break;
                    case "UDP":
                        protocol = "UDP";
                        int.TryParse(layer.Fields.GetValueOrDefault("Source Port", "0"), out srcPort);
                        int.TryParse(layer.Fields.GetValueOrDefault("Destination Port", "0"), out dstPort);
                        break;
                    case "ICMP":
                        protocol = "ICMP";
                        break;
                }
            }

            // Feed DNS answers into resolver
            if (_dnsResolver is not null && srcPort == 53 && protocol == "UDP")
            {
                var dnsAnswers = ProtocolDissector.ExtractDnsAnswers(protocolStack);
                foreach (var answer in dnsAnswers)
                {
                    _dnsResolver.AddResolution(answer.Ip, answer.Hostname, "A");
                }
            }

            // Capture only first N bytes for header storage
            var headerBytes = new byte[Math.Min(rawCapture.Data.Length, _captureSnapLen)];
            Buffer.BlockCopy(rawCapture.Data, 0, headerBytes, 0, headerBytes.Length);

            var record = new PacketRecord
            {
                Timestamp = rawCapture.Timeval.Date,
                SourceIp = srcIp,
                DestinationIp = dstIp,
                SourcePort = srcPort,
                DestinationPort = dstPort,
                Protocol = protocol,
                Length = rawCapture.Data.Length,
                HeaderBytes = headerBytes,
                ProtocolStack = protocolStack,
                Info = info
            };

            // Ring buffer: evict oldest if full
            while (_buffer.Count >= _maxBufferSize)
                _buffer.TryDequeue(out _);

            _buffer.Enqueue(record);
            Interlocked.Increment(ref _packetCount);
        }
        catch
        {
            // Best effort — don't crash the capture loop
        }
    }

    public List<PacketRecord> FlushPackets()
    {
        var packets = new List<PacketRecord>();
        while (_buffer.TryDequeue(out var packet))
        {
            packets.Add(packet);
        }
        return packets;
    }

    public int TotalCaptured => _packetCount;

    public void Dispose()
    {
        try
        {
            _device?.StopCapture();
            _device?.Close();
        }
        catch { }
    }
}
