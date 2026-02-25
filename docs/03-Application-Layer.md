# 📊 WeeklyUp — 03. Application Layer

> **Regra:** Application depende APENAS do Domain. Sem EF Core, sem HttpClient, sem detalhes de infraestrutura.

---

## 1. MediatR Pipeline Behaviors

Executados em ordem para TODOS os commands/queries que passam pelo pipeline:

```
Request → ValidationBehavior → LoggingBehavior → PerformanceBehavior → Handler → Response
```

### 1.1 ValidationBehavior

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### 1.2 LoggingBehavior

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUser;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {Request} for User {UserId}", requestName, _currentUser.UserId);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
            _logger.LogWarning("SLOW: {Request} took {Ms}ms", requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
```

---

## 2. Application Interfaces (cross-cutting)

```csharp
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public interface IDataAggregator
{
    ReportMetrics AggregateMetrics(IEnumerable<CollectedMetrics> sources);
    Demographics? AggregateDemographics(IEnumerable<DemographicData> sources);
}
```

---

## 3. Core Handlers

### 3.1 GenerateWeeklyReport (o handler mais importante)

```csharp
// Command
public sealed record GenerateWeeklyReportCommand(Guid UserId) : IRequest<Result<ReportDto>>;

// Validator
public sealed class GenerateWeeklyReportCommandValidator
    : AbstractValidator<GenerateWeeklyReportCommand>
{
    public GenerateWeeklyReportCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId é obrigatório.");
    }
}

// Handler
public sealed class GenerateWeeklyReportCommandHandler
    : IRequestHandler<GenerateWeeklyReportCommand, Result<ReportDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IEnumerable<IDataSourceProvider> _providers;
    private readonly IDataAggregator _aggregator;
    private readonly IInsightGenerator _insightGenerator;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<GenerateWeeklyReportCommandHandler> _logger;

    public GenerateWeeklyReportCommandHandler(
        IUnitOfWork uow, IEnumerable<IDataSourceProvider> providers,
        IDataAggregator aggregator, IInsightGenerator insightGenerator,
        IDateTimeProvider dateTime, ILogger<GenerateWeeklyReportCommandHandler> logger)
    {
        _uow = uow; _providers = providers; _aggregator = aggregator;
        _insightGenerator = insightGenerator; _dateTime = dateTime; _logger = logger;
    }

    public async Task<Result<ReportDto>> Handle(
        GenerateWeeklyReportCommand command, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdWithIntegrationsAsync(command.UserId, ct);
        if (user is null)
            return Result<ReportDto>.Failure("Usuário não encontrado.");

        var weekRange = DateRange.PreviousWeek();

        var existing = await _uow.Reports.GetByUserAndWeekAsync(user.Id, weekRange.Start, ct);
        if (existing is not null)
            return Result<ReportDto>.Failure("Relatório desta semana já foi gerado.");

        // Coletar de todas as integrações ativas em paralelo
        var collections = await CollectFromAllProvidersAsync(user, weekRange, ct);

        var metrics = _aggregator.AggregateMetrics(
            collections.Where(c => c.Metrics is not null).Select(c => c.Metrics!));

        // Enriquecer com dados comparativos da semana anterior
        var previousReport = await _uow.Reports.GetLatestAsync(user.Id, ct);
        if (previousReport is not null)
        {
            metrics = metrics with
            {
                PreviousRevenue = previousReport.Metrics.Revenue,
                PreviousVisits = previousReport.Metrics.TotalVisits
            };
        }

        // Demographics (apenas Pro+)
        Demographics? demographics = null;
        if (user.CanAccessDemographics())
        {
            demographics = _aggregator.AggregateDemographics(
                collections.Where(c => c.Demographics is not null).Select(c => c.Demographics!));
        }

        var report = Report.Create(user.Id, weekRange, metrics, demographics);

        // Insights com IA (apenas Pro+)
        if (user.CanAccessInsights())
        {
            var insights = await _insightGenerator.GenerateInsightsAsync(
                metrics, demographics, previousReport?.Metrics,
                user.BusinessType, ct);
            report.AddInsights(insights);
        }

        await _uow.Reports.AddAsync(report, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<ReportDto>.Success(report.Adapt<ReportDto>());
    }

    private async Task<List<CollectionResult>> CollectFromAllProvidersAsync(
        User user, DateRange range, CancellationToken ct)
    {
        var tasks = user.Integrations
            .Where(i => i.Status == IntegrationStatus.Active)
            .Select(async integration =>
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderType == integration.Provider);
                if (provider is null) return new CollectionResult(null, null);

                try
                {
                    var metrics = await provider.CollectMetricsAsync(integration, range, ct);
                    var demographics = await provider.CollectDemographicsAsync(integration, range, ct);
                    return new CollectionResult(metrics, demographics);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to collect from {Provider}", integration.Provider);
                    return new CollectionResult(null, null);
                }
            });

        return (await Task.WhenAll(tasks)).ToList();
    }

    private sealed record CollectionResult(CollectedMetrics? Metrics, DemographicData? Demographics);
}
```

### 3.2 SendReportEmail

```csharp
public sealed record SendReportEmailCommand(Guid UserId, Guid ReportId) : IRequest<Result<bool>>;

public sealed class SendReportEmailCommandHandler
    : IRequestHandler<SendReportEmailCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;

    public SendReportEmailCommandHandler(IUnitOfWork uow, IEmailSender emailSender)
    {
        _uow = uow; _emailSender = emailSender;
    }

    public async Task<Result<bool>> Handle(SendReportEmailCommand command, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<bool>.Failure("Usuário não encontrado.");

        var report = await _uow.Reports.GetByIdAsync(command.ReportId, ct);
        if (report is null) return Result<bool>.Failure("Relatório não encontrado.");

        if (report.WasEmailSent) return Result<bool>.Failure("Email já foi enviado.");

        var result = await _emailSender.SendWeeklyReportAsync(
            user.Email, user.Name, report, ct);

        if (result.IsSuccess)
        {
            report.MarkEmailSent();
            await _uow.SaveChangesAsync(ct);
        }

        return result;
    }
}
```

### 3.3 SendReportWhatsApp

```csharp
public sealed record SendReportWhatsAppCommand(Guid UserId, Guid ReportId) : IRequest<Result<bool>>;

public sealed class SendReportWhatsAppCommandHandler
    : IRequestHandler<SendReportWhatsAppCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly IWhatsAppSender _whatsApp;

    public SendReportWhatsAppCommandHandler(IUnitOfWork uow, IWhatsAppSender whatsApp)
    {
        _uow = uow; _whatsApp = whatsApp;
    }

    public async Task<Result<bool>> Handle(SendReportWhatsAppCommand command, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<bool>.Failure("Usuário não encontrado.");

        if (!user.CanAccessWhatsApp())
            return Result<bool>.Failure("WhatsApp disponível apenas no plano Business.");

        if (string.IsNullOrEmpty(user.PhoneNumber))
            return Result<bool>.Failure("Número de WhatsApp não cadastrado.");

        var report = await _uow.Reports.GetByIdAsync(command.ReportId, ct);
        if (report is null) return Result<bool>.Failure("Relatório não encontrado.");

        var result = await _whatsApp.SendWeeklyReportAsync(
            user.PhoneNumber, user.Name, report, ct);

        if (result.IsSuccess)
        {
            report.MarkWhatsAppSent();
            await _uow.SaveChangesAsync(ct);
        }

        return result;
    }
}
```

### 3.4 Dashboard Query

```csharp
public sealed record GetUserDashboardQuery(Guid UserId) : IRequest<Result<UserDashboardResponse>>;

public sealed record UserDashboardResponse
{
    public required CurrentWeekSummary CurrentWeek { get; init; }
    public required CurrentWeekSummary? PreviousWeek { get; init; }
    public required DemographicsDto? Demographics { get; init; }
    public required InsightsDto? Insights { get; init; }
    public required List<WeeklyTrendPoint> RevenueTrend { get; init; }
    public required List<WeeklyTrendPoint> TrafficTrend { get; init; }
    public required PlanType UserPlan { get; init; }
}

public sealed record CurrentWeekSummary
{
    public required string WeekLabel { get; init; }
    public required decimal Revenue { get; init; }
    public required string RevenueFormatted { get; init; }
    public required decimal? RevenueChangePercent { get; init; }
    public required int SalesCount { get; init; }
    public required decimal AverageTicket { get; init; }
    public required int TotalVisits { get; init; }
    public required decimal? TrafficChangePercent { get; init; }
    public required string? TopPage { get; init; }
    public required string? TopTrafficSource { get; init; }
}

public sealed record WeeklyTrendPoint(string WeekLabel, decimal Value);

public sealed class GetUserDashboardQueryHandler
    : IRequestHandler<GetUserDashboardQuery, Result<UserDashboardResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;

    public GetUserDashboardQueryHandler(IUnitOfWork uow, ICacheService cache)
    {
        _uow = uow; _cache = cache;
    }

    public async Task<Result<UserDashboardResponse>> Handle(
        GetUserDashboardQuery query, CancellationToken ct)
    {
        var cacheKey = $"dashboard:{query.UserId}";
        var cached = await _cache.GetAsync<UserDashboardResponse>(cacheKey, ct);
        if (cached is not null) return Result<UserDashboardResponse>.Success(cached);

        var user = await _uow.Users.GetByIdAsync(query.UserId, ct);
        if (user is null) return Result<UserDashboardResponse>.Failure("Usuário não encontrado.");

        var reports = await _uow.Reports.GetHistoryAsync(user.Id, 12, ct);
        var latest = reports.FirstOrDefault();
        var previous = reports.Skip(1).FirstOrDefault();

        var response = new UserDashboardResponse
        {
            CurrentWeek = MapToSummary(latest),
            PreviousWeek = previous is not null ? MapToSummary(previous) : null,
            Demographics = user.CanAccessDemographics()
                ? latest?.Demographics?.Adapt<DemographicsDto>() : null,
            Insights = user.CanAccessInsights()
                ? latest?.Insights?.Adapt<InsightsDto>() : null,
            RevenueTrend = reports.OrderBy(r => r.WeekRange.Start)
                .Select(r => new WeeklyTrendPoint(r.WeekRange.ToString(), r.Metrics.Revenue.Amount))
                .ToList(),
            TrafficTrend = reports.OrderBy(r => r.WeekRange.Start)
                .Select(r => new WeeklyTrendPoint(r.WeekRange.ToString(), r.Metrics.TotalVisits))
                .ToList(),
            UserPlan = user.Plan
        };

        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30), ct);
        return Result<UserDashboardResponse>.Success(response);
    }

    private static CurrentWeekSummary MapToSummary(Report? report)
    {
        if (report is null)
            return new CurrentWeekSummary
            {
                WeekLabel = "Sem dados", Revenue = 0, RevenueFormatted = "R$ 0,00",
                RevenueChangePercent = null, SalesCount = 0, AverageTicket = 0,
                TotalVisits = 0, TrafficChangePercent = null, TopPage = null, TopTrafficSource = null
            };

        return new CurrentWeekSummary
        {
            WeekLabel = report.WeekRange.ToString(),
            Revenue = report.Metrics.Revenue.Amount,
            RevenueFormatted = report.Metrics.Revenue.ToFormattedString(),
            RevenueChangePercent = report.RevenueChange?.Value,
            SalesCount = report.Metrics.SalesCount,
            AverageTicket = report.Metrics.AverageTicket.Amount,
            TotalVisits = report.Metrics.TotalVisits,
            TrafficChangePercent = report.TrafficChange?.Value,
            TopPage = report.Metrics.TopPage,
            TopTrafficSource = report.Metrics.TopTrafficSource
        };
    }
}
```

### 3.5 SubmitManualMetrics

```csharp
public sealed record SubmitManualMetricsCommand(
    Guid UserId, DateOnly WeekStart,
    decimal? Revenue, int? SalesCount,
    int? NewCustomers, int? Visits
) : IRequest<Result<bool>>;

public sealed class SubmitManualMetricsCommandValidator
    : AbstractValidator<SubmitManualMetricsCommand>
{
    public SubmitManualMetricsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.WeekStart).NotEmpty();
        RuleFor(x => x.Revenue).GreaterThanOrEqualTo(0).When(x => x.Revenue.HasValue);
        RuleFor(x => x.SalesCount).GreaterThanOrEqualTo(0).When(x => x.SalesCount.HasValue);
    }
}

public sealed class SubmitManualMetricsCommandHandler
    : IRequestHandler<SubmitManualMetricsCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public SubmitManualMetricsCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<bool>> Handle(SubmitManualMetricsCommand cmd, CancellationToken ct)
    {
        var existing = await _uow.ManualMetrics.GetByUserAndWeekAsync(cmd.UserId, cmd.WeekStart, ct);

        if (existing is not null)
        {
            existing.Update(cmd.Revenue, cmd.SalesCount, cmd.NewCustomers, cmd.Visits);
            _uow.ManualMetrics.Update(existing);
        }
        else
        {
            var metric = ManualMetric.Create(
                cmd.UserId, cmd.WeekStart, cmd.Revenue, cmd.SalesCount, cmd.NewCustomers, cmd.Visits);
            await _uow.ManualMetrics.AddAsync(metric, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
```

---

## 4. DTOs

```csharp
public sealed record ReportDto
{
    public Guid Id { get; init; }
    public string WeekLabel { get; init; } = "";
    public MetricsDto Metrics { get; init; } = default!;
    public DemographicsDto? Demographics { get; init; }
    public InsightsDto? Insights { get; init; }
    public bool EmailSent { get; init; }
    public bool WhatsAppSent { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record MetricsDto
{
    public decimal Revenue { get; init; }
    public string RevenueFormatted { get; init; } = "";
    public decimal? RevenueChangePercent { get; init; }
    public string? RevenueChangeArrow { get; init; }
    public int SalesCount { get; init; }
    public decimal AverageTicket { get; init; }
    public int NewCustomers { get; init; }
    public int TotalVisits { get; init; }
    public decimal? TrafficChangePercent { get; init; }
    public string? TopPage { get; init; }
    public string? TopTrafficSource { get; init; }
}

public sealed record DemographicsDto
{
    public GenderDto Gender { get; init; } = default!;
    public Dictionary<string, decimal> AgeGroups { get; init; } = new();
    public List<LocationDto> TopCities { get; init; } = new();
    public List<LocationDto> TopStates { get; init; } = new();
    public DeviceDto Devices { get; init; } = default!;
    public Dictionary<string, decimal> TrafficSources { get; init; } = new();
}

public sealed record GenderDto(decimal Female, decimal Male);
public sealed record DeviceDto(decimal Mobile, decimal Desktop, decimal Tablet);
public sealed record LocationDto(string Name, decimal Percentage);

public sealed record InsightsDto
{
    public string Highlight { get; init; } = "";
    public string Alert { get; init; } = "";
    public string Tip { get; init; } = "";
}
```

---

## 5. Event Handlers

```csharp
// Quando um relatório é gerado, invalida o cache do dashboard
public sealed class ReportGeneratedEventHandler
    : INotificationHandler<ReportGeneratedEvent>
{
    private readonly ICacheService _cache;

    public ReportGeneratedEventHandler(ICacheService cache) => _cache = cache;

    public async Task Handle(ReportGeneratedEvent notification, CancellationToken ct)
    {
        await _cache.RemoveAsync($"dashboard:{notification.UserId}", ct);
    }
}
```

---

## 6. Dependency Injection Registration

```csharp
// src/WeeklyUp.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        TypeAdapterConfig.GlobalSettings.Scan(assembly);

        return services;
    }
}
```

---

*Próximo documento: [04-Infrastructure-Layer.md] — EF Core, Providers, Email, WhatsApp, Jobs*
