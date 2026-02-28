namespace WeeklyUp.WebApp.Models;

public sealed record ReportPreferencesResponse(
    string SendDay,
    string SendTime,
    IReadOnlyList<string> EnabledSections)
{
    public int SendHour =>
        int.TryParse(SendTime.Split(':')[0], out int h) ? h : 8;

    public DayOfWeek SendDayOfWeek =>
        Enum.TryParse<DayOfWeek>(SendDay, out var d) ? d : DayOfWeek.Monday;
}
