namespace WeeklyUp.Api.Extensions;

internal static class HealthChecksExtensions
{
    internal static IServiceCollection AddWeeklyUpHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("Database")!,
                name: "postgresql",
                tags: ["db", "ready"])
            .AddRedis(
                configuration.GetConnectionString("Redis")!,
                name: "redis",
                tags: ["cache", "ready"]);

        return services;
    }
}
