namespace VoidPulse.Application.DTOs.Dashboard;

public record ProcessesResponse(List<ProcessEntry> Processes, int TotalProcesses);

public record ProcessEntry(
    string ProcessName,
    long TotalBytes,
    int FlowCount,
    DateTime LastSeen);
