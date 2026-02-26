using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Domain.Entities;

public sealed class ReportPreference : Entity
{
    private const string DefaultLanguage = "pt-BR";
    private static readonly TimeOnly DefaultSendTime = new(7, 0);

    private List<string> _enabledSections = [];

    public Guid UserId { get; private set; }
    public DayOfWeekPreference SendDay { get; private set; }
    public TimeOnly SendTime { get; private set; }
    public string Language { get; private set; } = DefaultLanguage;
    public IReadOnlyList<string> EnabledSections => _enabledSections.AsReadOnly();

    private ReportPreference() { }

    public static ReportPreference CreateDefault(Guid userId)
    {
        return new ReportPreference
        {
            UserId = userId,
            SendDay = DayOfWeekPreference.Monday,
            SendTime = DefaultSendTime,
            Language = DefaultLanguage,
            _enabledSections = [],
        };
    }

    public void Update(DayOfWeekPreference sendDay, TimeOnly sendTime, IReadOnlyList<string> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        SendDay = sendDay;
        SendTime = sendTime;
        _enabledSections = [.. sections];
        UpdatedAt = DateTime.UtcNow;
    }
}
