using FluentAssertions;
using NSubstitute;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Users.Queries.GetUserProfile;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Users.Queries;

public sealed class GetUserProfileQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IApplicationCacheService _cache = Substitute.For<IApplicationCacheService>();
    private readonly GetUserProfileQueryHandler _sut;

    public GetUserProfileQueryHandlerTests()
    {
        _sut = new GetUserProfileQueryHandler(_users, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedDtoWithoutHittingRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery(userId);
        var cachedDto = new UserProfileDto(userId, "test@example.com", "Cached User", "Cached Biz",
            "Ecommerce", "Free", false, true, DateTimeOffset.UtcNow);
        _cache.GetAsync<UserProfileDto>(CacheKeys.UserProfile(userId), Arg.Any<CancellationToken>())
            .Returns(cachedDto);

        // Act
        Result<UserProfileDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedDto);
        await _users.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldFetchFromRepositoryAndSetCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery(userId);
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        _cache.GetAsync<UserProfileDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserProfileDto?)null);
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<UserProfileDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
        await _cache.Received(1).SetAsync(
            CacheKeys.UserProfile(userId),
            Arg.Any<UserProfileDto>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery(userId);
        _cache.GetAsync<UserProfileDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserProfileDto?)null);
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<UserProfileDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }
}
