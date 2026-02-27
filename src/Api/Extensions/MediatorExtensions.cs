using WeeklyUp.Application.Common.Behaviors;

namespace WeeklyUp.Api.Extensions;

internal static class MediatorExtensions
{
    internal static IServiceCollection AddWeeklyUpMediator(this IServiceCollection services)
    {
        services.AddMediator(static options =>
            options.ServiceLifetime = ServiceLifetime.Scoped);

        // Order matters: Logging → Validation → Transaction
        services.AddScoped(typeof(Mediator.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(Mediator.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(Mediator.IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        return services;
    }
}
