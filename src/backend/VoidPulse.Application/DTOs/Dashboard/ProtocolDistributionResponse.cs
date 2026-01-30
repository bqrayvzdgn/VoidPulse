namespace VoidPulse.Application.DTOs.Dashboard;

public record ProtocolDistributionResponse(List<ProtocolEntry> Entries);

public record ProtocolEntry(string Protocol, long TotalBytes, int FlowCount, double Percentage);
