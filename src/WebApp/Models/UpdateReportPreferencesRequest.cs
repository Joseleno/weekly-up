namespace WeeklyUp.WebApp.Models;

// Alinhado com UpdatePreferencesRequest na API:
//   record(DayOfWeekPreference SendDay, TimeOnly SendTime, IReadOnlyList<string> EnabledSections)
// DayOfWeekPreference serializado como string via JsonStringEnumConverter na API.
// TimeOnly serializado como "HH:mm:ss".
public sealed record UpdateReportPreferencesRequest(
    string SendDay,
    string SendTime,
    IReadOnlyList<string> EnabledSections);
