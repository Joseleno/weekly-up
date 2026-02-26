namespace WeeklyUp.Application.Common.DTOs;

public sealed record DemographicsDto(
    IReadOnlyList<AgeGroupDto> AgeGroups,
    IReadOnlyList<string> TopCities);
