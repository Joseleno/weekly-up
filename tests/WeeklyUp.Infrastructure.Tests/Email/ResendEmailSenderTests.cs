using System.Net;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Infrastructure.Email;

namespace WeeklyUp.Infrastructure.Tests.Email;

public sealed class ResendEmailSenderTests
{
    private const string FromEmail = "noreply@weeklyup.app";
    private const string FromName = "WeeklyUp";
    private const string RecipientEmail = "user@test.com";
    private const string RecipientName = "Test User";
    private const string VerificationToken = "verify-token-xyz";

    private static ResendOptions DefaultOptions => new()
    {
        ApiKey = "test-key",
        FromEmail = FromEmail,
        FromName = FromName,
    };

    // HttpClient toma ownership do handler (disposeHandler: true).
    // O caller é responsável por descartar o HttpClient retornado (que descarta o handler).
    private static (ResendEmailSender Sender, HttpClient Client) CreateSender(HttpMessageHandler handler)
    {
#pragma warning disable CA2000 // Ownership transferido para HttpClient via disposeHandler: true
        var httpClient = new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://api.resend.com") };
#pragma warning restore CA2000
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("resend").Returns(httpClient);
        var logger = Substitute.For<ILogger<ResendEmailSender>>();
        return (new ResendEmailSender(factory, Options.Create(DefaultOptions), logger), httpClient);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenHttpSuccess_ReturnsSuccess()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new FakeHttpMessageHandler(HttpStatusCode.OK));
#pragma warning restore CA2000
        using (client)
        {
            // Act
            var result = await sut.SendVerificationEmailAsync(RecipientEmail, RecipientName, VerificationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenHttpFails_ReturnsFailure()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new FakeHttpMessageHandler(HttpStatusCode.UnprocessableEntity));
#pragma warning restore CA2000
        using (client)
        {
            // Act
            var result = await sut.SendVerificationEmailAsync(RecipientEmail, RecipientName, VerificationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.SendFailed");
        }
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenHttpRequestException_ReturnsFailure()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new ThrowingHttpMessageHandler());
#pragma warning restore CA2000
        using (client)
        {
            // Act
            var result = await sut.SendVerificationEmailAsync(RecipientEmail, RecipientName, VerificationToken);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.Exception");
        }
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenTokenIsEmpty_ReturnsValidationError()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new FakeHttpMessageHandler(HttpStatusCode.OK));
#pragma warning restore CA2000
        using (client)
        {
            // Act
            var result = await sut.SendVerificationEmailAsync(RecipientEmail, RecipientName, string.Empty);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.InvalidToken");
        }
    }

    [Fact]
    public async Task SendWelcomeAsync_WhenHttpSuccess_ReturnsSuccess()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new FakeHttpMessageHandler(HttpStatusCode.OK));
#pragma warning restore CA2000
        using (client)
        {
            // Act
            var result = await sut.SendWelcomeAsync(RecipientEmail, RecipientName);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SendWelcomeAsync_WhenHttpFails_ReturnsFailure()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new FakeHttpMessageHandler(HttpStatusCode.BadRequest));
#pragma warning restore CA2000
        using (client)
        {
            // Act
            var result = await sut.SendWelcomeAsync(RecipientEmail, RecipientName);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.SendFailed");
        }
    }

    [Fact]
    public async Task SendWelcomeAsync_WhenHttpRequestException_ReturnsFailure()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new ThrowingHttpMessageHandler());
#pragma warning restore CA2000
        using (client)
        {
            // Act
            var result = await sut.SendWelcomeAsync(RecipientEmail, RecipientName);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.Exception");
        }
    }

    private static Report CreateTestReport()
    {
        var weekRange = DateRange.Create(
            new DateOnly(2025, 1, 6),
            new DateOnly(2025, 1, 12)).Value;
        return Report.Create(Guid.NewGuid(), weekRange);
    }

    [Fact]
    public async Task SendWeeklyReportAsync_WhenHttpSuccess_ReturnsSuccess()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new FakeHttpMessageHandler(HttpStatusCode.OK));
#pragma warning restore CA2000
        using (client)
        {
            var report = CreateTestReport();

            // Act
            var result = await sut.SendWeeklyReportAsync(RecipientEmail, RecipientName, report);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SendWeeklyReportAsync_WhenHttpFails_ReturnsFailure()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new FakeHttpMessageHandler(HttpStatusCode.InternalServerError));
#pragma warning restore CA2000
        using (client)
        {
            var report = CreateTestReport();

            // Act
            var result = await sut.SendWeeklyReportAsync(RecipientEmail, RecipientName, report);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.SendFailed");
        }
    }

    [Fact]
    public async Task SendWeeklyReportAsync_WhenHttpRequestException_ReturnsFailure()
    {
        // Arrange
#pragma warning disable CA2000
        var (sut, client) = CreateSender(new ThrowingHttpMessageHandler());
#pragma warning restore CA2000
        using (client)
        {
            var report = CreateTestReport();

            // Act
            var result = await sut.SendWeeklyReportAsync(RecipientEmail, RecipientName, report);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Email.Exception");
        }
    }

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Simulated network failure");
    }
}
