namespace WeeklyUp.Application.Common.DTOs;

public sealed record ReportPreferencesDto(
    string SendDay,
    string SendTime,
    IReadOnlyList<string> EnabledSections);
