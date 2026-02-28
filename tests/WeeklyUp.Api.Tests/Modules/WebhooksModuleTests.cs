// CA2012 is suppressed because NSubstitute's .Returns() setup uses ValueTask returned by
// .Send() intercept without awaiting it, which is intentional in test arrange blocks.
#pragma warning disable CA2012
using System.Net;
using System.Text;

using FluentAssertions;

using Mediator;

using NSubstitute;

using WeeklyUp.Api.Tests.Infrastructure;
using WeeklyUp.Application.Billing;
using WeeklyUp.Application.Billing.Commands.HandleStripeWebhook;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Tests.Modules;

[Collection("Modules")]
public sealed class WebhooksModuleTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public WebhooksModuleTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAnonymousClient();
        // Reset shared mock state to prevent test pollution across test runs
        _fixture.StripeService.ClearReceivedCalls();
        _fixture.Mediator.ClearReceivedCalls();
    }

    // ─── POST /api/webhooks/stripe ────────────────────────────────────────────

    [Fact]
    public async Task StripeWebhook_SemStripeSignatureHeader_Retorna400()
    {
        // Arrange — sem o header Stripe-Signature
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/webhooks/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StripeWebhook_ComAssinaturaInvalida_Retorna400()
    {
        // Arrange — IStripeService retorna falha quando assinatura é inválida
        _fixture.StripeService
            .ParseWebhookEvent(Arg.Any<string>(), Arg.Any<string>())
            .Returns(AppError.Validation("Webhook.InvalidSignature", "Assinatura invalida."));

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = content,
        };
        request.Headers.Add("Stripe-Signature", "t=invalid,v1=invalid");

        // Act
        HttpResponseMessage response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StripeWebhook_ComEventoValido_EnviaCommandERetorna200()
    {
        // Arrange — IStripeService retorna evento válido, mediator processa e retorna sucesso
        var webhookEvent = new StripeWebhookEvent(
            "customer.subscription.updated",
            "evt_test123",
            Guid.NewGuid().ToString(),
            "price_pro_test");

        _fixture.StripeService
            .ParseWebhookEvent(Arg.Any<string>(), Arg.Any<string>())
            .Returns(webhookEvent);

        _fixture.Mediator
            .Send(Arg.Any<HandleStripeWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        using var content = new StringContent(
            """{"type":"customer.subscription.updated"}""", Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = content,
        };
        request.Headers.Add("Stripe-Signature", "t=1234,v1=valid_sig");

        // Act
        HttpResponseMessage response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
