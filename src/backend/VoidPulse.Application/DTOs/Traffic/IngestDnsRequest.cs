namespace VoidPulse.Application.DTOs.Traffic;

public record IngestDnsRequest(
    string QueriedHostname,
    string ResolvedIp,
    string QueryType = "A",
    int Ttl = 300,
    string? ClientIp = null,
    DateTime? ResolvedAt = null);
