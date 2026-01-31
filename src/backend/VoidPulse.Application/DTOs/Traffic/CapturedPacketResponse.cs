namespace VoidPulse.Application.DTOs.Traffic;

public record CapturedPacketResponse(
    Guid Id,
    Guid? TrafficFlowId,
    DateTime CapturedAt,
    string SourceIp,
    string DestinationIp,
    int SourcePort,
    int DestinationPort,
    string Protocol,
    int PacketLength,
    string HeaderBytesBase64,
    List<ProtocolLayerDto> ProtocolStack,
    string Info);

public record ProtocolLayerDto(
    string Name,
    int Offset,
    int Length,
    Dictionary<string, string> Fields);
