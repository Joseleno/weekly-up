using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeeklyUp.Infrastructure.Persistence;

namespace WeeklyUp.Api.Tests.Infrastructure;

public sealed class WeeklyUpWebAppFactory : WebApplicationFactory<Program>
{
    // Mesma connection string dos user secrets / ambiente CI
    public static readonly string ConnectionString =
        "Host=localhost;Port=5432;Database=weeklyup_e2e;Username=weeklyup;Password=weeklyup123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Substitui a connection string pela do banco de testes
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(ConnectionString));
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Sobrescreve as configs necessárias para testes
            var testSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"]  = ConnectionString,
                ["ConnectionStrings:Redis"]     = "localhost:6379",
                ["Jwt:Key"]                     = "weeklyup-e2e-test-key-32chars!!x",
                ["Jwt:Issuer"]                  = "weeklyup-api",
                ["Jwt:Audience"]                = "weeklyup-clients",
                ["AesEncryption:Key"]           = "weeklyup-aes-key-32-characters!!",
                ["Resend:ApiKey"]               = "re_test_placeholder",
                ["Claude:ApiKey"]               = "sk-ant-test-placeholder",
                ["EvolutionApi:BaseUrl"]        = "http://localhost:8080",
                ["EvolutionApi:ApiKey"]         = "weeklyup-evolution-key",
                ["EvolutionApi:Instance"]       = "weeklyup",
            };

            config.AddInMemoryCollection(testSettings);
        });
    }
}
