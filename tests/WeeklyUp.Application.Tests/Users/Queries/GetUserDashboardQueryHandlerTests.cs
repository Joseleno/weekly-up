using FluentAssertions;
using NSubstitute;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Users.Queries.GetUserDashboard;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Users.Queries;

public sealed class GetUserDashboardQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IManualMetricRepository _manualMetrics = Substitute.For<IManualMetricRepository>();
    private readonly IApplicationCacheService _cache = Substitute.For<IApplicationCacheService>();
    private readonly GetUserDashboardQueryHandler _sut;

    public GetUserDashboardQueryHandlerTests()
    {
        _sut = new GetUserDashboardQueryHandler(_users, _reports, _manualMetrics, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserDashboardQuery(userId);
        var profileDto = new UserProfileDto(userId, "test@example.com", "User", "Biz",
            "Ecommerce", "Free", false, true, DateTimeOffset.UtcNow);
        var cachedDto = new UserDashboardDto(profileDto, null, [], false);
        _cache.GetAsync<UserDashboardDto>(CacheKeys.Dashboard(userId), Arg.Any<CancellationToken>())
            .Returns(cachedDto);

        // Act
        Result<UserDashboardDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedDto);
        await _users.DidNotReceive().GetByIdWithIntegrationsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldFetchDataAndReturnDashboard()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserDashboardQuery(userId);
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        _cache.GetAsync<UserDashboardDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserDashboardDto?)null);
        _users.GetByIdWithIntegrationsAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _reports.GetLatestAsync(userId, Arg.Any<CancellationToken>()).Returns((Report?)null);
        _manualMetrics.GetByUserAndWeekAsync(userId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((ManualMetric?)null);

        // Act
        Result<UserDashboardDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profile.Email.Should().Be("test@example.com");
        result.Value.LastReport.Should().BeNull();
        result.Value.HasManualMetrics.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserDashboardQuery(userId);
        _cache.GetAsync<UserDashboardDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserDashboardDto?)null);
        _users.GetByIdWithIntegrationsAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<UserDashboardDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }
}
