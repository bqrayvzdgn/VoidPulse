namespace VoidPulse.Application.DTOs.Dashboard;

public record TopTalkersResponse(List<TalkerEntry> Entries);

public record TalkerEntry(string Ip, long TotalBytes, int FlowCount);
