using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using WeeklyUp.Shared.Constants;

namespace WeeklyUp.Api.Extensions;

internal static class RateLimiterExtensions
{
    internal static IServiceCollection AddWeeklyUpRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("api", context =>
            {
                string? userId = context.User.FindFirst(CustomClaimTypes.UserId)?.Value;
                string partitionKey = userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                });
            });

            options.AddPolicy("webhook", _ =>
                RateLimitPartition.GetFixedWindowLimiter("stripe-webhook", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));
        });

        return services;
    }
}
