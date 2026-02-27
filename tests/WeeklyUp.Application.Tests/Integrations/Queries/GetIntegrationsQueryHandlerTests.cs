using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Integrations.Queries.GetIntegrations;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Integrations.Queries;

public sealed class GetIntegrationsQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IApplicationCacheService _cache = Substitute.For<IApplicationCacheService>();
    private readonly GetIntegrationsQueryHandler _sut;

    public GetIntegrationsQueryHandlerTests()
    {
        _sut = new GetIntegrationsQueryHandler(_users, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetIntegrationsQuery(userId);
        IReadOnlyList<IntegrationDto> cachedList = new List<IntegrationDto>
        {
            new IntegrationDto(Guid.NewGuid(), "GoogleAnalytics4", "Connected", "account-123", DateTimeOffset.UtcNow)
        }.AsReadOnly();
        _cache.GetAsync<IReadOnlyList<IntegrationDto>>(CacheKeys.Integrations(userId), Arg.Any<CancellationToken>())
            .Returns(cachedList);

        // Act
        Result<IReadOnlyList<IntegrationDto>> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        await _users.DidNotReceive().GetByIdWithIntegrationsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetIntegrationsQuery(userId);
        _cache.GetAsync<IReadOnlyList<IntegrationDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<IntegrationDto>?)null);
        _users.GetByIdWithIntegrationsAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<IReadOnlyList<IntegrationDto>> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenUserFound_ShouldReturnIntegrationDtoList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetIntegrationsQuery(userId);
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce, verificationToken: "test-token").Value;
        var encryptor = Substitute.For<ITokenEncryptor>();
        encryptor.Encrypt(Arg.Any<string>()).Returns(args => $"enc-{args[0]}");
        user.AddIntegration(
            IntegrationProvider.GoogleAnalytics4, "access-token", "refresh-token", "account-123", null, encryptor);

        _cache.GetAsync<IReadOnlyList<IntegrationDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<IntegrationDto>?)null);
        _users.GetByIdWithIntegrationsAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<IReadOnlyList<IntegrationDto>> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Provider.Should().Be("GoogleAnalytics4");
    }
}
