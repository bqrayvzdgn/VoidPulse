namespace VoidPulse.Application.DTOs.Traffic;

public record TrafficFlowResponse(
    Guid Id,
    string SourceIp,
    string DestinationIp,
    int SourcePort,
    int DestinationPort,
    string Protocol,
    long BytesSent,
    long BytesReceived,
    int PacketsSent,
    int PacketsReceived,
    DateTime StartedAt,
    DateTime EndedAt,
    double FlowDuration,
    string? ProcessName,
    string? ResolvedHostname,
    string? TlsSni,
    HttpMetadataResponse? HttpMetadata,
    DateTime CreatedAt);

public record HttpMetadataResponse(
    string Method,
    string Host,
    string Path,
    int StatusCode,
    string? UserAgent,
    string? ContentType,
    double ResponseTimeMs);
