using FluentAssertions;
using NSubstitute;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Integrations.Commands.ConnectIntegration;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Integrations.Commands;

public sealed class ConnectIntegrationCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenEncryptor _encryptor = Substitute.For<ITokenEncryptor>();
    private readonly ConnectIntegrationCommandHandler _sut;

    public ConnectIntegrationCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _encryptor.Encrypt(Arg.Any<string>()).Returns(args => $"encrypted-{args[0]}");
        _sut = new ConnectIntegrationCommandHandler(_uow, _encryptor);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new ConnectIntegrationCommand(
            Guid.NewGuid(), IntegrationProvider.GoogleAnalytics4,
            "access-token", "refresh-token", "account-123", null);
        _users.GetByIdWithIntegrationsAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<IntegrationDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenUserFound_ShouldAddIntegrationAndReturnDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ConnectIntegrationCommand(
            userId, IntegrationProvider.GoogleAnalytics4,
            "access-token", "refresh-token", "account-123", null);
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        _users.GetByIdWithIntegrationsAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<IntegrationDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be(IntegrationProvider.GoogleAnalytics4.ToString());
        result.Value.AccountId.Should().Be("account-123");
        _users.Received(1).Update(user);
    }
}
