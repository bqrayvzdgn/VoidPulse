using Microsoft.Extensions.Configuration;
using VoidPulse.Agent.Api;
using VoidPulse.Agent.Capture;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("VOIDPULSE_")
    .AddCommandLine(args)
    .Build();

var agentConfig = config.GetSection("Agent");
var apiUrl = agentConfig["ApiUrl"] ?? "http://localhost:8080/api/v1/traffic/ingest/batch";
var dnsApiUrl = agentConfig["DnsApiUrl"] ?? "http://localhost:8080/api/v1/traffic/dns";
var packetApiUrl = agentConfig["PacketApiUrl"] ?? "http://localhost:8080/api/v1/traffic/packets/ingest";
var apiKey = agentConfig["ApiKey"] ?? "";
var flushInterval = int.TryParse(agentConfig["FlushIntervalSeconds"], out var fi) ? fi : 10;
var batchSize = int.TryParse(agentConfig["BatchSize"], out var bs) ? bs : 50;
var idleTimeout = int.TryParse(agentConfig["IdleTimeoutSeconds"], out var it) ? it : 30;
var enablePacketCapture = bool.TryParse(agentConfig["EnablePacketCapture"], out var epc) && epc;
var captureSnapLen = int.TryParse(agentConfig["CaptureSnapLen"], out var csl) ? csl : 128;
var packetBufferSize = int.TryParse(agentConfig["PacketBufferSize"], out var pbs) ? pbs : 10000;
var captureDevice = agentConfig["CaptureDevice"] ?? "";

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Usage: VoidPulse.Agent --Agent:ApiKey \"vp_xxx\" [--Agent:ApiUrl \"http://...\"]");
    Console.WriteLine("  Or set VOIDPULSE_AGENT__APIKEY environment variable.");
    Console.ResetColor();
    return 1;
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== VoidPulse Agent ===");
Console.ResetColor();
Console.WriteLine($"API URL        : {apiUrl}");
Console.WriteLine($"Flush Interval : {flushInterval}s");
Console.WriteLine($"Batch Size     : {batchSize}");
Console.WriteLine($"Idle Timeout   : {idleTimeout}s");
Console.WriteLine($"Packet Capture : {(enablePacketCapture ? "ENABLED" : "disabled")}");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nShutting down...");
};

var client = new VoidPulseClient(apiUrl, dnsApiUrl, packetApiUrl, apiKey, batchSize);
var dnsResolver = new DnsResolver();
var processResolver = new ProcessResolver();
var tracker = new ConnectionTracker(idleTimeout);

// ETW is Windows-only
EtwNetworkCapture? capture = null;
if (OperatingSystem.IsWindows())
{
    capture = new EtwNetworkCapture(tracker, dnsResolver, processResolver);
}

// Linux process resolver
LinuxProcessResolver? linuxProcessResolver = null;
if (OperatingSystem.IsLinux())
{
    linuxProcessResolver = new LinuxProcessResolver();
    Console.WriteLine("Linux process resolver enabled (via /proc/net/tcp)");
}

// Packet capture (opt-in)
PacketCaptureEngine? packetCapture = null;
if (enablePacketCapture)
{
    packetCapture = new PacketCaptureEngine(packetBufferSize, captureSnapLen, dnsResolver);
    if (packetCapture.IsAvailable)
    {
        try
        {
            packetCapture.Start(cts.Token, string.IsNullOrEmpty(captureDevice) ? null : captureDevice);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Packet capture enabled on: {packetCapture.DeviceName}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Packet capture failed to start: {ex.Message}");
            Console.ResetColor();
            packetCapture = null;
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("WARNING: Npcap not found. Packet capture disabled. Install from https://npcap.com");
        Console.ResetColor();
        packetCapture = null;
    }
}

// Start flush loop in background
var flushTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(flushInterval), cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        // Step 1: Flush packets and track as flows on Linux
        List<PacketRecord>? packets = null;
        if (packetCapture is not null)
        {
            packets = packetCapture.FlushPackets();
            if (packets.Count > 0)
            {
                // On Linux (no ETW), generate flows from captured packets
                if (capture is null)
                {
                    foreach (var pkt in packets)
                    {
                        tracker.TrackPacket(pkt);
                    }
                }
            }
        }

        // Step 2: Flush completed flows
        var flows = tracker.FlushCompletedFlows();
        if (flows.Count > 0)
        {
            // Enrich with DNS + TLS SNI fallback + process resolution
            foreach (var flow in flows)
            {
                if (string.IsNullOrEmpty(flow.Hostname))
                    flow.Hostname = dnsResolver.Resolve(flow.DestinationIp);
                // Use TLS SNI as hostname fallback
                if (string.IsNullOrEmpty(flow.Hostname) && !string.IsNullOrEmpty(flow.TlsSni))
                    flow.Hostname = flow.TlsSni;
                // Linux process resolution
                if (string.IsNullOrEmpty(flow.ProcessName) && linuxProcessResolver is not null)
                    flow.ProcessName = linuxProcessResolver.Resolve(flow.SourceIp, flow.SourcePort, flow.DestinationIp, flow.DestinationPort, flow.Protocol);
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Flushing {flows.Count} flows...");
            Console.ResetColor();

            var (sent, failed) = await client.SendFlowsAsync(flows);

            foreach (var flow in flows)
            {
                var label = !string.IsNullOrEmpty(flow.Hostname) ? flow.Hostname : flow.DestinationIp;
                var process = !string.IsNullOrEmpty(flow.ProcessName) ? flow.ProcessName : "?";
                var bytesFmt = FormatBytes(flow.BytesSent + flow.BytesReceived);

                var color = label.ToLowerInvariant() switch
                {
                    var l when l.Contains("google") || l.Contains("youtube") => ConsoleColor.Red,
                    var l when l.Contains("microsoft") || l.Contains("azure") => ConsoleColor.Blue,
                    var l when l.Contains("cloudflare") || l.Contains("cdn") => ConsoleColor.DarkYellow,
                    var l when l.Contains("amazon") || l.Contains("aws") => ConsoleColor.Yellow,
                    var l when l.Contains("discord") || l.Contains("steam") || l.Contains("riot") => ConsoleColor.Green,
                    var l when l.Contains("github") || l.Contains("gitlab") => ConsoleColor.DarkCyan,
                    var l when l.Contains("claude") || l.Contains("anthropic") => ConsoleColor.Magenta,
                    _ => ConsoleColor.White
                };

                Console.ForegroundColor = color;
                Console.WriteLine($"  {flow.Protocol,-5} {label,-50} [{process}] {bytesFmt}");
                Console.ResetColor();
            }

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Failed to send {failed} flows");
                Console.ResetColor();
            }

            // Send DNS resolutions
            var dnsEntries = dnsResolver.FlushNewResolutions();
            if (dnsEntries.Count > 0)
            {
                await client.SendDnsResolutionsAsync(dnsEntries);
            }
        }

        // Step 3: Send packets to backend
        if (packets is not null && packets.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sending {packets.Count} packets (total captured: {packetCapture!.TotalCaptured})");
            Console.ResetColor();

            var (pSent, pFailed) = await client.SendPacketsAsync(packets);
            if (pFailed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Failed to send {pFailed} packets");
                Console.ResetColor();
            }
        }
    }
}, cts.Token);

// Start capture (blocking until cancelled)
if (capture is not null)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Starting ETW network capture... (Ctrl+C to stop)");
    Console.ResetColor();
    Console.WriteLine();

    try
    {
        capture.Start(cts.Token);
    }
    catch (OperationCanceledException) { }
}
else
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Running on Linux — ETW disabled, using packet capture only. (Ctrl+C to stop)");
    Console.ResetColor();
    Console.WriteLine();

    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException) { }
}

// Final flush
var remaining = tracker.FlushAllFlows();
if (remaining.Count > 0)
{
    Console.WriteLine($"Final flush: {remaining.Count} flows");
    await client.SendFlowsAsync(remaining);
}

if (packetCapture is not null)
{
    var remainingPackets = packetCapture.FlushPackets();
    if (remainingPackets.Count > 0)
    {
        Console.WriteLine($"Final packet flush: {remainingPackets.Count} packets");
        await client.SendPacketsAsync(remainingPackets);
    }
    packetCapture.Dispose();
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("VoidPulse Agent stopped.");
Console.ResetColor();

return 0;

static string FormatBytes(long bytes)
{
    return bytes switch
    {
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
