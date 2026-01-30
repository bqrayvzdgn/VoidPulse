namespace VoidPulse.Application.DTOs.Dashboard;

public record BandwidthResponse(List<BandwidthEntry> Entries);

public record BandwidthEntry(DateTime Timestamp, long BytesSent, long BytesReceived, long TotalBytes);
