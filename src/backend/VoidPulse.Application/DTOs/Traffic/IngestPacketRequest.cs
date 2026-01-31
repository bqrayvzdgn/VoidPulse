namespace VoidPulse.Application.DTOs.Traffic;

public record IngestPacketRequest(
    DateTime CapturedAt,
    string SourceIp,
    string DestinationIp,
    int SourcePort,
    int DestinationPort,
    string Protocol,
    int PacketLength,
    string HeaderBytesBase64,
    string ProtocolStackJson,
    string Info);
