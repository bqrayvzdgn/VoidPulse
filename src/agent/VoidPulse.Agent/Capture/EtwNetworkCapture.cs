using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace VoidPulse.Agent.Capture;

public class EtwNetworkCapture
{
    private readonly ConnectionTracker _tracker;
    private readonly DnsResolver _dnsResolver;
    private readonly ProcessResolver _processResolver;

    private const string SessionName = "VoidPulseCapture";

    public EtwNetworkCapture(ConnectionTracker tracker, DnsResolver dnsResolver, ProcessResolver processResolver)
    {
        _tracker = tracker;
        _dnsResolver = dnsResolver;
        _processResolver = processResolver;
    }

    public void Start(CancellationToken cancellationToken)
    {
        // Stop any leftover session from a previous crash
        try { TraceEventSession.GetActiveSession(SessionName)?.Stop(); } catch { }

        using var session = new TraceEventSession(SessionName);

        cancellationToken.Register(() =>
        {
            try { session.Stop(); } catch { }
        });

        // Enable kernel network events
        session.EnableKernelProvider(
            KernelTraceEventParser.Keywords.NetworkTCPIP);

        session.Source.Kernel.TcpIpSend += OnTcpSend;
        session.Source.Kernel.TcpIpRecv += OnTcpReceive;
        session.Source.Kernel.TcpIpConnect += OnTcpConnect;
        session.Source.Kernel.TcpIpDisconnect += OnTcpDisconnect;
        session.Source.Kernel.UdpIpSend += OnUdpSend;
        session.Source.Kernel.UdpIpRecv += OnUdpReceive;

        // IPv6 variants
        session.Source.Kernel.TcpIpSendIPV6 += OnTcpSendV6;
        session.Source.Kernel.TcpIpRecvIPV6 += OnTcpReceiveV6;
        session.Source.Kernel.TcpIpConnectIPV6 += OnTcpConnectV6;
        session.Source.Kernel.TcpIpDisconnectIPV6 += OnTcpDisconnectV6;
        session.Source.Kernel.UdpIpSendIPV6 += OnUdpSendV6;
        session.Source.Kernel.UdpIpRecvIPV6 += OnUdpReceiveV6;

        // Enable DNS Client ETW provider for DNS query events
        var dnsProviderGuid = new Guid("1C95126E-7EEA-49A9-A3FE-A378B03DDB4D"); // Microsoft-Windows-DNS-Client
        session.EnableProvider(dnsProviderGuid, TraceEventLevel.Informational);

        // Listen for DNS query completion events (EventID 3008 = QueryCompleted)
        session.Source.Dynamic.All += OnDnsEvent;

        // This blocks until session.Stop() is called
        session.Source.Process();
    }

    // TCP IPv4
    private void OnTcpSend(TcpIpSendTraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordSend(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            "TCP", data.size, data.ProcessID);
        SetProcessName(data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, "TCP", procName);
    }

    private void OnTcpReceive(TcpIpTraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordReceive(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            "TCP", data.size, data.ProcessID);
        SetProcessName(data.daddr.ToString(), data.dport, data.saddr.ToString(), data.sport, "TCP", procName);
    }

    private void OnTcpConnect(TcpIpConnectTraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordConnect(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            data.ProcessID);
        SetProcessName(data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, "TCP", procName);
    }

    private void OnTcpDisconnect(TcpIpTraceData data)
    {
        _tracker.RecordDisconnect(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport);
    }

    // UDP IPv4
    private void OnUdpSend(UdpIpTraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordSend(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            "UDP", data.size, data.ProcessID);
        SetProcessName(data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, "UDP", procName);
    }

    private void OnUdpReceive(UdpIpTraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordReceive(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            "UDP", data.size, data.ProcessID);
        SetProcessName(data.daddr.ToString(), data.dport, data.saddr.ToString(), data.sport, "UDP", procName);
    }

    // TCP IPv6
    private void OnTcpSendV6(TcpIpV6SendTraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordSend(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            "TCP", data.size, data.ProcessID);
        SetProcessName(data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, "TCP", procName);
    }

    private void OnTcpReceiveV6(TcpIpV6TraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordReceive(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            "TCP", data.size, data.ProcessID);
        SetProcessName(data.daddr.ToString(), data.dport, data.saddr.ToString(), data.sport, "TCP", procName);
    }

    private void OnTcpConnectV6(TcpIpV6ConnectTraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordConnect(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            data.ProcessID);
        SetProcessName(data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, "TCP", procName);
    }

    private void OnTcpDisconnectV6(TcpIpV6TraceData data)
    {
        _tracker.RecordDisconnect(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport);
    }

    // UDP IPv6 — reuses UdpIpTraceData type
    private void OnUdpSendV6(UpdIpV6TraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordSend(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            "UDP", data.size, data.ProcessID);
        SetProcessName(data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, "UDP", procName);
    }

    private void OnUdpReceiveV6(UpdIpV6TraceData data)
    {
        var procName = _processResolver.Resolve(data.ProcessID);
        _tracker.RecordReceive(
            data.saddr.ToString(), data.sport,
            data.daddr.ToString(), data.dport,
            "UDP", data.size, data.ProcessID);
        SetProcessName(data.daddr.ToString(), data.dport, data.saddr.ToString(), data.sport, "UDP", procName);
    }

    private void OnDnsEvent(TraceEvent data)
    {
        // EventID 3008 = DNS query completed
        if ((int)data.ID != 3008) return;

        try
        {
            var queryName = data.PayloadStringByName("QueryName");
            var queryResults = data.PayloadStringByName("QueryResults");

            if (string.IsNullOrEmpty(queryName) || string.IsNullOrEmpty(queryResults))
                return;

            // QueryResults can contain multiple IPs separated by ";"
            var ips = queryResults.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var ip in ips)
            {
                var trimmed = ip.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.Contains(':') || System.Net.IPAddress.TryParse(trimmed, out _))
                {
                    _dnsResolver.AddResolution(trimmed, queryName, "A");
                }
            }
        }
        catch
        {
            // Best effort — some events may not have the expected payload
        }
    }

    private void SetProcessName(string srcIp, int srcPort, string dstIp, int dstPort, string protocol, string? processName)
    {
        if (string.IsNullOrEmpty(processName)) return;
        var key = $"{srcIp}:{srcPort}->{dstIp}:{dstPort}/{protocol}";
        _tracker.EnrichProcessName(key, processName);
    }
}
