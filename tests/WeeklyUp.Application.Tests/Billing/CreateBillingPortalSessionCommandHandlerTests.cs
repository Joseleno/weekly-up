using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Billing.Commands.CreateBillingPortalSession;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Billing;

public sealed class CreateBillingPortalSessionCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IStripeService _stripeService = Substitute.For<IStripeService>();
    private readonly CreateBillingPortalSessionCommandHandler _sut;

    public CreateBillingPortalSessionCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _sut = new CreateBillingPortalSessionCommandHandler(_uow, _stripeService);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var command = new CreateBillingPortalSessionCommand(Guid.NewGuid(), "https://return.test");
        _users.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<BillingPortalSessionDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoStripeCustomerId_ReturnsValidationError()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce, verificationToken: "test-token").Value;
        // user.StripeCustomerId is null — no Stripe subscription yet
        var command = new CreateBillingPortalSessionCommand(user.Id, "https://return.test");
        _users.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<BillingPortalSessionDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.Validation);
        result.Error.Code.Should().Be("Billing.NoStripeCustomer");
    }

    [Fact]
    public async Task Handle_WhenUserHasStripeCustomerId_ReturnsPortalUrl()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce, verificationToken: "test-token").Value;
        user.SetStripeCustomerId("cus_existing123");
        var command = new CreateBillingPortalSessionCommand(user.Id, "https://return.test");

        _users.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        _stripeService.CreateBillingPortalSessionAsync(
                "cus_existing123", command.ReturnUrl, Arg.Any<CancellationToken>())
            .Returns("https://billing.stripe.com/portal123");

        // Act
        Result<BillingPortalSessionDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be("https://billing.stripe.com/portal123");
        await _stripeService.Received(1).CreateBillingPortalSessionAsync(
            "cus_existing123", "https://return.test", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validator_WhenReturnUrlIsNotHttps_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateBillingPortalSessionCommandValidator();
        var command = new CreateBillingPortalSessionCommand(Guid.NewGuid(), "http://insecure.test");

        // Act
        var validation = await validator.ValidateAsync(command);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle(e => e.PropertyName == "ReturnUrl");
    }
}
