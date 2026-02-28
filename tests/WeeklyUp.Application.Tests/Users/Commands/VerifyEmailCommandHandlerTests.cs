using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Users.Commands.VerifyEmail;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Users.Commands;

public sealed class VerifyEmailCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly VerifyEmailCommandHandler _sut;

    public VerifyEmailCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _sut = new VerifyEmailCommandHandler(_uow);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new VerifyEmailCommand(userId, "some-token");
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenUserFoundWithValidToken_ShouldVerifyEmailAndReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string token = "valid-token";
        var command = new VerifyEmailCommand(userId, token);
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce, verificationToken: "test-token").Value;
        user.SetVerificationToken(token);
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        user.VerificationToken.Should().BeNull();
        _users.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WhenTokenInvalid_ShouldReturnValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new VerifyEmailCommand(userId, "wrong-token");
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce, verificationToken: "test-token").Value;
        user.SetVerificationToken("correct-token");
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.Validation);
        result.Error.Code.Should().Be("User.InvalidVerificationToken");
    }
}
