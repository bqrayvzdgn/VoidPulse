namespace VoidPulse.Application.DTOs.Traffic;

public record PacketQueryParams(
    string? SourceIp = null,
    string? DestinationIp = null,
    string? Protocol = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50);
