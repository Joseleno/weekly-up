using System.Net.Http.Headers;
using System.Security.Claims;

using Hangfire;

using Mediator;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Infrastructure.Billing;
using WeeklyUp.Infrastructure.Persistence;
using WeeklyUp.Shared.Constants;

namespace WeeklyUp.Api.Tests.Infrastructure;

/// <summary>
/// Shared fixture for module unit tests (one WebApplicationFactory per collection).
/// Serilog's bootstrap logger can only be frozen once per process — sharing the factory
/// avoids "logger already frozen" when multiple test classes each create their own factory.
/// </summary>
public sealed class ApiTestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;

    // Shared mocks — tests call ClearReceivedCalls() between scenarios when needed.
    public IMediator Mediator { get; } = Substitute.For<IMediator>();
    public ICurrentUserService CurrentUser { get; } = Substitute.For<ICurrentUserService>();
    public IStripeService StripeService { get; } = Substitute.For<IStripeService>();

    public static readonly Guid DefaultUserId = Guid.NewGuid();

    public Task InitializeAsync()
    {
        CurrentUser.UserId.Returns(DefaultUserId);

#pragma warning disable CA2000 // Disposed in DisposeAsync
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, cfg) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Database"] = "Host=localhost;Database=dummy",
                        ["ConnectionStrings:Redis"] = "localhost:6379",
                        ["Jwt:Key"] = "unit-test-key-32-chars-padding!x",
                        ["Jwt:Issuer"] = "weeklyup-api",
                        ["Jwt:Audience"] = "weeklyup-clients",
                        ["AesEncryption:Key"] = "unit-test-aes-32-chars-padding!x",
                        ["Resend:ApiKey"] = "re_test",
                        ["Claude:ApiKey"] = "sk-ant-test",
                        ["EvolutionApi:BaseUrl"] = "http://localhost:8080",
                        ["EvolutionApi:ApiKey"] = "test-key",
                        ["EvolutionApi:Instance"] = "test",
                        ["Stripe:SecretKey"] = "sk_test_unit_placeholder",
                        ["Stripe:WebhookSecret"] = "whsec_unit_placeholder",
                        ["Stripe:ProPriceId"] = "price_pro_unit",
                        ["Stripe:BusinessPriceId"] = "price_business_unit",
                    };
                    cfg.AddInMemoryCollection(settings);
                });

                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with InMemory to avoid real DB connection.
                    // Remove ALL EF Core-related descriptors to prevent "multiple providers" error.
                    var dbDescriptors = services
                        .Where(d => d.ServiceType.FullName?.Contains("EntityFramework") == true
                            || d.ServiceType == typeof(ApplicationDbContext)
                            || d.ServiceType == typeof(DbContextOptions)
                            || d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>))
                        .ToList();
                    foreach (var descriptor in dbDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(opts =>
                        opts.UseInMemoryDatabase("ApiTestDb_Modules"));

                    // Replace Hangfire with in-memory storage
                    services.AddHangfire(cfg => cfg.UseInMemoryStorage());

                    // Replace IMediator with shared mock
                    services.RemoveAll<IMediator>();
                    services.AddSingleton(Mediator);

                    // Replace ICurrentUserService with shared mock
                    services.RemoveAll<ICurrentUserService>();
                    services.AddSingleton(CurrentUser);

                    // Replace IStripeService with shared singleton mock — avoids real Stripe calls
                    services.RemoveAll<IStripeService>();
                    services.AddSingleton(StripeService);

                    // Add test authentication scheme
                    services
                        .AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            "Test", _ => { });

                    services.Configure<AuthenticationOptions>(opts =>
                    {
                        opts.DefaultAuthenticateScheme = "Test";
                        opts.DefaultChallengeScheme = "Test";
                    });
                });
            });
#pragma warning restore CA2000

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
            _factory = null;
        }
    }

    /// <summary>Creates an authenticated HttpClient (has UserId + Plan claims).</summary>
    public HttpClient CreateAuthenticatedClient(string plan = "Free")
    {
        var client = _factory!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test", plan);
        return client;
    }

    /// <summary>Creates an anonymous HttpClient.</summary>
    public HttpClient CreateAnonymousClient() => _factory!.CreateClient();
}

/// <summary>
/// xUnit collection definition — shares a single ApiTestFixture across all module test classes.
/// </summary>
[CollectionDefinition("Modules")]
public sealed class ModulesCollectionFixture : ICollectionFixture<ApiTestFixture>
{
}

/// <summary>
/// Test authentication handler that creates a fake ClaimsPrincipal
/// so endpoints with RequireAuthorization() succeed.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var header = values.FirstOrDefault();
        if (header is null || !header.StartsWith("Test ", StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var plan = header["Test ".Length..];

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ApiTestFixture.DefaultUserId.ToString()),
            new Claim(ClaimTypes.Email, "test@test.com"),
            new Claim(CustomClaimTypes.Plan, plan),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
