using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Users.Commands.UpgradePlan;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Users.Commands;

public sealed class UpgradePlanCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly UpgradePlanCommandHandler _sut;

    public UpgradePlanCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _sut = new UpgradePlanCommandHandler(_uow);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpgradePlanCommand(userId, PlanType.Pro);
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenUserFound_ShouldUpgradePlanAndReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpgradePlanCommand(userId, PlanType.Pro);
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        user.Plan.Should().Be(PlanType.Pro);
        _users.Received(1).Update(user);
    }
}
