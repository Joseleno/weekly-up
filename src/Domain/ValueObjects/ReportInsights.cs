using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class ReportInsights : ValueObject
{
    public string Highlight { get; private set; } = string.Empty;
    public string Alert { get; private set; } = string.Empty;
    public string Tip { get; private set; } = string.Empty;
    public DateTime GeneratedAt { get; private set; }

    private ReportInsights() { }

    public ReportInsights(string highlight, string alert, string tip, DateTime generatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(highlight);
        ArgumentException.ThrowIfNullOrWhiteSpace(alert);
        ArgumentException.ThrowIfNullOrWhiteSpace(tip);

        Highlight = highlight;
        Alert = alert;
        Tip = tip;
        GeneratedAt = generatedAt;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Highlight;
        yield return Alert;
        yield return Tip;
        yield return GeneratedAt;
    }
}
