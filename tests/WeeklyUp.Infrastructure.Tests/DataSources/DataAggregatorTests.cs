using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Infrastructure.DataSources;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.Tests.DataSources;

public sealed class DataAggregatorTests
{
    private readonly IIntegrationRepository _integrations = Substitute.For<IIntegrationRepository>();

    private static DateRange CreateWeekRange() =>
        DateRange.Create(new DateOnly(2026, 2, 16), new DateOnly(2026, 2, 22)).Value;

    private static ReportMetrics CreateMetrics() =>
        new(Money.BRL(1000m), 10, Money.BRL(100m), 5, 200, 150, 300, "/home", "google", null, null);

    private static Integration CreateConnectedIntegration(IntegrationProvider provider = IntegrationProvider.GoogleAnalytics4)
    {
        var encryptor = Substitute.For<ITokenEncryptor>();
        encryptor.Encrypt(Arg.Any<string>()).Returns("encrypted");

        var userResult = User.Create("owner@test.com", "Owner", "Business", BusinessType.Ecommerce, verificationToken: "test-token");
        var user = userResult.Value;
        user.AddIntegration(provider, "access-token", "refresh-token", "account-1", null, encryptor);
        return user.Integrations.First();
    }

    private DataAggregator CreateAggregator(IEnumerable<IDataSourceProvider> providers) =>
        new(providers, _integrations);

    [Fact]
    public async Task GetAggregatedMetricsAsync_WhenNoActiveIntegrations_ShouldReturnNotFoundError()
    {
        // Arrange
        var aggregator = CreateAggregator([]);
        var userId = Guid.NewGuid();
        _integrations.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Integration>().AsReadOnly());

        // Act
        var result = await aggregator.GetAggregatedMetricsAsync(userId, CreateWeekRange());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DataAggregator.NoIntegrations");
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_WhenProviderNotFound_ShouldSkipAndContinue()
    {
        // Arrange
        var integration = CreateConnectedIntegration(IntegrationProvider.GoogleAnalytics4);
        var userId = integration.UserId;

        _integrations.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Integration> { integration }.AsReadOnly());

        // No provider registered for GoogleAnalytics4
        var aggregator = CreateAggregator([]);

        // Act
        var result = await aggregator.GetAggregatedMetricsAsync(userId, CreateWeekRange());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DataAggregator.NoData");
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_WhenFirstProviderSucceeds_ShouldReturnMetrics()
    {
        // Arrange
        var integration = CreateConnectedIntegration(IntegrationProvider.GoogleAnalytics4);
        var userId = integration.UserId;
        var metrics = CreateMetrics();

        _integrations.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Integration> { integration }.AsReadOnly());

        var provider = Substitute.For<IDataSourceProvider>();
        provider.ProviderType.Returns(IntegrationProvider.GoogleAnalytics4);
        provider.GetMetricsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(metrics));

        var aggregator = CreateAggregator([provider]);

        // Act
        var result = await aggregator.GetAggregatedMetricsAsync(userId, CreateWeekRange());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(metrics);
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_WhenFirstProviderFails_ShouldTryNextProvider()
    {
        // Arrange
        var encryptor = Substitute.For<ITokenEncryptor>();
        encryptor.Encrypt(Arg.Any<string>()).Returns("encrypted");
        var userResult = User.Create("owner2@test.com", "Owner2", "Business2", BusinessType.Ecommerce, verificationToken: "test-token");
        var user = userResult.Value;
        user.UpgradePlan(PlanType.Pro);
        user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "tok1", "ref1", "acc1", null, encryptor);
        user.AddIntegration(IntegrationProvider.Stripe, "tok2", "ref2", "acc2", null, encryptor);

        var ga4Integration = user.Integrations.First(i => i.Provider == IntegrationProvider.GoogleAnalytics4);
        var metaIntegration = user.Integrations.First(i => i.Provider == IntegrationProvider.Stripe);

        var userId = user.Id;
        var metrics = CreateMetrics();

        _integrations.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Integration> { ga4Integration, metaIntegration }.AsReadOnly());

        var failingProvider = Substitute.For<IDataSourceProvider>();
        failingProvider.ProviderType.Returns(IntegrationProvider.GoogleAnalytics4);
        failingProvider.GetMetricsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ReportMetrics>(AppError.Failure("GA4.Failed", "API error")));

        var successProvider = Substitute.For<IDataSourceProvider>();
        successProvider.ProviderType.Returns(IntegrationProvider.Stripe);
        successProvider.GetMetricsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(metrics));

        var aggregator = CreateAggregator([failingProvider, successProvider]);

        // Act
        var result = await aggregator.GetAggregatedMetricsAsync(userId, CreateWeekRange());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(metrics);
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_WhenAllProvidersFail_ShouldReturnFailureError()
    {
        // Arrange
        var integration = CreateConnectedIntegration(IntegrationProvider.GoogleAnalytics4);
        var userId = integration.UserId;

        _integrations.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Integration> { integration }.AsReadOnly());

        var failingProvider = Substitute.For<IDataSourceProvider>();
        failingProvider.ProviderType.Returns(IntegrationProvider.GoogleAnalytics4);
        failingProvider.GetMetricsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ReportMetrics>(AppError.Failure("GA4.Failed", "API error")));

        var aggregator = CreateAggregator([failingProvider]);

        // Act
        var result = await aggregator.GetAggregatedMetricsAsync(userId, CreateWeekRange());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DataAggregator.NoData");
    }
}
