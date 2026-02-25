# 📊 WeeklyUp — 05. API, Testes e DevOps

---

## 1. API Layer (Minimal APIs + Carter)

**Regra:** Endpoints apenas delegam para MediatR. Zero lógica de negócio. Cada módulo Carter agrupa endpoints por feature.

### 1.1 Padrão de Response

```csharp
public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };
}

public sealed record ErrorResponse
{
    public bool Success => false;
    public required string Error { get; init; }
    public required string Code { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ErrorResponse From(string error, string code = "ERROR")
        => new() { Error = error, Code = code };
}
```

### 1.2 Result → IResult Extension (ponte entre Domain e Minimal APIs)

```csharp
// src/WeeklyUp.Api/Extensions/ResultExtensions.cs
public static class ResultExtensions
{
    public static IResult ToOk<T>(this Result<T> result)
        => result.Match(
            data => Results.Ok(ApiResponse<T>.Ok(data)),
            error => Results.BadRequest(ErrorResponse.From(error)));

    public static IResult ToCreated<T>(this Result<T> result, string? uri = null)
        => result.Match(
            data => Results.Created(uri, ApiResponse<T>.Ok(data)),
            error => Results.BadRequest(ErrorResponse.From(error)));

    public static IResult ToNotFound<T>(this Result<T> result)
        => result.Match(
            data => Results.Ok(ApiResponse<T>.Ok(data)),
            error => Results.NotFound(ErrorResponse.From(error)));

    public static IResult ToPlanRequired<T>(this Result<T> result)
        => result.Match(
            data => Results.Ok(ApiResponse<T>.Ok(data)),
            error => Results.Json(ErrorResponse.From(error, "PLAN_REQUIRED"), statusCode: 403));
}

// src/WeeklyUp.Api/Extensions/ClaimsPrincipalExtensions.cs
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

### 1.3 Carter Endpoint Modules

```csharp
// src/WeeklyUp.Api/Endpoints/DashboardEndpoints.cs
public sealed class DashboardEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .RequireAuthorization()
            .WithTags("Dashboard");

        group.MapGet("/", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetUserDashboardQuery(ctx.User.GetUserId()), ct);
            return result.ToNotFound();
        })
        .WithName("GetDashboard")
        .Produces<ApiResponse<UserDashboardResponse>>(200)
        .Produces<ErrorResponse>(404);
    }
}

// src/WeeklyUp.Api/Endpoints/ReportEndpoints.cs
public sealed class ReportEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .RequireAuthorization()
            .WithTags("Reports");

        group.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetWeeklyReportQuery(ctx.User.GetUserId(), id), ct);
            return result.ToNotFound();
        }).WithName("GetReport");

        group.MapGet("/history", async (int count, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetReportHistoryQuery(ctx.User.GetUserId(), count == 0 ? 12 : count), ct);
            return result.ToOk();
        }).WithName("GetReportHistory");

        group.MapGet("/{id:guid}/demographics", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetDemographicsDetailQuery(ctx.User.GetUserId(), id), ct);
            return result.ToPlanRequired();
        }).WithName("GetDemographics");

        group.MapPost("/manual", async (SubmitManualMetricsCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { UserId = ctx.User.GetUserId() }, ct);
            return result.ToCreated();
        }).WithName("SubmitManualMetrics");
    }
}

// src/WeeklyUp.Api/Endpoints/IntegrationEndpoints.cs
public sealed class IntegrationEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/integrations")
            .RequireAuthorization()
            .WithTags("Integrations");

        group.MapGet("/", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetUserIntegrationsQuery(ctx.User.GetUserId()), ct);
            return Results.Ok(ApiResponse<List<IntegrationResponse>>.Ok(result));
        }).WithName("GetIntegrations");

        group.MapPost("/google-analytics", async (ConnectGoogleAnalyticsCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { UserId = ctx.User.GetUserId() }, ct);
            return result.ToCreated();
        }).WithName("ConnectGA");

        group.MapPost("/stripe", async (ConnectStripeCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { UserId = ctx.User.GetUserId() }, ct);
            return result.ToCreated();
        }).WithName("ConnectStripe");

        group.MapDelete("/{provider}", async (IntegrationProvider provider, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DisconnectIntegrationCommand(ctx.User.GetUserId(), provider), ct);
            return result.Match(_ => Results.NoContent(), error => Results.BadRequest(ErrorResponse.From(error)));
        }).WithName("DisconnectIntegration");
    }
}

// src/WeeklyUp.Api/Endpoints/AuthEndpoints.cs
public sealed class AuthEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (RegisterUserCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.ToCreated();
        }).WithName("Register");

        group.MapPost("/login", async (LoginCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.ToOk();
        }).WithName("Login");

        group.MapPost("/google/callback", async (GoogleCallbackCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.ToOk();
        }).WithName("GoogleCallback");
    }
}

// src/WeeklyUp.Api/Endpoints/UserEndpoints.cs
public sealed class UserEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization()
            .WithTags("Users");

        group.MapGet("/me", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetUserProfileQuery(ctx.User.GetUserId()), ct);
            return result.ToNotFound();
        }).WithName("GetProfile");

        group.MapPut("/me", async (UpdateProfileCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { UserId = ctx.User.GetUserId() }, ct);
            return result.ToOk();
        }).WithName("UpdateProfile");

        group.MapPost("/upgrade", async (UpgradePlanCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { UserId = ctx.User.GetUserId() }, ct);
            return result.ToOk();
        }).WithName("UpgradePlan");
    }
}

// src/WeeklyUp.Api/Endpoints/WebhookEndpoints.cs
public sealed class WebhookEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks");

        group.MapPost("/stripe", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var json = await new StreamReader(ctx.Request.Body).ReadToEndAsync(ct);
            var result = await mediator.Send(new ProcessStripeWebhookCommand(json, ctx.Request.Headers["Stripe-Signature"]!), ct);
            return result.Match(_ => Results.Ok(), error => Results.BadRequest(ErrorResponse.From(error)));
        }).WithName("StripeWebhook");
    }
}
```

### 1.4 Exception Handling Middleware

```csharp
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            var (status, code) = ex switch
            {
                ValidationException => (400, "VALIDATION_ERROR"),
                NotFoundException => (404, "NOT_FOUND"),
                ForbiddenException => (403, "FORBIDDEN"),
                DomainException => (422, "DOMAIN_ERROR"),
                _ => (500, "INTERNAL_ERROR")
            };

            if (status == 500) _logger.LogError(ex, "Unhandled: {Msg}", ex.Message);

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Error = status == 500 ? "Erro interno." : ex.Message,
                Code = code
            });
        }
    }
}
```

### 1.5 Program.cs (.NET 10 + Minimal APIs + Carter + Scalar)

```csharp
using Carter;
using Hangfire;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ───────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// ─── Layers (Clean Architecture DI) ───────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ─── Carter (Minimal API modules) ─────────────────────────
builder.Services.AddCarter();

// ─── OpenAPI 3.1 (nativo no .NET 10, sem Swashbuckle) ─────
builder.Services.AddOpenApi();

// ─── Auth ──────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

// ─── Rate Limiting ─────────────────────────────────────────
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
    });
});

// ─── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.WithOrigins(builder.Configuration["Frontend:Url"]!)
     .AllowAnyHeader()
     .AllowAnyMethod()));

// ─── Health Checks ─────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

// ═══════════════════════════════════════════════════════════
var app = builder.Build();
// ═══════════════════════════════════════════════════════════

// ─── Middleware pipeline ───────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// ─── OpenAPI + Scalar (substituto do Swagger UI) ──────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                              // /openapi/v1.json
    app.MapScalarApiReference(options =>            // /scalar/v1
    {
        options.WithTitle("WeeklyUp API")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// ─── Carter (mapeia todos os ICarterModule automaticamente)
app.MapCarter();

// ─── Health ────────────────────────────────────────────────
app.MapHealthChecks("/health");

// ─── Hangfire ──────────────────────────────────────────────
app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<WeeklyReportJob>(
    "weekly-report",
    j => j.ExecuteAsync(CancellationToken.None),
    "0 7 * * 1",
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
    });

RecurringJob.AddOrUpdate<TokenRefreshJob>(
    "token-refresh",
    j => j.ExecuteAsync(CancellationToken.None),
    "0 */6 * * *");

app.Run();

// Necessário para WebApplicationFactory nos testes
public partial class Program;
```

---

## 2. Testes

### 2.1 Architecture Tests (NetArchTest)

```csharp
public sealed class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(User).Assembly;
    private static readonly Assembly Application = typeof(GenerateWeeklyReportCommand).Assembly;
    private static readonly Assembly Infrastructure = typeof(ApplicationDbContext).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact] public void Domain_Not_Depend_On_Application() =>
        Types.InAssembly(Domain).ShouldNot().HaveDependencyOn("WeeklyUp.Application").GetResult().IsSuccessful.Should().BeTrue();

    [Fact] public void Domain_Not_Depend_On_Infrastructure() =>
        Types.InAssembly(Domain).ShouldNot().HaveDependencyOn("WeeklyUp.Infrastructure").GetResult().IsSuccessful.Should().BeTrue();

    [Fact] public void Domain_Not_Depend_On_EFCore() =>
        Types.InAssembly(Domain).ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore").GetResult().IsSuccessful.Should().BeTrue();

    [Fact] public void Application_Not_Depend_On_Infrastructure() =>
        Types.InAssembly(Application).ShouldNot().HaveDependencyOn("WeeklyUp.Infrastructure").GetResult().IsSuccessful.Should().BeTrue();

    [Fact] public void Handlers_Should_Be_Sealed() =>
        Types.InAssembly(Application).That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().BeSealed().GetResult().IsSuccessful.Should().BeTrue();

    [Fact] public void Api_Should_Not_Reference_Domain_Entities_Directly() =>
        Types.InAssembly(Api).ShouldNot().HaveDependencyOn("WeeklyUp.Domain.Entities").GetResult().IsSuccessful.Should().BeTrue();

    [Fact] public void Endpoints_Should_Be_Sealed() =>
        Types.InAssembly(Api).That().ImplementInterface(typeof(ICarterModule))
            .Should().BeSealed().GetResult().IsSuccessful.Should().BeTrue();
}
```

### 2.2 Domain Tests (100% coverage)

```csharp
public sealed class UserTests
{
    [Fact]
    public void Create_ValidData_ReturnsSuccess()
    {
        var result = User.Create("joao@email.com", "João", "Loja do João", BusinessType.Ecommerce);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Plan.Should().Be(PlanType.Free);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_InvalidEmail_ReturnsFailure()
    {
        var result = User.Create("bad", "João", "Loja", BusinessType.Ecommerce);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AddIntegration_FreePlanSecondIntegration_Fails()
    {
        var user = CreateUser();
        var enc = Substitute.For<ITokenEncryptor>();
        enc.Encrypt(Arg.Any<string>()).Returns("enc");

        user.AddIntegration(IntegrationProvider.GoogleAnalytics, "t", "r", "a", "p", enc);
        var result = user.AddIntegration(IntegrationProvider.Stripe, "t", "r", "a", null, enc);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("máximo");
    }

    [Fact]
    public void CanAccessDemographics_Free_False() =>
        CreateUser().CanAccessDemographics().Should().BeFalse();

    [Fact]
    public void CanAccessDemographics_Pro_True()
    {
        var user = CreateUser();
        user.UpgradePlan(PlanType.Pro);
        user.CanAccessDemographics().Should().BeTrue();
    }

    [Fact]
    public void CanAccessWhatsApp_OnlyBusiness()
    {
        var user = CreateUser();
        user.CanAccessWhatsApp().Should().BeFalse();
        user.UpgradePlan(PlanType.Pro);
        user.CanAccessWhatsApp().Should().BeFalse();
        user.UpgradePlan(PlanType.Business);
        user.CanAccessWhatsApp().Should().BeTrue();
    }

    private static User CreateUser()
        => User.Create("test@test.com", "Test", "Loja", BusinessType.Ecommerce).Value!;
}

public sealed class DateRangeTests
{
    [Fact]
    public void Create_EndBeforeStart_Fails()
    {
        var result = DateRange.Create(new DateOnly(2026, 2, 20), new DateOnly(2026, 2, 10));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void PreviousWeek_ReturnsMonToSun()
    {
        var range = DateRange.PreviousWeek();
        range.Start.DayOfWeek.Should().Be(DayOfWeek.Monday);
        range.End.DayOfWeek.Should().Be(DayOfWeek.Sunday);
    }
}

public sealed class MoneyTests
{
    [Fact]
    public void Add_SameCurrency_Works()
    {
        var a = Money.BRL(100.50m);
        var b = Money.BRL(50.25m);
        a.Add(b).Amount.Should().Be(150.75m);
    }

    [Fact]
    public void ToFormatted_ShowsBRL()
    {
        Money.BRL(4350).ToFormattedString().Should().Be("R$ 4.350,00");
    }
}

public sealed class PercentageTests
{
    [Fact]
    public void CalculateChange_Growth()
    {
        var p = Percentage.CalculateChange(112, 100);
        p.Value.Should().Be(12);
        p.IsPositive.Should().BeTrue();
        p.Arrow.Should().Be("↑");
    }

    [Fact]
    public void CalculateChange_Decline()
    {
        var p = Percentage.CalculateChange(85, 100);
        p.IsNegative.Should().BeTrue();
    }
}
```

### 2.3 Application Tests (Handler)

```csharp
public sealed class GenerateWeeklyReportTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDataAggregator _aggregator = Substitute.For<IDataAggregator>();
    private readonly IInsightGenerator _insights = Substitute.For<IInsightGenerator>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _uow.Users.GetByIdWithIntegrationsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GenerateWeeklyReportCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("não encontrado");
    }

    [Fact]
    public async Task Handle_ValidUser_CreatesReport()
    {
        var user = User.Create("test@test.com", "Test", "Loja", BusinessType.Ecommerce).Value!;
        _uow.Users.GetByIdWithIntegrationsAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _uow.Reports.GetByUserAndWeekAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Report?)null);
        _aggregator.AggregateMetrics(Arg.Any<IEnumerable<CollectedMetrics>>())
            .Returns(CreateTestMetrics());

        var handler = CreateHandler();
        var result = await handler.Handle(new GenerateWeeklyReportCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _uow.Reports.Received(1).AddAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private GenerateWeeklyReportCommandHandler CreateHandler()
        => new(_uow, Enumerable.Empty<IDataSourceProvider>(), _aggregator, _insights,
            _dateTime, Substitute.For<ILogger<GenerateWeeklyReportCommandHandler>>());

    private static ReportMetrics CreateTestMetrics()
        => new(Money.BRL(4350), 23, Money.BRL(189.13m), 5, 1240, 980, 3200, "/produto-x", "instagram");
}
```

### 2.4 API Integration Tests (WebApplicationFactory + Minimal APIs)

```csharp
// Funciona igual com Minimal APIs — WebApplicationFactory detecta o Program.cs
public sealed class ReportEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReportEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                // Replace DB with in-memory for tests
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetReport_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync($"/api/reports/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

## 3. Docker

```yaml
# docker/docker-compose.yml
services:
  api:
    build:
      context: ../
      dockerfile: src/WeeklyUp.Api/Dockerfile
    ports: ["5000:8080"]
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=weeklyup;Username=wu_user;Password=wu_secret"
      ConnectionStrings__Redis: "redis:6379"
    depends_on:
      postgres: { condition: service_healthy }
      redis: { condition: service_started }

  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: weeklyup
      POSTGRES_USER: wu_user
      POSTGRES_PASSWORD: wu_secret
    ports: ["5432:5432"]
    volumes: [postgres_data:/var/lib/postgresql/data]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U wu_user -d weeklyup"]
      interval: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  seq:
    image: datalust/seq:latest
    environment: { ACCEPT_EULA: "Y" }
    ports: ["5341:80"]

volumes:
  postgres_data:
```

```dockerfile
# src/WeeklyUp.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/WeeklyUp.Domain/WeeklyUp.Domain.csproj", "src/WeeklyUp.Domain/"]
COPY ["src/WeeklyUp.Application/WeeklyUp.Application.csproj", "src/WeeklyUp.Application/"]
COPY ["src/WeeklyUp.Infrastructure/WeeklyUp.Infrastructure.csproj", "src/WeeklyUp.Infrastructure/"]
COPY ["src/WeeklyUp.Api/WeeklyUp.Api.csproj", "src/WeeklyUp.Api/"]
RUN dotnet restore "src/WeeklyUp.Api/WeeklyUp.Api.csproj"
COPY . .
RUN dotnet publish "src/WeeklyUp.Api/WeeklyUp.Api.csproj" -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WeeklyUp.Api.dll"]
```

---

## 4. CI/CD (GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: CI
on:
  pull_request: { branches: [main, develop] }
  push: { branches: [main] }

jobs:
  build-test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:17-alpine
        env: { POSTGRES_DB: wu_test, POSTGRES_USER: test, POSTGRES_PASSWORD: test }
        ports: ["5432:5432"]
        options: --health-cmd pg_isready --health-interval 10s --health-retries 5

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }

      - run: dotnet restore
      - run: dotnet build --no-restore -c Release

      - name: Architecture Tests
        run: dotnet test tests/WeeklyUp.Architecture.Tests/ --no-build -c Release

      - name: Domain Tests (100% coverage)
        run: dotnet test tests/WeeklyUp.Domain.Tests/ --no-build -c Release --collect:"XPlat Code Coverage"

      - name: Application Tests (>80% coverage)
        run: dotnet test tests/WeeklyUp.Application.Tests/ --no-build -c Release

      - name: Integration Tests
        run: dotnet test tests/WeeklyUp.Infrastructure.Tests/ --no-build -c Release
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=wu_test;Username=test;Password=test"

      - name: API Tests
        run: dotnet test tests/WeeklyUp.Api.Tests/ --no-build -c Release
```

---

## 5. Resumo dos Endpoints

| Método | Rota | Módulo Carter | Auth |
|--------|------|---------------|------|
| POST | /api/auth/register | AuthEndpoints | ❌ |
| POST | /api/auth/login | AuthEndpoints | ❌ |
| POST | /api/auth/google/callback | AuthEndpoints | ❌ |
| GET | /api/users/me | UserEndpoints | ✅ |
| PUT | /api/users/me | UserEndpoints | ✅ |
| POST | /api/users/upgrade | UserEndpoints | ✅ |
| GET | /api/dashboard | DashboardEndpoints | ✅ |
| GET | /api/reports/{id} | ReportEndpoints | ✅ |
| GET | /api/reports/history | ReportEndpoints | ✅ |
| GET | /api/reports/{id}/demographics | ReportEndpoints | ✅ |
| POST | /api/reports/manual | ReportEndpoints | ✅ |
| GET | /api/integrations | IntegrationEndpoints | ✅ |
| POST | /api/integrations/google-analytics | IntegrationEndpoints | ✅ |
| POST | /api/integrations/stripe | IntegrationEndpoints | ✅ |
| DELETE | /api/integrations/{provider} | IntegrationEndpoints | ✅ |
| POST | /api/webhooks/stripe | WebhookEndpoints | ❌ |
| GET | /health | Built-in | ❌ |
| GET | /openapi/v1.json | Built-in (.NET 10) | ❌ |
| GET | /scalar/v1 | Scalar (dev only) | ❌ |

---

*Próximo documento: [06-Cronograma-Checklist.md] — Roadmap semanal detalhado com checklist*
