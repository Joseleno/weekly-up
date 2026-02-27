using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Users.Commands.RegisterUser;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Users.Commands;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IReportPreferenceRepository _reportPreferences = Substitute.For<IReportPreferenceRepository>();
    private readonly RegisterUserCommandHandler _sut;

    public RegisterUserCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _uow.ReportPreferences.Returns(_reportPreferences);
        _sut = new RegisterUserCommandHandler(_uow);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var command = new RegisterUserCommand("existing@example.com", "Test User", "Test Biz", BusinessType.Ecommerce);
        var existingUser = User.Create("existing@example.com", "Existing User", "Existing Biz", BusinessType.Services).Value;
        _users.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(existingUser);

        // Act
        Result<UserProfileDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.Conflict);
        result.Error.Code.Should().Be("User.EmailAlreadyExists");
    }

    [Fact]
    public async Task Handle_WhenEmailIsNew_ShouldCreateUserAndReturnProfileDto()
    {
        // Arrange
        var command = new RegisterUserCommand("new@example.com", "New User", "New Business", BusinessType.Services);
        _users.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<UserProfileDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(command.Email);
        result.Value.Name.Should().Be(command.Name);
        result.Value.BusinessName.Should().Be(command.BusinessName);
    }

    [Fact]
    public async Task Handle_WhenEmailIsNew_ShouldAddReportPreferenceDefault()
    {
        // Arrange
        var command = new RegisterUserCommand("new@example.com", "New User", "New Business", BusinessType.Services);
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
        var command = new RegisterUserCommand("new@example.com", "New User", "New Business", BusinessType.Services);
        _users.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _reportPreferences.Received(1).AddAsync(Arg.Any<ReportPreference>(), Arg.Any<CancellationToken>());
    }
}
