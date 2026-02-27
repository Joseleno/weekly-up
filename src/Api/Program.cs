using Carter;
using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using WeeklyUp.Api.Extensions;
using WeeklyUp.Api.Middleware;
using WeeklyUp.Application;
using WeeklyUp.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWeeklyUpMediator();
builder.Services.AddWeeklyUpAuthentication(builder.Configuration);
builder.Services.AddWeeklyUpRateLimiter();
builder.Services.AddWeeklyUpCors(builder.Configuration, builder.Environment);
builder.Services.AddWeeklyUpHealthChecks(builder.Configuration);

builder.Services.AddCarter(configurator: c =>
{
    c.WithModule<WeeklyUp.Api.Modules.AuthModule>();
    c.WithModule<WeeklyUp.Api.Modules.UsersModule>();
    c.WithModule<WeeklyUp.Api.Modules.IntegrationsModule>();
    c.WithModule<WeeklyUp.Api.Modules.MetricsModule>();
    c.WithModule<WeeklyUp.Api.Modules.ReportsModule>();
    c.WithModule<WeeklyUp.Api.Modules.BillingModule>();
    c.WithModule<WeeklyUp.Api.Modules.WebhooksModule>();
});

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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

app.MapRecurringJobs();

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
public partial class Program
{
    protected Program() { }
}
