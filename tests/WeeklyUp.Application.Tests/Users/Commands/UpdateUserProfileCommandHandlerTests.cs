using FluentAssertions;
using NSubstitute;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Users.Commands.UpdateUserProfile;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Users.Commands;

public sealed class UpdateUserProfileCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly UpdateUserProfileCommandHandler _sut;

    public UpdateUserProfileCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _sut = new UpdateUserProfileCommandHandler(_uow);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserProfileCommand(userId, "New Name", "New Business", BusinessType.Services);
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<UserProfileDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenUserFound_ShouldUpdateProfileAndReturnDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserProfileCommand(userId, "Updated Name", "Updated Business", BusinessType.Services);
        var user = User.Create("test@example.com", "Original Name", "Original Business", BusinessType.Ecommerce).Value;
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<UserProfileDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated Name");
        result.Value.BusinessName.Should().Be("Updated Business");
        result.Value.BusinessType.Should().Be(BusinessType.Services.ToString());
        _users.Received(1).Update(user);
    }
}
