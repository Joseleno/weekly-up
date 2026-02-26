using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Refit;
using StackExchange.Redis;
using Stripe;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Infrastructure.AI;
using WeeklyUp.Infrastructure.BackgroundJobs;
using WeeklyUp.Infrastructure.Caching;
using WeeklyUp.Infrastructure.DataSources;
using WeeklyUp.Infrastructure.DataSources.GoogleAnalytics;
using WeeklyUp.Infrastructure.DataSources.Manual;
using WeeklyUp.Infrastructure.DataSources.Stripe;
using WeeklyUp.Infrastructure.Email;
using WeeklyUp.Infrastructure.Persistence;
using WeeklyUp.Infrastructure.Persistence.Interceptors;
using WeeklyUp.Infrastructure.Persistence.Repositories;
using WeeklyUp.Infrastructure.Security;
using WeeklyUp.Infrastructure.Time;
using WeeklyUp.Infrastructure.WhatsApp;

namespace WeeklyUp.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddPersistence(configuration)
            .AddCaching(configuration)
            .AddSecurity(configuration)
            .AddExternalServices(configuration)
            .AddBackgroundJobs(configuration);

        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseConnectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(databaseConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Database nao configurada. Verifique appsettings ou variaveis de ambiente.");
        }

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(databaseConnectionString);
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportPreferenceRepository, ReportPreferenceRepository>();
        services.AddScoped<IManualMetricRepository, ManualMetricRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Redis nao configurada. Verifique appsettings ou variaveis de ambiente.");
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<IApplicationCacheService, RedisCacheService>();

        return services;
    }

    private static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AesEncryptionOptions>(configuration.GetSection("AesEncryption"));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ITokenEncryptor, AesTokenEncryptor>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

    private static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResendOptions>(configuration.GetSection("Resend"));
        services.Configure<EvolutionApiOptions>(configuration.GetSection("EvolutionApi"));
        services.Configure<ClaudeOptions>(configuration.GetSection("Claude"));

        services
            .AddRefitClient<IGoogleAnalyticsClient>()
            .ConfigureHttpClient(c =>
                c.BaseAddress = new Uri("https://analyticsdata.googleapis.com"))
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        ValidateRequiredApiKeys(configuration);

        services.AddTransient<ResendAuthHandler>();
        services.AddTransient<ClaudeAuthHandler>();
        services.AddTransient<EvolutionApiAuthHandler>();

        services.AddHttpClient("resend", c =>
            c.BaseAddress = new Uri("https://api.resend.com"))
            .AddHttpMessageHandler<ResendAuthHandler>()
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        services.AddHttpClient("claude", c =>
            c.BaseAddress = new Uri("https://api.anthropic.com"))
            .AddHttpMessageHandler<ClaudeAuthHandler>()
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        services.AddHttpClient("evolutionapi", (sp, c) =>
        {
            var opts = sp.GetRequiredService<IOptions<EvolutionApiOptions>>().Value;
            c.BaseAddress = new Uri(opts.BaseUrl);
        })
            .AddHttpMessageHandler<EvolutionApiAuthHandler>()
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        services.AddScoped<ChargeService>();
        services.AddScoped<IDataSourceProvider, GoogleAnalyticsProvider>();
        services.AddScoped<IDataSourceProvider, StripeDataProvider>();
        services.AddScoped<IDataSourceProvider, ManualDataProvider>();
        services.AddScoped<IDataAggregator, DataAggregator>();
        services.AddScoped<IInsightGenerator, ClaudeInsightGenerator>();
        services.AddScoped<IEmailSender, ResendEmailSender>();
        services.AddScoped<IWhatsAppSender, EvolutionApiWhatsAppSender>();

        return services;
    }

    private static void ValidateRequiredApiKeys(IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration["Resend:ApiKey"]))
        {
            throw new InvalidOperationException("Resend:ApiKey nao configurada. Verifique appsettings ou variaveis de ambiente.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Claude:ApiKey"]))
        {
            throw new InvalidOperationException("Claude:ApiKey nao configurada. Verifique appsettings ou variaveis de ambiente.");
        }

        if (string.IsNullOrWhiteSpace(configuration["EvolutionApi:ApiKey"]))
        {
            throw new InvalidOperationException("EvolutionApi:ApiKey nao configurada. Verifique appsettings ou variaveis de ambiente.");
        }

        if (string.IsNullOrWhiteSpace(configuration["EvolutionApi:BaseUrl"]))
        {
            throw new InvalidOperationException("EvolutionApi:BaseUrl nao configurada. Verifique appsettings ou variaveis de ambiente.");
        }
    }

    private static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(
                    configuration.GetConnectionString("Database")
                    ?? throw new InvalidOperationException("ConnectionStrings:Database nao configurada."))));

        services.AddHangfireServer();

        services.AddScoped<WeeklyReportGenerationJob>();
        services.AddScoped<ReportSendingJob>();
        services.AddScoped<IntegrationSyncJob>();
        services.AddScoped<TokenRefreshJob>();

        return services;
    }
}
