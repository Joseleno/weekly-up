using Carter;

using Hangfire;

using HealthChecks.UI.Client;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

using Serilog;

using System.Text;
using System.Threading.RateLimiting;

using WeeklyUp.Api.Middleware;
using WeeklyUp.Application;
using WeeklyUp.Infrastructure;
using WeeklyUp.Infrastructure.BackgroundJobs;
using WeeklyUp.Shared.Constants;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

// Register Mediator pipeline behaviors (order matters: Logging → Validation → Transaction)
builder.Services.AddScoped(
    typeof(Mediator.IPipelineBehavior<,>),
    typeof(WeeklyUp.Application.Common.Behaviors.LoggingBehavior<,>));
builder.Services.AddScoped(
    typeof(Mediator.IPipelineBehavior<,>),
    typeof(WeeklyUp.Application.Common.Behaviors.ValidationBehavior<,>));
builder.Services.AddScoped(
    typeof(Mediator.IPipelineBehavior<,>),
    typeof(WeeklyUp.Application.Common.Behaviors.TransactionBehavior<,>));

builder.Services.AddCarter(configurator: c =>
{
    c.WithModule<WeeklyUp.Api.Modules.AuthModule>();
    c.WithModule<WeeklyUp.Api.Modules.UsersModule>();
    c.WithModule<WeeklyUp.Api.Modules.IntegrationsModule>();
    c.WithModule<WeeklyUp.Api.Modules.MetricsModule>();
    c.WithModule<WeeklyUp.Api.Modules.ReportsModule>();
});

builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        IConfigurationSection jwt = builder.Configuration.GetSection("Jwt");
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.ProPlan, policy =>
        policy.RequireClaim(CustomClaimTypes.Plan, "Pro", "Business"));
    options.AddPolicy(Policies.BusinessPlan, policy =>
        policy.RequireClaim(CustomClaimTypes.Plan, "Business"));
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Database")!,
        name: "postgresql",
        tags: ["db", "ready"])
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: ["cache", "ready"]);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 60;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Recurring jobs — toda segunda-feira às 7h (horário de Brasília) e token refresh a cada 6h
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
recurringJobs.AddOrUpdate<WeeklyReportGenerationJob>(
    recurringJobId: "weekly-report-generation",
    methodCall: j => j.ExecuteAsync(CancellationToken.None),
    cronExpression: "0 7 * * 1",
    options: new RecurringJobOptions { TimeZone = brasiliaTimeZone });

recurringJobs.AddOrUpdate<TokenRefreshJob>(
    recurringJobId: "integration-token-refresh",
    methodCall: j => j.ExecuteAsync(CancellationToken.None),
    cronExpression: "0 */6 * * *");

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});

try
{
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Required for WebApplicationFactory in E2E tests
public partial class Program { }
