namespace WeeklyUp.WebApp.Models;

public sealed record DemographicsResponse(
    IReadOnlyList<AgeGroupResponse> AgeGroups,
    IReadOnlyList<string> TopCities);
