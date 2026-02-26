using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class Demographics : ValueObject
{
    private const int MaxCities = 10;
    private const int MaxStates = 5;

    public IReadOnlyDictionary<string, decimal> GenderDistribution { get; }
    public IReadOnlyDictionary<string, decimal> AgeGroups { get; }
    public IReadOnlyList<string> TopCities { get; }
    public IReadOnlyList<string> TopStates { get; }
    public IReadOnlyDictionary<string, decimal> Devices { get; }
    public IReadOnlyDictionary<string, decimal> TrafficSources { get; }

    public Demographics(
        IReadOnlyDictionary<string, decimal> genderDistribution,
        IReadOnlyDictionary<string, decimal> ageGroups,
        IReadOnlyList<string> topCities,
        IReadOnlyList<string> topStates,
        IReadOnlyDictionary<string, decimal> devices,
        IReadOnlyDictionary<string, decimal> trafficSources)
    {
        ArgumentNullException.ThrowIfNull(genderDistribution);
        ArgumentNullException.ThrowIfNull(ageGroups);
        ArgumentNullException.ThrowIfNull(topCities);
        ArgumentNullException.ThrowIfNull(topStates);
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(trafficSources);

        if (topCities.Count > MaxCities)
        {
            throw new ArgumentException($"TopCities nao pode ter mais de {MaxCities} entradas.", nameof(topCities));
        }

        if (topStates.Count > MaxStates)
        {
            throw new ArgumentException($"TopStates nao pode ter mais de {MaxStates} entradas.", nameof(topStates));
        }

        GenderDistribution = genderDistribution;
        AgeGroups = ageGroups;
        TopCities = topCities;
        TopStates = topStates;
        Devices = devices;
        TrafficSources = trafficSources;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return GenderDistribution;
        yield return AgeGroups;
        yield return TopCities;
        yield return TopStates;
        yield return Devices;
        yield return TrafficSources;
    }
}
