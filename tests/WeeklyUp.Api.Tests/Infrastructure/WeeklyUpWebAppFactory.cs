using Hangfire;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using StackExchange.Redis;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Infrastructure.Billing;
using WeeklyUp.Infrastructure.Persistence;

namespace WeeklyUp.Api.Tests.Infrastructure;

public sealed class WeeklyUpWebAppFactory : WebApplicationFactory<Program>
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=weeklyup_e2e;Username=weeklyup;Password=weeklyup123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Substitui a connection string pela do banco de testes
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(ConnectionString));

            // Substitui Hangfire PostgreSQL por in-memory para evitar conexão ao banco durante testes
            services.AddHangfire(cfg => cfg.UseInMemoryStorage());

            // Substitui Redis por mock — cache não é crítico para validar fluxos E2E
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton(_ => Substitute.For<IConnectionMultiplexer>());
            services.RemoveAll<IApplicationCacheService>();
            services.AddScoped(_ => Substitute.For<IApplicationCacheService>());

            // Substitui o scheduler de jobs por mock — evita que Hangfire dispare jobs externos nos E2E
            services.RemoveAll<IReportJobScheduler>();
            services.AddScoped(_ => Substitute.For<IReportJobScheduler>());

            // Substitui IStripeService por mock — evita chamadas reais ao Stripe nos E2E
            services.RemoveAll<IStripeService>();
            services.AddScoped(_ => Substitute.For<IStripeService>());
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Sobrescreve as configs necessárias para testes
            var testSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = ConnectionString,
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Jwt:Key"] = "weeklyup-e2e-test-key-32chars!!x",
                ["Jwt:Issuer"] = "weeklyup-api",
                ["Jwt:Audience"] = "weeklyup-clients",
                ["AesEncryption:Key"] = "weeklyup-aes-key-32-characters!!",
                ["Resend:ApiKey"] = "re_test_placeholder",
                ["Claude:ApiKey"] = "sk-ant-test-placeholder",
                ["EvolutionApi:BaseUrl"] = "http://localhost:8080",
                ["EvolutionApi:ApiKey"] = "weeklyup-evolution-key",
                ["EvolutionApi:Instance"] = "weeklyup",
                ["Stripe:SecretKey"] = "sk_test_e2e_placeholder",
                ["Stripe:WebhookSecret"] = "whsec_e2e_placeholder",
                ["Stripe:ProPriceId"] = "price_pro_e2e",
                ["Stripe:BusinessPriceId"] = "price_business_e2e",
            };

            config.AddInMemoryCollection(testSettings);
        });
    }
}
