using System.Collections.ObjectModel;

using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class Demographics : ValueObject
{
    private const int MaxCities = 10;
    private const int MaxStates = 5;

    public Dictionary<string, decimal> GenderDistribution { get; private set; } = [];
    public Dictionary<string, decimal> AgeGroups { get; private set; } = [];
    public Collection<string> TopCities { get; private set; } = [];
    public Collection<string> TopStates { get; private set; } = [];
    public Dictionary<string, decimal> Devices { get; private set; } = [];
    public Dictionary<string, decimal> TrafficSources { get; private set; } = [];

    // Construtor sem parâmetros exigido pelo System.Text.Json para deserialização
    [System.Text.Json.Serialization.JsonConstructor]
    public Demographics() { }

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

        GenderDistribution = new Dictionary<string, decimal>(genderDistribution);
        AgeGroups = new Dictionary<string, decimal>(ageGroups);
        TopCities = new Collection<string>([.. topCities]);
        TopStates = new Collection<string>([.. topStates]);
        Devices = new Dictionary<string, decimal>(devices);
        TrafficSources = new Dictionary<string, decimal>(trafficSources);
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
