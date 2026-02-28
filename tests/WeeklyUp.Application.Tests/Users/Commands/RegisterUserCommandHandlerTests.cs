using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Users.Commands.RegisterUser;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Users.Commands;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IReportPreferenceRepository _reportPreferences = Substitute.For<IReportPreferenceRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly RegisterUserCommandHandler _sut;

    public RegisterUserCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _uow.ReportPreferences.Returns(_reportPreferences);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed_password");
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>()).Returns("jwt-token");
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _sut = new RegisterUserCommandHandler(_uow, _passwordHasher, _jwtTokenGenerator, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var command = new RegisterUserCommand("existing@example.com", "Senha@123", "Test User", "Test Biz", BusinessType.Ecommerce);
        var existingUser = User.Create("existing@example.com", "Existing User", "Existing Biz", BusinessType.Services, verificationToken: "test-token").Value;
        _users.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(existingUser);

        // Act
        Result<AuthTokenDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.Conflict);
        result.Error.Code.Should().Be("User.EmailAlreadyExists");
    }

    [Fact]
    public async Task Handle_WhenEmailIsNew_ShouldReturnAuthToken()
    {
        // Arrange
        var command = new RegisterUserCommand("new@example.com", "Senha@123", "New User", "New Business", BusinessType.Services);
        _users.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<AuthTokenDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt-token");
        result.Value.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_WhenEmailIsNew_ShouldAddReportPreferenceDefault()
    {
        // Arrange
        var command = new RegisterUserCommand("new@example.com", "Senha@123", "New User", "New Business", BusinessType.Services);
        _users.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _reportPreferences.Received(1).AddAsync(Arg.Any<ReportPreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailIsNew_ShouldAddUserToRepositoryAndPreference()
    {
        // Arrange
        var command = new RegisterUserCommand("new@example.com", "Senha@123", "New User", "New Business", BusinessType.Services);
        _users.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _reportPreferences.Received(1).AddAsync(Arg.Any<ReportPreference>(), Arg.Any<CancellationToken>());
    }
}
