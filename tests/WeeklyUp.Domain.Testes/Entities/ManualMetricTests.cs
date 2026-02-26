using FluentAssertions;

using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Domain.Testes.Entities;

public sealed class ManualMetricTests
{
    [Fact]
    public void Create_ValidMonday_ReturnsSuccess()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);

        // Act
        var result = ManualMetric.Create(Guid.NewGuid(), monday, 1000m, 10, 5, 200);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Revenue.Should().Be(1000m);
    }

    [Fact]
    public void Create_NotMonday_ReturnsFailure()
    {
        // Arrange
        var tuesday = new DateOnly(2025, 1, 7);

        // Act
        var result = ManualMetric.Create(Guid.NewGuid(), tuesday, null, null, null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.WeekStartNotMonday");
    }

    [Fact]
    public void Create_NegativeRevenue_ReturnsFailure()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);

        // Act
        var result = ManualMetric.Create(Guid.NewGuid(), monday, -1m, null, null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.NegativeRevenue");
    }

    [Fact]
    public void Create_NegativeSalesCount_ReturnsFailure()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);

        // Act
        var result = ManualMetric.Create(Guid.NewGuid(), monday, null, -1, null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.NegativeSalesCount");
    }

    [Fact]
    public void Create_NegativeNewCustomers_ReturnsFailure()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);

        // Act
        var result = ManualMetric.Create(Guid.NewGuid(), monday, null, null, -1, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.NegativeNewCustomers");
    }

    [Fact]
    public void Create_NegativeVisits_ReturnsFailure()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);

        // Act
        var result = ManualMetric.Create(Guid.NewGuid(), monday, null, null, null, -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.NegativeVisits");
    }

    [Fact]
    public void Create_AllNulls_ReturnsSuccess()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);

        // Act
        var result = ManualMetric.Create(Guid.NewGuid(), monday, null, null, null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Update_ValidValues_UpdatesFields()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);
        var metric = ManualMetric.Create(Guid.NewGuid(), monday, 100m, 5, 2, 50).Value;

        // Act
        var result = metric.Update(200m, 10, 4, 100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        metric.Revenue.Should().Be(200m);
        metric.SalesCount.Should().Be(10);
    }

    [Fact]
    public void Update_NegativeRevenue_ReturnsFailure()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);
        var metric = ManualMetric.Create(Guid.NewGuid(), monday, null, null, null, null).Value;

        // Act
        var result = metric.Update(-1m, null, null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.NegativeRevenue");
    }

    [Fact]
    public void Update_NegativeSalesCount_ReturnsFailure()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);
        var metric = ManualMetric.Create(Guid.NewGuid(), monday, null, null, null, null).Value;

        // Act
        var result = metric.Update(null, -1, null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.NegativeSalesCount");
    }

    [Fact]
    public void Update_NegativeNewCustomers_ReturnsFailure()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);
        var metric = ManualMetric.Create(Guid.NewGuid(), monday, null, null, null, null).Value;

        // Act
        var result = metric.Update(null, null, -1, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.NegativeNewCustomers");
    }

    [Fact]
    public void Update_NegativeVisits_ReturnsFailure()
    {
        // Arrange
        var monday = new DateOnly(2025, 1, 6);
        var metric = ManualMetric.Create(Guid.NewGuid(), monday, null, null, null, null).Value;

        // Act
        var result = metric.Update(null, null, null, -5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ManualMetric.NegativeVisits");
    }
}
