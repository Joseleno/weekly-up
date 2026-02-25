# 📊 WeeklyUp — 04. Infrastructure Layer

> **Regra:** Infrastructure implementa interfaces do Domain. Contém TODOS os detalhes de tecnologia.

---

## 1. Persistence (EF Core 10 + PostgreSQL)

### 1.1 DbContext

```csharp
public sealed class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportPreference> ReportPreferences => Set<ReportPreference>();
    public DbSet<ManualMetric> ManualMetrics => Set<ManualMetric>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### 1.2 Entity Configurations (Fluent API)

```csharp
// UserConfiguration.cs
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("email").HasMaxLength(255).IsRequired();
            email.HasIndex(e => e.Value).IsUnique();
        });

        builder.OwnsOne(u => u.BusinessName, bn =>
        {
            bn.Property(b => b.Value).HasColumnName("business_name").HasMaxLength(255);
        });

        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(u => u.BusinessType).HasColumnName("business_type").HasConversion<string>();
        builder.Property(u => u.Timezone).HasColumnName("timezone").HasMaxLength(50).HasDefaultValue("America/Sao_Paulo");
        builder.Property(u => u.Plan).HasColumnName("plan").HasConversion<string>().HasDefaultValue(PlanType.Free);
        builder.Property(u => u.ExternalAuthId).HasColumnName("external_auth_id").HasMaxLength(255);
        builder.Property(u => u.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(u => u.Integrations).WithOne().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(u => u.DomainEvents);
    }
}

// ReportConfiguration.cs
public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();

        builder.OwnsOne(r => r.WeekRange, range =>
        {
            range.Property(d => d.Start).HasColumnName("week_start").IsRequired();
            range.Property(d => d.End).HasColumnName("week_end").IsRequired();
        });

        // JSONB columns para dados complexos
        builder.OwnsOne(r => r.Metrics, m => m.ToJson("metrics"));
        builder.OwnsOne(r => r.Demographics, d => d.ToJson("demographics"));
        builder.OwnsOne(r => r.Insights, i => i.ToJson("insights"));

        builder.Property(r => r.EmailSentAt).HasColumnName("email_sent_at");
        builder.Property(r => r.EmailOpenedAt).HasColumnName("email_opened_at");
        builder.Property(r => r.WhatsAppSentAt).HasColumnName("whatsapp_sent_at");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => new { r.UserId, r.CreatedAt })
            .IsDescending(false, true).HasDatabaseName("ix_reports_user_created");

        builder.Ignore(r => r.DomainEvents);
    }
}
```

### 1.3 UnitOfWork

```csharp
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IUserRepository Users { get; }
    public IIntegrationRepository Integrations { get; }
    public IReportRepository Reports { get; }
    public IManualMetricRepository ManualMetrics { get; }

    public UnitOfWork(
        ApplicationDbContext context,
        IUserRepository users, IIntegrationRepository integrations,
        IReportRepository reports, IManualMetricRepository manualMetrics)
    {
        _context = context;
        Users = users; Integrations = integrations;
        Reports = reports; ManualMetrics = manualMetrics;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Dispatch domain events before saving
        var entities = _context.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity).ToList();

        var result = await _context.SaveChangesAsync(ct);

        // Domain events dispatched via MediatR after save (eventual consistency)
        return result;
    }

    public void Dispose() => _context.Dispose();
}
```

### 1.4 Repository Example

```csharp
public sealed class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context) => _context = context;

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Reports.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Report?> GetByUserAndWeekAsync(Guid userId, DateOnly weekStart, CancellationToken ct)
        => await _context.Reports
            .FirstOrDefaultAsync(r => r.UserId == userId && r.WeekRange.Start == weekStart, ct);

    public async Task<IReadOnlyList<Report>> GetHistoryAsync(Guid userId, int count, CancellationToken ct)
        => await _context.Reports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<Report?> GetLatestAsync(Guid userId, CancellationToken ct)
        => await _context.Reports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Report report, CancellationToken ct)
        => await _context.Reports.AddAsync(report, ct);

    public void Update(Report report) => _context.Reports.Update(report);
}
```

### 1.5 Auditable Interceptor

```csharp
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, ct);

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.GetType().GetProperty("UpdatedAt")?.SetValue(entry.Entity, DateTime.UtcNow);
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

---

## 2. Data Source Providers (Strategy Pattern)

### 2.1 Google Analytics 4

```csharp
// Refit interface para GA4 Data API
public interface IGoogleAnalyticsClient
{
    [Post("/v1beta/{propertyId}:runReport")]
    Task<GA4ReportResponse> RunReportAsync(
        [AliasAs("propertyId")] string propertyId,
        [Header("Authorization")] string bearerToken,
        [Body] GA4ReportRequest request,
        CancellationToken ct = default);
}

// Provider
public sealed class GoogleAnalyticsProvider : IDataSourceProvider
{
    private readonly IGoogleAnalyticsClient _client;
    private readonly ITokenEncryptor _encryptor;
    private readonly ILogger<GoogleAnalyticsProvider> _logger;

    public IntegrationProvider ProviderType => IntegrationProvider.GoogleAnalytics;

    public GoogleAnalyticsProvider(
        IGoogleAnalyticsClient client, ITokenEncryptor encryptor,
        ILogger<GoogleAnalyticsProvider> logger)
    {
        _client = client; _encryptor = encryptor; _logger = logger;
    }

    public async Task<CollectedMetrics> CollectMetricsAsync(
        Integration integration, DateRange range, CancellationToken ct)
    {
        var token = $"Bearer {_encryptor.Decrypt(integration.AccessToken)}";
        var property = $"properties/{integration.PropertyId}";

        var response = await _client.RunReportAsync(property, token,
            new GA4ReportRequest
            {
                DateRanges = new[] { new GA4DateRange(range.Start.ToString("yyyy-MM-dd"), range.End.ToString("yyyy-MM-dd")) },
                Metrics = new[] { new GA4Metric("sessions"), new GA4Metric("screenPageViews"), new GA4Metric("newUsers") },
                Dimensions = new[] { new GA4Dimension("pagePath"), new GA4Dimension("sessionSource") }
            }, ct);

        integration.MarkSynced();
        return GA4Mapper.ToCollectedMetrics(response);
    }

    public async Task<DemographicData?> CollectDemographicsAsync(
        Integration integration, DateRange range, CancellationToken ct)
    {
        var token = $"Bearer {_encryptor.Decrypt(integration.AccessToken)}";
        var property = $"properties/{integration.PropertyId}";

        var response = await _client.RunReportAsync(property, token,
            new GA4ReportRequest
            {
                DateRanges = new[] { new GA4DateRange(range.Start.ToString("yyyy-MM-dd"), range.End.ToString("yyyy-MM-dd")) },
                Metrics = new[] { new GA4Metric("sessions") },
                Dimensions = new[]
                {
                    new GA4Dimension("userGender"), new GA4Dimension("userAgeBracket"),
                    new GA4Dimension("city"), new GA4Dimension("region"),
                    new GA4Dimension("deviceCategory"), new GA4Dimension("sessionSource")
                }
            }, ct);

        return GA4Mapper.ToDemographicData(response);
    }

    public async Task<bool> ValidateConnectionAsync(Integration integration, CancellationToken ct)
    {
        try
        {
            var token = $"Bearer {_encryptor.Decrypt(integration.AccessToken)}";
            await _client.RunReportAsync($"properties/{integration.PropertyId}", token,
                new GA4ReportRequest { DateRanges = new[] { new GA4DateRange("yesterday", "yesterday") },
                    Metrics = new[] { new GA4Metric("sessions") } }, ct);
            return true;
        }
        catch { return false; }
    }
}
```

### 2.2 Stripe

```csharp
public sealed class StripeDataProvider : IDataSourceProvider
{
    private readonly ITokenEncryptor _encryptor;
    public IntegrationProvider ProviderType => IntegrationProvider.Stripe;

    public StripeDataProvider(ITokenEncryptor encryptor) => _encryptor = encryptor;

    public async Task<CollectedMetrics> CollectMetricsAsync(
        Integration integration, DateRange range, CancellationToken ct)
    {
        var apiKey = _encryptor.Decrypt(integration.AccessToken);
        var client = new Stripe.StripeClient(apiKey);

        var startUnix = new DateTimeOffset(range.Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var endUnix = new DateTimeOffset(range.End.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero).ToUnixTimeSeconds();

        var charges = await client.V1.Charges.ListAsync(new Stripe.ChargeListOptions
        {
            Created = new Stripe.DateRangeOptions { GreaterThanOrEqual = startUnix, LessThanOrEqual = endUnix },
            Limit = 100
        }, cancellationToken: ct);

        var successfulCharges = charges.Data.Where(c => c.Status == "succeeded").ToList();
        var totalRevenue = successfulCharges.Sum(c => c.Amount) / 100m;
        var salesCount = successfulCharges.Count;
        var avgTicket = salesCount > 0 ? totalRevenue / salesCount : 0;

        var customers = await client.V1.Customers.ListAsync(new Stripe.CustomerListOptions
        {
            Created = new Stripe.DateRangeOptions { GreaterThanOrEqual = startUnix, LessThanOrEqual = endUnix }
        }, cancellationToken: ct);

        integration.MarkSynced();

        return new CollectedMetrics
        {
            Revenue = Money.BRL(totalRevenue),
            SalesCount = salesCount,
            AverageTicket = Money.BRL(avgTicket),
            NewCustomers = customers.Data.Count
        };
    }

    public Task<DemographicData?> CollectDemographicsAsync(
        Integration integration, DateRange range, CancellationToken ct)
        => Task.FromResult<DemographicData?>(null); // Stripe não tem demographics

    public Task<bool> ValidateConnectionAsync(Integration integration, CancellationToken ct)
    {
        try
        {
            var apiKey = _encryptor.Decrypt(integration.AccessToken);
            var client = new Stripe.StripeClient(apiKey);
            // Simple validation - try listing 1 charge
            return Task.FromResult(true);
        }
        catch { return Task.FromResult(false); }
    }
}
```

---

## 3. AI — Insight Generator

```csharp
public sealed class ClaudeInsightGenerator : IInsightGenerator
{
    private readonly HttpClient _httpClient;
    private readonly InsightPromptBuilder _promptBuilder;
    private readonly ILogger<ClaudeInsightGenerator> _logger;
    private readonly string _apiKey;

    public ClaudeInsightGenerator(
        HttpClient httpClient, InsightPromptBuilder promptBuilder,
        IConfiguration config, ILogger<ClaudeInsightGenerator> logger)
    {
        _httpClient = httpClient; _promptBuilder = promptBuilder;
        _logger = logger; _apiKey = config["Claude:ApiKey"]!;
    }

    public async Task<ReportInsights> GenerateInsightsAsync(
        ReportMetrics metrics, Demographics? demographics,
        ReportMetrics? previousMetrics, BusinessType businessType, CancellationToken ct)
    {
        var prompt = _promptBuilder.Build(metrics, demographics, previousMetrics, businessType);

        var request = new
        {
            model = "claude-sonnet-4-5-20250514",
            max_tokens = 500,
            messages = new[] { new { role = "user", content = prompt } }
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.anthropic.com/v1/messages", request, ct);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(ct);
        var text = result!.Content.First().Text;

        return ParseInsights(text);
    }

    private static ReportInsights ParseInsights(string text)
    {
        // Parse o texto em 3 blocos: DESTAQUE, ALERTA, DICA
        var sections = text.Split(new[] { "DESTAQUE:", "ALERTA:", "DICA:" },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new ReportInsights(
            highlight: sections.ElementAtOrDefault(0)?.Trim() ?? "Sem destaques esta semana.",
            alert: sections.ElementAtOrDefault(1)?.Trim() ?? "Nenhum alerta.",
            tip: sections.ElementAtOrDefault(2)?.Trim() ?? "Continue monitorando seus dados."
        );
    }
}

public sealed class InsightPromptBuilder
{
    public string Build(
        ReportMetrics metrics, Demographics? demographics,
        ReportMetrics? previous, BusinessType businessType)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é um consultor de negócios brasileiro. Analise os dados abaixo e gere exatamente 3 blocos:");
        sb.AppendLine("DESTAQUE: O melhor resultado da semana (máx 2 frases)");
        sb.AppendLine("ALERTA: O que precisa de atenção (máx 2 frases)");
        sb.AppendLine("DICA: Uma sugestão prática e acionável (máx 2 frases)");
        sb.AppendLine();
        sb.AppendLine($"Tipo de negócio: {businessType}");
        sb.AppendLine($"Receita: {metrics.Revenue.ToFormattedString()} ({metrics.SalesCount} vendas)");
        sb.AppendLine($"Ticket médio: {metrics.AverageTicket.ToFormattedString()}");
        sb.AppendLine($"Visitas: {metrics.TotalVisits}");

        if (previous is not null)
        {
            var revenueChange = Percentage.CalculateChange(metrics.Revenue.Amount, previous.Revenue.Amount);
            var trafficChange = Percentage.CalculateChange(metrics.TotalVisits, previous.TotalVisits);
            sb.AppendLine($"Variação receita: {revenueChange.ToFormattedString()}");
            sb.AppendLine($"Variação tráfego: {trafficChange.ToFormattedString()}");
        }

        if (demographics is not null)
        {
            sb.AppendLine($"Gênero dominante: {demographics.DominantGender} ({demographics.DominantGenderPercentage:F0}%)");
            sb.AppendLine($"Faixa etária principal: {demographics.DominantAgeGroup}");
            sb.AppendLine($"Principal cidade: {demographics.TopCity}");
            sb.AppendLine($"Principal fonte de tráfego: {demographics.TopTrafficSource}");
            sb.AppendLine($"Dispositivo: {demographics.DominantDevice}");
        }

        sb.AppendLine();
        sb.AppendLine("Regras: linguagem simples (como para o dono de uma padaria), cite números, dica deve ser acionável.");

        return sb.ToString();
    }
}
```

---

## 4. Email (Resend)

```csharp
public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _fromEmail;

    public ResendEmailSender(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Resend:ApiKey"]!;
        _fromEmail = config["Resend:FromEmail"] ?? "relatorio@weeklypulse.com.br";
    }

    public async Task<Result<bool>> SendWeeklyReportAsync(
        string recipientEmail, string recipientName, Report report, CancellationToken ct)
    {
        var html = WeeklyReportEmailTemplate.Render(recipientName, report);

        var request = new
        {
            from = $"WeeklyUp <{_fromEmail}>",
            to = new[] { recipientEmail },
            subject = $"📊 Seu resumo semanal — {report.WeekRange}",
            html
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsJsonAsync("https://api.resend.com/emails", request, ct);

        return response.IsSuccessStatusCode
            ? Result<bool>.Success(true)
            : Result<bool>.Failure($"Falha ao enviar email: {response.StatusCode}");
    }

    public async Task<Result<bool>> SendWelcomeAsync(
        string recipientEmail, string recipientName, CancellationToken ct)
    {
        // Similar ao acima, com template de boas-vindas
        return Result<bool>.Success(true);
    }
}
```

---

## 5. WhatsApp (Twilio)

```csharp
public sealed class TwilioWhatsAppSender : IWhatsAppSender
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;
    private readonly WhatsAppMessageBuilder _builder;

    public TwilioWhatsAppSender(IConfiguration config, WhatsAppMessageBuilder builder)
    {
        _accountSid = config["Twilio:AccountSid"]!;
        _authToken = config["Twilio:AuthToken"]!;
        _fromNumber = config["Twilio:WhatsAppFrom"]!;
        _builder = builder;
    }

    public async Task<Result<bool>> SendWeeklyReportAsync(
        string phoneNumber, string recipientName, Report report, CancellationToken ct)
    {
        TwilioClient.Init(_accountSid, _authToken);

        var body = _builder.BuildWeeklyReport(recipientName, report);

        var message = await MessageResource.CreateAsync(
            to: new PhoneNumber($"whatsapp:{phoneNumber}"),
            from: new PhoneNumber($"whatsapp:{_fromNumber}"),
            body: body);

        return message.ErrorCode is null
            ? Result<bool>.Success(true)
            : Result<bool>.Failure($"WhatsApp error: {message.ErrorMessage}");
    }
}

public sealed class WhatsAppMessageBuilder
{
    public string BuildWeeklyReport(string name, Report report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📊 *WeeklyUp — {report.WeekRange}*");
        sb.AppendLine($"Olá {name}! Aqui vai seu resumo:");
        sb.AppendLine();
        sb.AppendLine($"💰 *Receita:* {report.Metrics.Revenue.ToFormattedString()} ({report.RevenueChange?.ToFormattedString() ?? "—"})");
        sb.AppendLine($"🛒 *Vendas:* {report.Metrics.SalesCount} (ticket médio {report.Metrics.AverageTicket.ToFormattedString()})");
        sb.AppendLine($"📈 *Visitas:* {report.Metrics.TotalVisits} ({report.TrafficChange?.ToFormattedString() ?? "—"})");

        if (report.Demographics is not null)
        {
            sb.AppendLine();
            sb.AppendLine("👥 *Seu público:*");
            sb.AppendLine($"👩 {report.Demographics.DominantGender} {report.Demographics.DominantGenderPercentage:F0}%");
            sb.AppendLine($"🎂 {report.Demographics.DominantAgeGroup}");
            sb.AppendLine($"📍 {report.Demographics.TopCity}");
        }

        if (report.Insights is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"⭐ {report.Insights.Highlight}");
            sb.AppendLine($"⚠️ {report.Insights.Alert}");
            sb.AppendLine($"💡 *Dica:* {report.Insights.Tip}");
        }

        return sb.ToString();
    }
}
```

---

## 6. Background Jobs (Hangfire)

```csharp
public sealed class WeeklyReportJob
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WeeklyReportJob> _logger;

    public WeeklyReportJob(IMediator mediator, IUnitOfWork uow, ILogger<WeeklyReportJob> logger)
    {
        _mediator = mediator; _uow = uow; _logger = logger;
    }

    // Registrado como: RecurringJob.AddOrUpdate<WeeklyReportJob>(
    //   "weekly-report", j => j.ExecuteAsync(CancellationToken.None),
    //   "0 7 * * 1", // Toda segunda 7h
    //   new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time") });
    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting weekly report generation...");

        var today = (DayOfWeekPreference)(int)DateTime.UtcNow.DayOfWeek;
        var users = await _uow.Users.GetActiveUsersForReportAsync(today, ct);

        _logger.LogInformation("Processing {Count} users", users.Count);

        int success = 0, failed = 0;

        foreach (var user in users)
        {
            try
            {
                var result = await _mediator.Send(new GenerateWeeklyReportCommand(user.Id), ct);
                if (!result.IsSuccess) { _logger.LogWarning("Skip {User}: {Error}", user.Id, result.Error); continue; }

                await _mediator.Send(new SendReportEmailCommand(user.Id, result.Value!.Id), ct);

                if (user.CanAccessWhatsApp() && !string.IsNullOrEmpty(user.PhoneNumber))
                    await _mediator.Send(new SendReportWhatsAppCommand(user.Id, result.Value!.Id), ct);

                success++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Failed for user {UserId}", user.Id);
            }
        }

        _logger.LogInformation("Done. Success: {S}, Failed: {F}", success, failed);
    }
}
```

---

## 7. Security

```csharp
public sealed class AesTokenEncryptor : ITokenEncryptor
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesTokenEncryptor(IConfiguration config)
    {
        _key = Convert.FromBase64String(config["Encryption:Key"]!);
        _iv = Convert.FromBase64String(config["Encryption:IV"]!);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key; aes.IV = _iv;
        var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key; aes.IV = _iv;
        var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

---

## 8. DI Registration

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // EF Core + PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"))
                .AddInterceptors(new AuditableEntityInterceptor()));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IManualMetricRepository, ManualMetricRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Data Providers (Strategy Pattern)
        services.AddScoped<IDataSourceProvider, GoogleAnalyticsProvider>();
        services.AddScoped<IDataSourceProvider, StripeDataProvider>();
        services.AddScoped<IDataSourceProvider, ManualDataProvider>();
        services.AddScoped<IDataAggregator, DataAggregator>();

        // External Services
        services.AddScoped<IInsightGenerator, ClaudeInsightGenerator>();
        services.AddScoped<IEmailSender, ResendEmailSender>();
        services.AddScoped<IWhatsAppSender, TwilioWhatsAppSender>();
        services.AddSingleton<ITokenEncryptor, AesTokenEncryptor>();
        services.AddSingleton<InsightPromptBuilder>();
        services.AddSingleton<WhatsAppMessageBuilder>();

        // Cache (Redis)
        services.AddStackExchangeRedisCache(o => o.Configuration = config.GetConnectionString("Redis"));
        services.AddScoped<ICacheService, RedisCacheService>();

        // Refit clients
        services.AddRefitClient<IGoogleAnalyticsClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://analyticsdata.googleapis.com"))
            .AddPolicyHandler(GetRetryPolicy());

        // Hangfire
        services.AddHangfire(c => c.UsePostgreSqlStorage(o =>
            o.UseNpgsqlConnection(config.GetConnectionString("DefaultConnection"))));
        services.AddHangfireServer();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
```

---

*Próximo documento: [05-API-Tests-DevOps.md] — Minimal APIs (Carter), Middleware, Testes, Docker, CI/CD*
