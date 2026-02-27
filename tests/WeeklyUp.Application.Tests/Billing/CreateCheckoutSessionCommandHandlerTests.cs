using FluentAssertions;

using Microsoft.Extensions.Options;

using NSubstitute;

using WeeklyUp.Application.Billing.Commands.CreateCheckoutSession;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Common.Settings;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Billing;

public sealed class CreateCheckoutSessionCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IStripeService _stripeService = Substitute.For<IStripeService>();
    private readonly CreateCheckoutSessionCommandHandler _sut;

    private static readonly StripeSettings DefaultSettings = new()
    {
        ProPriceId = "price_pro_test",
        BusinessPriceId = "price_business_test",
    };

    public CreateCheckoutSessionCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _sut = new CreateCheckoutSessionCommandHandler(
            _uow,
            _stripeService,
            Options.Create(DefaultSettings));
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Pro, "https://success.test", "https://cancel.test");
        _users.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<CheckoutSessionDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoStripeId_CreatesCustomerThenReturnsUrl()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce, verificationToken: "test-token").Value;
        var command = new CreateCheckoutSessionCommand(
            user.Id, PlanType.Pro, "https://success.test", "https://cancel.test");

        _users.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        _stripeService.GetOrCreateCustomerAsync("test@example.com", "Test User", Arg.Any<CancellationToken>())
            .Returns("cus_new123");
        _stripeService.CreateCheckoutSessionAsync(
                "cus_new123", DefaultSettings.ProPriceId,
                command.SuccessUrl, command.CancelUrl, user.Id, Arg.Any<CancellationToken>())
            .Returns("https://checkout.stripe.com/session123");

        // Act
        Result<CheckoutSessionDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be("https://checkout.stripe.com/session123");
        await _stripeService.Received(1).GetOrCreateCustomerAsync(
            "test@example.com", "Test User", Arg.Any<CancellationToken>());
        user.StripeCustomerId.Should().Be("cus_new123");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserHasStripeId_SkipsCustomerCreationAndReturnsUrl()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce, verificationToken: "test-token").Value;
        user.SetStripeCustomerId("cus_existing456");

        var command = new CreateCheckoutSessionCommand(
            user.Id, PlanType.Business, "https://success.test", "https://cancel.test");

        _users.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        _stripeService.CreateCheckoutSessionAsync(
                "cus_existing456", DefaultSettings.BusinessPriceId,
                command.SuccessUrl, command.CancelUrl, user.Id, Arg.Any<CancellationToken>())
            .Returns("https://checkout.stripe.com/session456");

        // Act
        Result<CheckoutSessionDto> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be("https://checkout.stripe.com/session456");
        await _stripeService.DidNotReceive().GetOrCreateCustomerAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validator_WhenPlanIsFree_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateCheckoutSessionCommandValidator();
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Free, "https://success.test", "https://cancel.test");

        // Act
        var validation = await validator.ValidateAsync(command);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle(e => e.PropertyName == "Plan");
    }
}
