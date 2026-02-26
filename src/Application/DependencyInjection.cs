using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace WeeklyUp.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;
        services.AddValidatorsFromAssembly(assembly);
        // Mediator is registered via source generator — AddMediator() in Program.cs
        // Pipeline behaviors are registered via Mediator options in Infrastructure/API layer
        return services;
    }
}
