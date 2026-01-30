namespace VoidPulse.Application.DTOs.Traffic;

public record IngestTrafficRequest(
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
    HttpMetadataDto? HttpMetadata);

public record HttpMetadataDto(
    string Method,
    string Host,
    string Path,
    int StatusCode,
    string? UserAgent,
    string? ContentType,
    double ResponseTimeMs);
