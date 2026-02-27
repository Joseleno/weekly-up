namespace WeeklyUp.WebApp.Models;

public sealed record UpdateReportPreferencesRequest(
    int SendHour,
    DayOfWeek SendDayOfWeek);
