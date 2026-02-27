// CA2012 is suppressed because NSubstitute's .Returns() setup uses ValueTask returned by
// .Send() intercept without awaiting it, which is intentional in test arrange blocks.
#pragma warning disable CA2012
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Mediator;

using NSubstitute;

using WeeklyUp.Api.Modules;
using WeeklyUp.Api.Tests.Infrastructure;
using WeeklyUp.Application.Billing.Commands.CreateBillingPortalSession;
using WeeklyUp.Application.Billing.Commands.CreateCheckoutSession;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Tests.Modules;

[Collection("Modules")]
public sealed class BillingModuleTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _authenticatedClient;
    private readonly HttpClient _anonymousClient;

    public BillingModuleTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _authenticatedClient = fixture.CreateAuthenticatedClient();
        _anonymousClient = fixture.CreateAnonymousClient();
        _fixture.Mediator.ClearReceivedCalls();
    }

    // ─── POST /api/billing/checkout-session ─────────────────────────────────────

    [Fact]
    public async Task CheckoutSession_SemAutenticacao_Retorna401()
    {
        // Arrange
        var body = new CreateCheckoutSessionRequest(Domain.Enums.PlanType.Pro, "https://s.test", "https://c.test");

        // Act
        HttpResponseMessage response = await _anonymousClient.PostAsJsonAsync("/api/billing/checkout-session", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CheckoutSession_ComAutenticacao_QuandoSucesso_Retorna200()
    {
        // Arrange
        var expectedDto = new CheckoutSessionDto("https://checkout.stripe.com/session_test");
        _fixture.Mediator
            .Send(Arg.Any<CreateCheckoutSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(expectedDto)));

        var body = new CreateCheckoutSessionRequest(Domain.Enums.PlanType.Pro, "https://s.test", "https://c.test");

        // Act
        HttpResponseMessage response = await _authenticatedClient.PostAsJsonAsync("/api/billing/checkout-session", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CheckoutSessionDto? dto = await response.Content.ReadFromJsonAsync<CheckoutSessionDto>();
        dto.Should().NotBeNull();
        dto!.Url.Should().Be("https://checkout.stripe.com/session_test");
    }

    [Fact]
    public async Task CheckoutSession_ComAutenticacao_QuandoNotFound_Retorna404()
    {
        // Arrange
        var error = AppError.NotFound("User.NotFound", "Usuario nao encontrado.");
        _fixture.Mediator
            .Send(Arg.Any<CreateCheckoutSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<CheckoutSessionDto>(error)));

        var body = new CreateCheckoutSessionRequest(Domain.Enums.PlanType.Pro, "https://s.test", "https://c.test");

        // Act
        HttpResponseMessage response = await _authenticatedClient.PostAsJsonAsync("/api/billing/checkout-session", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckoutSession_ComAutenticacao_QuandoValidation_Retorna422()
    {
        // Arrange
        var error = AppError.Validation("Billing.InvalidPlan", "Plano invalido.");
        _fixture.Mediator
            .Send(Arg.Any<CreateCheckoutSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<CheckoutSessionDto>(error)));

        var body = new CreateCheckoutSessionRequest(Domain.Enums.PlanType.Pro, "https://s.test", "https://c.test");

        // Act
        HttpResponseMessage response = await _authenticatedClient.PostAsJsonAsync("/api/billing/checkout-session", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ─── POST /api/billing/portal ──────────────────────────────────────────────

    [Fact]
    public async Task Portal_SemAutenticacao_Retorna401()
    {
        // Arrange
        var body = new BillingPortalRequest("https://return.test");

        // Act
        HttpResponseMessage response = await _anonymousClient.PostAsJsonAsync("/api/billing/portal", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Portal_ComAutenticacao_QuandoSucesso_Retorna200()
    {
        // Arrange
        var expectedDto = new BillingPortalSessionDto("https://billing.stripe.com/portal_test");
        _fixture.Mediator
            .Send(Arg.Any<CreateBillingPortalSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(expectedDto)));

        var body = new BillingPortalRequest("https://return.test");

        // Act
        HttpResponseMessage response = await _authenticatedClient.PostAsJsonAsync("/api/billing/portal", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        BillingPortalSessionDto? dto = await response.Content.ReadFromJsonAsync<BillingPortalSessionDto>();
        dto.Should().NotBeNull();
        dto!.Url.Should().Be("https://billing.stripe.com/portal_test");
    }

    [Fact]
    public async Task Portal_ComAutenticacao_QuandoValidation_Retorna422()
    {
        // Arrange
        var error = AppError.Validation("Billing.NoStripeCustomer", "Sem customer Stripe.");
        _fixture.Mediator
            .Send(Arg.Any<CreateBillingPortalSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<BillingPortalSessionDto>(error)));

        var body = new BillingPortalRequest("https://return.test");

        // Act
        HttpResponseMessage response = await _authenticatedClient.PostAsJsonAsync("/api/billing/portal", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
