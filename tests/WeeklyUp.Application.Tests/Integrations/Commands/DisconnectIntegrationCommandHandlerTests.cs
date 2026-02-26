using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Integrations.Commands.DisconnectIntegration;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Integrations.Commands;

public sealed class DisconnectIntegrationCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly DisconnectIntegrationCommandHandler _sut;

    public DisconnectIntegrationCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _sut = new DisconnectIntegrationCommandHandler(_uow);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new DisconnectIntegrationCommand(Guid.NewGuid(), IntegrationProvider.GoogleAnalytics4);
        _users.GetByIdWithIntegrationsAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenUserFound_ShouldRemoveIntegrationAndReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DisconnectIntegrationCommand(userId, IntegrationProvider.GoogleAnalytics4);
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;

        var encryptor = Substitute.For<ITokenEncryptor>();
        encryptor.Encrypt(Arg.Any<string>()).Returns(args => $"encrypted-{args[0]}");
        user.AddIntegration(
            IntegrationProvider.GoogleAnalytics4, "access-token", "refresh-token", "account-123", null, encryptor);

        _users.GetByIdWithIntegrationsAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _users.Received(1).Update(user);
    }
}
