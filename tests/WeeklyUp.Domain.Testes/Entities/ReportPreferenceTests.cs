using FluentAssertions;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Domain.Testes.Entities;

public sealed class ReportPreferenceTests
{
    [Fact]
    public void CreateDefault_SetsDefaultValues()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var pref = ReportPreference.CreateDefault(userId);

        // Assert
        pref.UserId.Should().Be(userId);
        pref.SendDay.Should().Be(DayOfWeekPreference.Monday);
        pref.SendTime.Should().Be(new TimeOnly(7, 0));
        pref.Language.Should().Be("pt-BR");
        pref.EnabledSections.Should().BeEmpty();
    }

    [Fact]
    public void Update_ValidData_UpdatesFields()
    {
        // Arrange
        var pref = ReportPreference.CreateDefault(Guid.NewGuid());
        IReadOnlyList<string> sections = new List<string> { "revenue", "visits" };

        // Act
        pref.Update(DayOfWeekPreference.Friday, new TimeOnly(9, 0), sections);

        // Assert
        pref.SendDay.Should().Be(DayOfWeekPreference.Friday);
        pref.SendTime.Should().Be(new TimeOnly(9, 0));
        pref.EnabledSections.Should().BeEquivalentTo(sections);
    }

    [Fact]
    public void Update_EmptySections_Succeeds()
    {
        // Arrange
        var pref = ReportPreference.CreateDefault(Guid.NewGuid());
        IReadOnlyList<string> sections = new List<string>();

        // Act
        pref.Update(DayOfWeekPreference.Wednesday, new TimeOnly(8, 0), sections);

        // Assert
        pref.EnabledSections.Should().BeEmpty();
        pref.SendDay.Should().Be(DayOfWeekPreference.Wednesday);
    }

    [Fact]
    public void CreateDefault_LanguageIsPortuguese()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var pref = ReportPreference.CreateDefault(userId);

        // Assert
        pref.Language.Should().Be("pt-BR");
    }

    [Fact]
    public void Update_NullSections_ThrowsArgumentNullException()
    {
        // Arrange
        var pref = ReportPreference.CreateDefault(Guid.NewGuid());

        // Act
        var act = () => pref.Update(DayOfWeekPreference.Monday, new TimeOnly(7, 0), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
