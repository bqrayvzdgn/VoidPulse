namespace VoidPulse.Application.DTOs.Dashboard;

public record OverviewResponse(
    long TotalFlows,
    long TotalBytes,
    int ActiveAgents,
    int UniqueSourceIps,
    int UniqueDestinationIps);
