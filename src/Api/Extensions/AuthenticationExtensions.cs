using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WeeklyUp.Shared.Constants;

namespace WeeklyUp.Api.Extensions;

internal static class AuthenticationExtensions
{
    internal static IServiceCollection AddWeeklyUpAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection jwt = configuration.GetSection("Jwt");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt["Key"]!)),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.ProPlan, policy =>
                policy.RequireClaim(CustomClaimTypes.Plan, "Pro", "Business"))
            .AddPolicy(Policies.BusinessPlan, policy =>
                policy.RequireClaim(CustomClaimTypes.Plan, "Business"));

        return services;
    }
}
