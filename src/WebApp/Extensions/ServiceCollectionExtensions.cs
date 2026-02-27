using WeeklyUp.WebApp.Handlers;
using WeeklyUp.WebApp.Services;

namespace WeeklyUp.WebApp.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeeklyUpHttpClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseAddress = configuration["Api:BaseAddress"] ?? "https://localhost:5001";

        services.AddScoped<JwtDelegatingHandler>();
        services.AddScoped<IApiClient, ApiClient>();
        services.AddScoped<AuthService>();

        services.AddHttpClient("WeeklyUp.Api", client =>
        {
            client.BaseAddress = new Uri(baseAddress);
        })
        .AddHttpMessageHandler<JwtDelegatingHandler>();

        services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("WeeklyUp.Api"));

        return services;
    }
}
