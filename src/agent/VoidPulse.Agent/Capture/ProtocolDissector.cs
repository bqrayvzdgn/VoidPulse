using PacketDotNet;
using SharpPcap;

namespace VoidPulse.Agent.Capture;

public static class ProtocolDissector
{
    public static (List<ProtocolLayer> Stack, string Info) Dissect(RawCapture rawCapture)
    {
        var stack = new List<ProtocolLayer>();
        var info = "";

        try
        {
            var packet = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
            if (packet is null) return (stack, "Unknown");

            // Ethernet layer
            if (packet is EthernetPacket ethernet)
            {
                stack.Add(new ProtocolLayer
                {
                    Name = "Ethernet",
                    Offset = 0,
                    Length = EthernetFields.HeaderLength,
                    Fields = new Dictionary<string, string>
                    {
                        ["Source MAC"] = ethernet.SourceHardwareAddress?.ToString() ?? "",
                        ["Destination MAC"] = ethernet.DestinationHardwareAddress?.ToString() ?? "",
                        ["Type"] = ethernet.Type.ToString()
                    }
                });
            }

            // IP layer
            var ipPacket = packet.Extract<IPPacket>();
            if (ipPacket is null) return (stack, "Non-IP");

            if (ipPacket is IPv4Packet ipv4)
            {
                stack.Add(new ProtocolLayer
                {
                    Name = "IPv4",
                    Offset = ipv4.ParentPacket != null ? EthernetFields.HeaderLength : 0,
                    Length = ipv4.HeaderLength,
                    Fields = new Dictionary<string, string>
                    {
                        ["Source"] = ipv4.SourceAddress.ToString(),
                        ["Destination"] = ipv4.DestinationAddress.ToString(),
                        ["TTL"] = ipv4.TimeToLive.ToString(),
                        ["Total Length"] = ipv4.TotalLength.ToString(),
                        ["Protocol"] = ipv4.Protocol.ToString(),
                        ["Identification"] = ipv4.Id.ToString(),
                        ["Flags"] = $"DF={((ipv4.FragmentFlags & 0x02) != 0 ? 1 : 0)}, MF={((ipv4.FragmentFlags & 0x01) != 0 ? 1 : 0)}"
                    }
                });
            }
            else if (ipPacket is IPv6Packet ipv6)
            {
                stack.Add(new ProtocolLayer
                {
                    Name = "IPv6",
                    Offset = ipv6.ParentPacket != null ? EthernetFields.HeaderLength : 0,
                    Length = IPv6Fields.HeaderLength,
                    Fields = new Dictionary<string, string>
                    {
                        ["Source"] = ipv6.SourceAddress.ToString(),
                        ["Destination"] = ipv6.DestinationAddress.ToString(),
                        ["Hop Limit"] = ipv6.HopLimit.ToString(),
                        ["Payload Length"] = ipv6.PayloadLength.ToString(),
                        ["Next Header"] = ipv6.NextHeader.ToString()
                    }
                });
            }

            // TCP layer
            var tcpPacket = packet.Extract<TcpPacket>();
            if (tcpPacket is not null)
            {
                var flags = BuildTcpFlags(tcpPacket);
                var tcpOffset = (ipPacket.ParentPacket != null ? EthernetFields.HeaderLength : 0) + ipPacket.HeaderLength;

                stack.Add(new ProtocolLayer
                {
                    Name = "TCP",
                    Offset = tcpOffset,
                    Length = tcpPacket.DataOffset * 4,
                    Fields = new Dictionary<string, string>
                    {
                        ["Source Port"] = tcpPacket.SourcePort.ToString(),
                        ["Destination Port"] = tcpPacket.DestinationPort.ToString(),
                        ["Sequence"] = tcpPacket.SequenceNumber.ToString(),
                        ["Acknowledgment"] = tcpPacket.AcknowledgmentNumber.ToString(),
                        ["Flags"] = flags,
                        ["Window Size"] = tcpPacket.WindowSize.ToString()
                    }
                });

                info = $"{tcpPacket.SourcePort} → {tcpPacket.DestinationPort} [{flags}] Seq={tcpPacket.SequenceNumber} Win={tcpPacket.WindowSize} Len={tcpPacket.PayloadData?.Length ?? 0}";

                // Try to detect application protocol from payload
                var payload = tcpPacket.PayloadData;
                if (payload is { Length: > 0 })
                {
                    var appLayer = DetectApplicationProtocol(payload, tcpPacket.SourcePort, tcpPacket.DestinationPort, tcpOffset + tcpPacket.DataOffset * 4);
                    if (appLayer is not null)
                    {
                        stack.Add(appLayer);
                        info = appLayer.Name switch
                        {
                            "TLS" => $"TLS {appLayer.Fields.GetValueOrDefault("Type", "")} {appLayer.Fields.GetValueOrDefault("Version", "")} {(appLayer.Fields.ContainsKey("SNI") ? $"SNI={appLayer.Fields["SNI"]}" : "")}".Trim(),
                            "HTTP" => $"HTTP {appLayer.Fields.GetValueOrDefault("Method", "")} {appLayer.Fields.GetValueOrDefault("Path", "")}",
                            _ => info
                        };
                    }
                }

                return (stack, info);
            }

            // UDP layer
            var udpPacket = packet.Extract<UdpPacket>();
            if (udpPacket is not null)
            {
                var udpOffset = (ipPacket.ParentPacket != null ? EthernetFields.HeaderLength : 0) + ipPacket.HeaderLength;

                stack.Add(new ProtocolLayer
                {
                    Name = "UDP",
                    Offset = udpOffset,
                    Length = UdpFields.HeaderLength,
                    Fields = new Dictionary<string, string>
                    {
                        ["Source Port"] = udpPacket.SourcePort.ToString(),
                        ["Destination Port"] = udpPacket.DestinationPort.ToString(),
                        ["Length"] = udpPacket.Length.ToString()
                    }
                });

                info = $"UDP {udpPacket.SourcePort} → {udpPacket.DestinationPort} Len={udpPacket.PayloadData?.Length ?? 0}";

                // Check for DNS
                if (udpPacket.SourcePort == 53 || udpPacket.DestinationPort == 53)
                {
                    var isResponse = udpPacket.SourcePort == 53;
                    var dnsFields = new Dictionary<string, string>
                    {
                        ["Type"] = isResponse ? "Response" : "Query"
                    };

                    // Parse DNS payload to extract query name and answers
                    if (udpPacket.PayloadData is { Length: >= 12 })
                    {
                        var dnsData = udpPacket.PayloadData;
                        var parsed = ParseDnsPayload(dnsData, isResponse);
                        if (parsed.QueryName is not null)
                            dnsFields["Query"] = parsed.QueryName;
                        if (parsed.Answers.Count > 0)
                            dnsFields["Answers"] = string.Join(", ", parsed.Answers.Select(a => $"{a.Name}={a.Ip}"));

                        info = isResponse
                            ? $"DNS Response: {parsed.QueryName ?? "?"} → {string.Join(", ", parsed.Answers.Select(a => a.Ip))}"
                            : $"DNS Query: {parsed.QueryName ?? "?"}";
                    }
                    else
                    {
                        info = $"DNS {(isResponse ? "Response" : "Query")} {udpPacket.SourcePort} → {udpPacket.DestinationPort}";
                    }

                    stack.Add(new ProtocolLayer
                    {
                        Name = "DNS",
                        Offset = udpOffset + UdpFields.HeaderLength,
                        Length = udpPacket.PayloadData?.Length ?? 0,
                        Fields = dnsFields
                    });
                }

                return (stack, info);
            }

            // ICMP
            var icmpPacket = packet.Extract<IcmpV4Packet>();
            if (icmpPacket is not null)
            {
                stack.Add(new ProtocolLayer
                {
                    Name = "ICMP",
                    Offset = (ipPacket.ParentPacket != null ? EthernetFields.HeaderLength : 0) + ipPacket.HeaderLength,
                    Length = 8,
                    Fields = new Dictionary<string, string>
                    {
                        ["Type"] = icmpPacket.TypeCode.ToString()
                    }
                });
                info = $"ICMP {icmpPacket.TypeCode}";
                return (stack, info);
            }

            info = ipPacket.Protocol.ToString();
        }
        catch
        {
            info = "Parse error";
        }

        return (stack, info);
    }

    private static string BuildTcpFlags(TcpPacket tcp)
    {
        var flags = new List<string>();
        if (tcp.Synchronize) flags.Add("SYN");
        if (tcp.Acknowledgment) flags.Add("ACK");
        if (tcp.Finished) flags.Add("FIN");
        if (tcp.Reset) flags.Add("RST");
        if (tcp.Push) flags.Add("PSH");
        if (tcp.Urgent) flags.Add("URG");
        return string.Join(", ", flags);
    }

    private static ProtocolLayer? DetectApplicationProtocol(byte[] payload, int srcPort, int dstPort, int offset)
    {
        if (payload.Length < 2) return null;

        // TLS detection: ContentType 0x16 = Handshake
        if (payload[0] == 0x16 && payload.Length >= 6)
        {
            var tlsVersion = $"{payload[1]}.{payload[2]}";
            var versionName = (payload[1], payload[2]) switch
            {
                (3, 1) => "TLS 1.0",
                (3, 2) => "TLS 1.1",
                (3, 3) => "TLS 1.2/1.3",
                _ => $"TLS {tlsVersion}"
            };

            var fields = new Dictionary<string, string>
            {
                ["Version"] = versionName
            };

            // Check for ClientHello (handshake type 0x01)
            if (payload.Length >= 6 && payload[5] == 0x01)
            {
                fields["Type"] = "ClientHello";
                var sni = ExtractTlsSni(payload);
                if (sni is not null)
                    fields["SNI"] = sni;
            }
            else if (payload.Length >= 6 && payload[5] == 0x02)
            {
                fields["Type"] = "ServerHello";
            }
            else
            {
                fields["Type"] = "Handshake";
            }

            return new ProtocolLayer
            {
                Name = "TLS",
                Offset = offset,
                Length = Math.Min(payload.Length, 128),
                Fields = fields
            };
        }

        // TLS Application Data (0x17)
        if (payload[0] == 0x17 && payload.Length >= 5)
        {
            return new ProtocolLayer
            {
                Name = "TLS",
                Offset = offset,
                Length = Math.Min(payload.Length, 5),
                Fields = new Dictionary<string, string>
                {
                    ["Type"] = "Application Data",
                    ["Version"] = $"{payload[1]}.{payload[2]}"
                }
            };
        }

        // HTTP detection
        if (IsHttpRequest(payload))
        {
            var firstLine = ExtractFirstLine(payload);
            var parts = firstLine.Split(' ', 3);
            var fields = new Dictionary<string, string>
            {
                ["Method"] = parts.Length > 0 ? parts[0] : "",
                ["Path"] = parts.Length > 1 ? parts[1] : "",
                ["Version"] = parts.Length > 2 ? parts[2] : ""
            };

            var host = ExtractHttpHeader(payload, "Host");
            if (host is not null)
                fields["Host"] = host;

            return new ProtocolLayer
            {
                Name = "HTTP",
                Offset = offset,
                Length = Math.Min(payload.Length, 128),
                Fields = fields
            };
        }

        // HTTP response
        if (payload.Length >= 4 && payload[0] == 'H' && payload[1] == 'T' && payload[2] == 'T' && payload[3] == 'P')
        {
            var firstLine = ExtractFirstLine(payload);
            var parts = firstLine.Split(' ', 3);
            return new ProtocolLayer
            {
                Name = "HTTP",
                Offset = offset,
                Length = Math.Min(payload.Length, 128),
                Fields = new Dictionary<string, string>
                {
                    ["Version"] = parts.Length > 0 ? parts[0] : "",
                    ["Status Code"] = parts.Length > 1 ? parts[1] : "",
                    ["Reason"] = parts.Length > 2 ? parts[2] : ""
                }
            };
        }

        return null;
    }

    /// <summary>
    /// Extract DNS answer records (A/AAAA) from a DNS response packet's protocol stack.
    /// </summary>
    public static List<DnsAnswer> ExtractDnsAnswers(List<ProtocolLayer> stack)
    {
        var dnsLayer = stack.FirstOrDefault(l => l.Name == "DNS");
        if (dnsLayer is null || dnsLayer.Fields.GetValueOrDefault("Type") != "Response")
            return new List<DnsAnswer>();

        var answers = new List<DnsAnswer>();
        var answersStr = dnsLayer.Fields.GetValueOrDefault("Answers", "");
        if (string.IsNullOrEmpty(answersStr)) return answers;

        foreach (var pair in answersStr.Split(", "))
        {
            var eqIdx = pair.IndexOf('=');
            if (eqIdx > 0)
            {
                var name = pair[..eqIdx];
                var ip = pair[(eqIdx + 1)..];
                if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(name))
                    answers.Add(new DnsAnswer(name, ip));
            }
        }
        return answers;
    }

    public record DnsAnswer(string Hostname, string Ip);

    private static (string? QueryName, List<(string Name, string Ip)> Answers) ParseDnsPayload(byte[] data, bool isResponse)
    {
        string? queryName = null;
        var answers = new List<(string Name, string Ip)>();

        try
        {
            // DNS header: ID(2), Flags(2), QDCOUNT(2), ANCOUNT(2), NSCOUNT(2), ARCOUNT(2)
            if (data.Length < 12) return (null, answers);

            int qdCount = (data[4] << 8) | data[5];
            int anCount = (data[6] << 8) | data[7];

            int pos = 12;

            // Parse question section
            for (int q = 0; q < qdCount && pos < data.Length; q++)
            {
                var (name, newPos) = ReadDnsName(data, pos);
                if (name is not null && q == 0)
                    queryName = name;
                pos = newPos + 4; // Skip QTYPE(2) + QCLASS(2)
            }

            // Parse answer section (only if response)
            if (isResponse)
            {
                for (int a = 0; a < anCount && pos < data.Length; a++)
                {
                    var (name, newPos) = ReadDnsName(data, pos);
                    pos = newPos;

                    if (pos + 10 > data.Length) break;

                    int rType = (data[pos] << 8) | data[pos + 1];
                    // int rClass = (data[pos + 2] << 8) | data[pos + 3];
                    // int ttl = (data[pos + 4] << 24) | (data[pos + 5] << 16) | (data[pos + 6] << 8) | data[pos + 7];
                    int rdLength = (data[pos + 8] << 8) | data[pos + 9];
                    pos += 10;

                    if (pos + rdLength > data.Length) break;

                    var answerName = name ?? queryName ?? "";

                    if (rType == 1 && rdLength == 4) // A record
                    {
                        var ip = $"{data[pos]}.{data[pos + 1]}.{data[pos + 2]}.{data[pos + 3]}";
                        answers.Add((answerName, ip));
                    }
                    else if (rType == 28 && rdLength == 16) // AAAA record
                    {
                        var ipBytes = new byte[16];
                        Array.Copy(data, pos, ipBytes, 0, 16);
                        var ip = new System.Net.IPAddress(ipBytes).ToString();
                        answers.Add((answerName, ip));
                    }

                    pos += rdLength;
                }
            }
        }
        catch
        {
            // Best effort DNS parsing
        }

        return (queryName, answers);
    }

    private static (string? Name, int NewPos) ReadDnsName(byte[] data, int pos)
    {
        var parts = new List<string>();
        int maxJumps = 10;
        int jumps = 0;
        int savedPos = -1;

        while (pos < data.Length && jumps < maxJumps)
        {
            byte len = data[pos];

            if (len == 0)
            {
                pos++;
                break;
            }

            // Compression pointer
            if ((len & 0xC0) == 0xC0)
            {
                if (pos + 1 >= data.Length) break;
                if (savedPos < 0) savedPos = pos + 2;
                pos = ((len & 0x3F) << 8) | data[pos + 1];
                jumps++;
                continue;
            }

            pos++;
            if (pos + len > data.Length) break;
            parts.Add(System.Text.Encoding.ASCII.GetString(data, pos, len));
            pos += len;
        }

        if (savedPos >= 0) pos = savedPos;
        return (parts.Count > 0 ? string.Join(".", parts) : null, pos);
    }

    private static string? ExtractTlsSni(byte[] payload)
    {
        try
        {
            // TLS record: [type(1)][version(2)][length(2)][handshake_type(1)][length(3)][version(2)][random(32)][session_id_len(1)]...
            if (payload.Length < 44) return null;

            int pos = 5; // Skip TLS record header
            if (payload[pos] != 0x01) return null; // Not ClientHello
            pos += 4; // Skip handshake type + length
            pos += 2; // Skip client version
            pos += 32; // Skip random

            // Session ID
            if (pos >= payload.Length) return null;
            var sessionIdLen = payload[pos];
            pos += 1 + sessionIdLen;

            // Cipher suites
            if (pos + 2 > payload.Length) return null;
            var cipherSuitesLen = (payload[pos] << 8) | payload[pos + 1];
            pos += 2 + cipherSuitesLen;

            // Compression methods
            if (pos >= payload.Length) return null;
            var compMethodsLen = payload[pos];
            pos += 1 + compMethodsLen;

            // Extensions
            if (pos + 2 > payload.Length) return null;
            var extensionsLen = (payload[pos] << 8) | payload[pos + 1];
            pos += 2;

            var extensionsEnd = pos + extensionsLen;
            while (pos + 4 <= extensionsEnd && pos + 4 <= payload.Length)
            {
                var extType = (payload[pos] << 8) | payload[pos + 1];
                var extLen = (payload[pos + 2] << 8) | payload[pos + 3];
                pos += 4;

                if (extType == 0x0000) // SNI extension
                {
                    if (pos + 5 <= payload.Length)
                    {
                        // Skip SNI list length (2) and type (1)
                        var nameLen = (payload[pos + 3] << 8) | payload[pos + 4];
                        if (pos + 5 + nameLen <= payload.Length)
                        {
                            return System.Text.Encoding.ASCII.GetString(payload, pos + 5, nameLen);
                        }
                    }
                    break;
                }

                pos += extLen;
            }
        }
        catch
        {
            // Best effort SNI extraction
        }

        return null;
    }

    private static bool IsHttpRequest(byte[] data)
    {
        if (data.Length < 4) return false;
        var methods = new[] { "GET ", "POST", "PUT ", "DELE", "HEAD", "OPTI", "PATC" };
        var start = System.Text.Encoding.ASCII.GetString(data, 0, Math.Min(4, data.Length));
        return methods.Any(m => start.StartsWith(m, StringComparison.Ordinal));
    }

    private static string ExtractFirstLine(byte[] data)
    {
        var len = Math.Min(data.Length, 256);
        for (int i = 0; i < len; i++)
        {
            if (data[i] == '\r' || data[i] == '\n')
                return System.Text.Encoding.ASCII.GetString(data, 0, i);
        }
        return System.Text.Encoding.ASCII.GetString(data, 0, len);
    }

    private static string? ExtractHttpHeader(byte[] data, string headerName)
    {
        var text = System.Text.Encoding.ASCII.GetString(data, 0, Math.Min(data.Length, 512));
        var search = $"\r\n{headerName}: ";
        var idx = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var start = idx + search.Length;
        var end = text.IndexOf("\r\n", start);
        return end > start ? text[start..end] : text[start..];
    }
}
