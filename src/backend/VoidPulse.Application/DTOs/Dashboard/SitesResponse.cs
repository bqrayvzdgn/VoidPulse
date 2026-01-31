namespace VoidPulse.Application.DTOs.Dashboard;

public record SitesResponse(List<SiteEntry> Sites, int TotalSites);

public record SiteEntry(
    string Hostname,
    long TotalBytes,
    int FlowCount,
    DateTime LastSeen);
