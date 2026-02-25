# 📊 WeeklyUp — 02. Domain Layer

> **Regra de ouro:** Domain Layer tem ZERO dependências externas. Apenas `System.*`.

---

## 1. Base Classes

### 1.1 Entity

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
```

### 1.2 AggregateRoot

```csharp
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
```

### 1.3 ValueObject

```csharp
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
        => GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);

    public static bool operator ==(ValueObject? a, ValueObject? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(ValueObject? a, ValueObject? b) => !(a == b);
}
```

### 1.4 Result Pattern

```csharp
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error) { IsSuccess = false; Error = error; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}
```

---

## 2. Enums

```csharp
public enum PlanType
{
    Free = 0,
    Pro = 1,
    Business = 2
}

public enum IntegrationProvider
{
    GoogleAnalytics = 1,
    Stripe = 2,
    Manual = 3,
    // Futuro:
    InstagramBusiness = 4,
    Hotmart = 5,
    Shopify = 6
}

public enum IntegrationStatus
{
    Active = 1,
    Disconnected = 2,
    Error = 3,
    TokenExpired = 4
}

public enum BusinessType
{
    Ecommerce = 1,
    PhysicalStore = 2,
    Services = 3,
    Restaurant = 4,
    Infoproducer = 5,
    Freelancer = 6,
    Other = 99
}

public enum DayOfWeekPreference
{
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6,
    Sunday = 0
}
```

---

## 3. Value Objects

### 3.1 Email

```csharp
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure("Email não pode ser vazio.");

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 255)
            return Result<Email>.Failure("Email não pode exceder 255 caracteres.");

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return Result<Email>.Failure("Formato de email inválido.");

        return Result<Email>.Success(new Email(email));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
    public static implicit operator string(Email email) => email.Value;
}
```

### 3.2 Money

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = Math.Round(amount, 2);
        Currency = currency;
    }

    public static Money BRL(decimal amount) => new(amount, "BRL");
    public static Money Zero => new(0, "BRL");

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Não é possível somar moedas diferentes.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Não é possível subtrair moedas diferentes.");
        return new Money(Amount - other.Amount, Currency);
    }

    public string ToFormattedString() => $"R$ {Amount:N2}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### 3.3 Percentage

```csharp
public sealed class Percentage : ValueObject
{
    public decimal Value { get; }

    private Percentage(decimal value) => Value = Math.Round(value, 1);

    public static Percentage FromValue(decimal value) => new(value);

    public static Percentage CalculateChange(decimal current, decimal previous)
    {
        if (previous == 0) return new Percentage(current > 0 ? 100 : 0);
        return new Percentage(((current - previous) / previous) * 100);
    }

    public bool IsPositive => Value > 0;
    public bool IsNegative => Value < 0;
    public bool IsNeutral => Value == 0;

    public string Arrow => IsPositive ? "↑" : IsNegative ? "↓" : "→";
    public string ToFormattedString() => $"{Arrow} {Math.Abs(Value):F1}%";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### 3.4 DateRange

```csharp
public sealed class DateRange : ValueObject
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
            return Result<DateRange>.Failure("Data final deve ser igual ou posterior à inicial.");
        if (DaysBetween(start, end) > 7)
            return Result<DateRange>.Failure("Intervalo não pode exceder 7 dias.");
        return Result<DateRange>.Success(new DateRange(start, end));
    }

    public static DateRange PreviousWeek()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var thisMonday = today.AddDays(-daysSinceMonday);
        var prevMonday = thisMonday.AddDays(-7);
        var prevSunday = prevMonday.AddDays(6);
        return new DateRange(prevMonday, prevSunday);
    }

    public static DateRange CurrentWeek()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var monday = today.AddDays(-daysSinceMonday);
        var sunday = monday.AddDays(6);
        return new DateRange(monday, sunday);
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    private static int DaysBetween(DateOnly a, DateOnly b)
        => Math.Abs(b.DayNumber - a.DayNumber);

    public override string ToString() => $"{Start:dd/MM} a {End:dd/MM}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
```

### 3.5 BusinessName

```csharp
public sealed class BusinessName : ValueObject
{
    public string Value { get; }

    private BusinessName(string value) => Value = value;

    public static Result<BusinessName> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<BusinessName>.Failure("Nome do negócio é obrigatório.");

        name = name.Trim();

        if (name.Length < 2)
            return Result<BusinessName>.Failure("Nome deve ter no mínimo 2 caracteres.");
        if (name.Length > 255)
            return Result<BusinessName>.Failure("Nome não pode exceder 255 caracteres.");

        return Result<BusinessName>.Success(new BusinessName(name));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
    public static implicit operator string(BusinessName name) => name.Value;
}
```

### 3.6 Demographics (Value Object complexo)

```csharp
public sealed class Demographics : ValueObject
{
    public GenderDistribution Gender { get; }
    public IReadOnlyDictionary<string, decimal> AgeGroups { get; }
    public IReadOnlyList<LocationEntry> TopCities { get; }
    public IReadOnlyList<LocationEntry> TopStates { get; }
    public DeviceDistribution Devices { get; }
    public IReadOnlyDictionary<string, decimal> TrafficSources { get; }

    public Demographics(
        GenderDistribution gender,
        Dictionary<string, decimal> ageGroups,
        List<LocationEntry> topCities,
        List<LocationEntry> topStates,
        DeviceDistribution devices,
        Dictionary<string, decimal> trafficSources)
    {
        Gender = gender;
        AgeGroups = ageGroups.AsReadOnly();
        TopCities = topCities.OrderByDescending(l => l.Percentage).Take(10).ToList().AsReadOnly();
        TopStates = topStates.OrderByDescending(l => l.Percentage).Take(5).ToList().AsReadOnly();
        Devices = devices;
        TrafficSources = trafficSources.AsReadOnly();
    }

    // Helpers para insights
    public string DominantGender => Gender.Female > Gender.Male ? "Feminino" : "Masculino";
    public decimal DominantGenderPercentage => Math.Max(Gender.Female, Gender.Male);
    public string DominantAgeGroup => AgeGroups.MaxBy(x => x.Value).Key;
    public string TopCity => TopCities.FirstOrDefault()?.Name ?? "N/A";
    public string TopTrafficSource => TrafficSources.MaxBy(x => x.Value).Key;
    public string DominantDevice => Devices.Mobile >= Devices.Desktop ? "Mobile" : "Desktop";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Gender;
        yield return string.Join(",", AgeGroups.Select(x => $"{x.Key}:{x.Value}"));
    }
}

public sealed record GenderDistribution(decimal Female, decimal Male);
public sealed record DeviceDistribution(decimal Mobile, decimal Desktop, decimal Tablet);
public sealed record LocationEntry(string Name, decimal Percentage);
```

### 3.7 ReportMetrics (Value Object)

```csharp
public sealed class ReportMetrics : ValueObject
{
    // Revenue
    public Money Revenue { get; }
    public int SalesCount { get; }
    public Money AverageTicket { get; }
    public int NewCustomers { get; }

    // Traffic
    public int TotalVisits { get; }
    public int UniqueVisitors { get; }
    public int PageViews { get; }
    public string? TopPage { get; }
    public string? TopTrafficSource { get; }

    // Comparatives (vs semana anterior)
    public Money? PreviousRevenue { get; }
    public int? PreviousVisits { get; }

    public ReportMetrics(
        Money revenue, int salesCount, Money averageTicket, int newCustomers,
        int totalVisits, int uniqueVisitors, int pageViews,
        string? topPage, string? topTrafficSource,
        Money? previousRevenue = null, int? previousVisits = null)
    {
        Revenue = revenue;
        SalesCount = salesCount;
        AverageTicket = averageTicket;
        NewCustomers = newCustomers;
        TotalVisits = totalVisits;
        UniqueVisitors = uniqueVisitors;
        PageViews = pageViews;
        TopPage = topPage;
        TopTrafficSource = topTrafficSource;
        PreviousRevenue = previousRevenue;
        PreviousVisits = previousVisits;
    }

    public Percentage? GetRevenueChangePercentage()
    {
        if (PreviousRevenue is null) return null;
        return Percentage.CalculateChange(Revenue.Amount, PreviousRevenue.Amount);
    }

    public Percentage? GetTrafficChangePercentage()
    {
        if (PreviousVisits is null) return null;
        return Percentage.CalculateChange(TotalVisits, PreviousVisits.Value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Revenue;
        yield return SalesCount;
        yield return TotalVisits;
    }
}
```

### 3.8 ReportInsights (Value Object)

```csharp
public sealed class ReportInsights : ValueObject
{
    public string Highlight { get; }       // Destaque da semana
    public string Alert { get; }           // O que precisa atenção
    public string Tip { get; }             // Dica acionável
    public DateTime GeneratedAt { get; }

    public ReportInsights(string highlight, string alert, string tip)
    {
        Highlight = highlight ?? throw new ArgumentNullException(nameof(highlight));
        Alert = alert ?? throw new ArgumentNullException(nameof(alert));
        Tip = tip ?? throw new ArgumentNullException(nameof(tip));
        GeneratedAt = DateTime.UtcNow;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Highlight;
        yield return Alert;
        yield return Tip;
    }
}
```

---

## 4. Entities

### 4.1 User (Aggregate Root)

```csharp
public sealed class User : AggregateRoot
{
    public Email Email { get; private set; }
    public string Name { get; private set; }
    public BusinessName BusinessName { get; private set; }
    public BusinessType BusinessType { get; private set; }
    public string Timezone { get; private set; }
    public PlanType Plan { get; private set; }
    public string? ExternalAuthId { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Integration> _integrations = new();
    public IReadOnlyCollection<Integration> Integrations => _integrations.AsReadOnly();

    private User() { } // EF Core

    public static Result<User> Create(
        string email, string name, string businessName,
        BusinessType businessType, string? externalAuthId = null)
    {
        var emailResult = Email.Create(email);
        if (!emailResult.IsSuccess)
            return Result<User>.Failure(emailResult.Error!);

        var bizNameResult = BusinessName.Create(businessName);
        if (!bizNameResult.IsSuccess)
            return Result<User>.Failure(bizNameResult.Error!);

        if (string.IsNullOrWhiteSpace(name))
            return Result<User>.Failure("Nome é obrigatório.");

        return Result<User>.Success(new User
        {
            Email = emailResult.Value!,
            Name = name.Trim(),
            BusinessName = bizNameResult.Value!,
            BusinessType = businessType,
            Timezone = "America/Sao_Paulo",
            Plan = PlanType.Free,
            ExternalAuthId = externalAuthId,
            IsActive = true
        });
    }

    // ---------- Integrations ----------

    public Result<Integration> AddIntegration(
        IntegrationProvider provider, string accessToken,
        string refreshToken, string providerAccountId,
        string? propertyId, ITokenEncryptor encryptor)
    {
        if (HasActiveIntegration(provider))
            return Result<Integration>.Failure($"Integração com {provider} já está ativa.");

        if (ActiveIntegrationCount >= MaxIntegrationsForPlan)
            return Result<Integration>.Failure(
                $"Plano {Plan} permite no máximo {MaxIntegrationsForPlan} integração(ões). Faça upgrade.");

        var integration = Integration.Create(
            Id, provider,
            encryptor.Encrypt(accessToken),
            encryptor.Encrypt(refreshToken),
            providerAccountId, propertyId);

        _integrations.Add(integration);
        RaiseDomainEvent(new IntegrationConnectedEvent(Id, provider));

        return Result<Integration>.Success(integration);
    }

    public Result<bool> RemoveIntegration(IntegrationProvider provider)
    {
        var integration = _integrations
            .FirstOrDefault(i => i.Provider == provider && i.Status == IntegrationStatus.Active);

        if (integration is null)
            return Result<bool>.Failure("Integração não encontrada ou já desconectada.");

        integration.Disconnect();
        return Result<bool>.Success(true);
    }

    private bool HasActiveIntegration(IntegrationProvider provider)
        => _integrations.Any(i => i.Provider == provider && i.Status == IntegrationStatus.Active);

    private int ActiveIntegrationCount
        => _integrations.Count(i => i.Status == IntegrationStatus.Active);

    private int MaxIntegrationsForPlan => Plan switch
    {
        PlanType.Free => 1,
        PlanType.Pro => 3,
        PlanType.Business => int.MaxValue,
        _ => 1
    };

    // ---------- Plan ----------

    public Result<bool> UpgradePlan(PlanType newPlan)
    {
        if (newPlan <= Plan)
            return Result<bool>.Failure("Novo plano deve ser superior ao atual.");

        var previous = Plan;
        Plan = newPlan;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserUpgradedPlanEvent(Id, previous, newPlan));
        return Result<bool>.Success(true);
    }

    // ---------- Feature Access (baseado no plano) ----------

    public bool CanAccessDemographics() => Plan >= PlanType.Pro;
    public bool CanAccessInsights() => Plan >= PlanType.Pro;
    public bool CanAccessWhatsApp() => Plan >= PlanType.Business;
    public bool CanAccessMultipleBusiness() => Plan >= PlanType.Business;

    // ---------- Profile ----------

    public void UpdateProfile(string name, string businessName, BusinessType type)
    {
        Name = name.Trim();
        BusinessName = BusinessName.Create(businessName).Value!;
        BusinessType = type;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPhoneNumber(string phone)
    {
        PhoneNumber = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 4.2 Integration (Entity)

```csharp
public sealed class Integration : Entity
{
    public Guid UserId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public string AccessToken { get; private set; }     // Criptografado
    public string RefreshToken { get; private set; }    // Criptografado
    public string ProviderAccountId { get; private set; }
    public string? PropertyId { get; private set; }     // GA4 property
    public IntegrationStatus Status { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastError { get; private set; }

    private Integration() { } // EF Core

    internal static Integration Create(
        Guid userId, IntegrationProvider provider,
        string encryptedAccessToken, string encryptedRefreshToken,
        string providerAccountId, string? propertyId)
    {
        return new Integration
        {
            UserId = userId,
            Provider = provider,
            AccessToken = encryptedAccessToken,
            RefreshToken = encryptedRefreshToken,
            ProviderAccountId = providerAccountId,
            PropertyId = propertyId,
            Status = IntegrationStatus.Active
        };
    }

    public void UpdateTokens(string encryptedAccess, string encryptedRefresh)
    {
        AccessToken = encryptedAccess;
        RefreshToken = encryptedRefresh;
        Status = IntegrationStatus.Active;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSynced() { LastSyncAt = DateTime.UtcNow; }

    public void MarkError(string error)
    {
        Status = IntegrationStatus.Error;
        LastError = error;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkTokenExpired()
    {
        Status = IntegrationStatus.TokenExpired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disconnect()
    {
        Status = IntegrationStatus.Disconnected;
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 4.3 Report (Aggregate Root)

```csharp
public sealed class Report : AggregateRoot
{
    public Guid UserId { get; private set; }
    public DateRange WeekRange { get; private set; }
    public ReportMetrics Metrics { get; private set; }
    public Demographics? Demographics { get; private set; }
    public ReportInsights? Insights { get; private set; }
    public DateTime? EmailSentAt { get; private set; }
    public DateTime? EmailOpenedAt { get; private set; }
    public DateTime? WhatsAppSentAt { get; private set; }

    private Report() { } // EF Core

    public static Report Create(
        Guid userId, DateRange weekRange,
        ReportMetrics metrics, Demographics? demographics = null)
    {
        var report = new Report
        {
            UserId = userId,
            WeekRange = weekRange,
            Metrics = metrics,
            Demographics = demographics
        };

        report.RaiseDomainEvent(
            new ReportGeneratedEvent(report.Id, userId, weekRange));

        return report;
    }

    public void AddInsights(ReportInsights insights)
    {
        Insights = insights ?? throw new ArgumentNullException(nameof(insights));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDemographics(Demographics demographics)
    {
        Demographics = demographics;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkEmailSent() { EmailSentAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void MarkEmailOpened() { EmailOpenedAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void MarkWhatsAppSent() { WhatsAppSentAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }

    public bool WasEmailSent => EmailSentAt.HasValue;
    public bool WasEmailOpened => EmailOpenedAt.HasValue;
    public Percentage? RevenueChange => Metrics.GetRevenueChangePercentage();
    public Percentage? TrafficChange => Metrics.GetTrafficChangePercentage();
}
```

### 4.4 ReportPreference e ManualMetric

```csharp
public sealed class ReportPreference : Entity
{
    public Guid UserId { get; private set; }
    public DayOfWeekPreference SendDay { get; private set; }
    public TimeOnly SendTime { get; private set; }
    public string Language { get; private set; }
    public List<string> EnabledSections { get; private set; }

    private ReportPreference() { }

    public static ReportPreference CreateDefault(Guid userId)
    {
        return new ReportPreference
        {
            UserId = userId,
            SendDay = DayOfWeekPreference.Monday,
            SendTime = new TimeOnly(7, 0),
            Language = "pt-BR",
            EnabledSections = new() { "revenue", "traffic", "demographics", "insights", "tips" }
        };
    }

    public void Update(DayOfWeekPreference day, TimeOnly time, List<string> sections)
    {
        SendDay = day;
        SendTime = time;
        EnabledSections = sections;
        UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class ManualMetric : Entity
{
    public Guid UserId { get; private set; }
    public DateOnly WeekStart { get; private set; }
    public decimal? Revenue { get; private set; }
    public int? SalesCount { get; private set; }
    public int? NewCustomers { get; private set; }
    public int? Visits { get; private set; }
    public Dictionary<string, object>? CustomMetrics { get; private set; }

    private ManualMetric() { }

    public static ManualMetric Create(
        Guid userId, DateOnly weekStart,
        decimal? revenue = null, int? sales = null,
        int? customers = null, int? visits = null)
    {
        return new ManualMetric
        {
            UserId = userId,
            WeekStart = weekStart,
            Revenue = revenue,
            SalesCount = sales,
            NewCustomers = customers,
            Visits = visits
        };
    }
}
```

---

## 5. Domain Interfaces

### 5.1 Repositories

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByIdWithIntegrationsAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByExternalAuthIdAsync(string externalId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetActiveUsersForReportAsync(DayOfWeekPreference day, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
}

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Report?> GetByUserAndWeekAsync(Guid userId, DateOnly weekStart, CancellationToken ct = default);
    Task<IReadOnlyList<Report>> GetHistoryAsync(Guid userId, int count = 12, CancellationToken ct = default);
    Task<Report?> GetLatestAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Report report, CancellationToken ct = default);
    void Update(Report report);
}

public interface IIntegrationRepository
{
    Task<Integration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Integration>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Integration>> GetActiveWithExpiredTokensAsync(CancellationToken ct = default);
}

public interface IManualMetricRepository
{
    Task<ManualMetric?> GetByUserAndWeekAsync(Guid userId, DateOnly weekStart, CancellationToken ct = default);
    Task AddAsync(ManualMetric metric, CancellationToken ct = default);
    void Update(ManualMetric metric);
}
```

### 5.2 Service Interfaces

```csharp
// Strategy Pattern — cada fonte de dados implementa isso
public interface IDataSourceProvider
{
    IntegrationProvider ProviderType { get; }
    Task<CollectedMetrics> CollectMetricsAsync(
        Integration integration, DateRange range, CancellationToken ct = default);
    Task<DemographicData?> CollectDemographicsAsync(
        Integration integration, DateRange range, CancellationToken ct = default);
    Task<bool> ValidateConnectionAsync(
        Integration integration, CancellationToken ct = default);
}

public interface IInsightGenerator
{
    Task<ReportInsights> GenerateInsightsAsync(
        ReportMetrics metrics, Demographics? demographics,
        ReportMetrics? previousMetrics, BusinessType businessType,
        CancellationToken ct = default);
}

public interface IEmailSender
{
    Task<Result<bool>> SendWeeklyReportAsync(
        string recipientEmail, string recipientName,
        Report report, CancellationToken ct = default);
    Task<Result<bool>> SendWelcomeAsync(
        string recipientEmail, string recipientName,
        CancellationToken ct = default);
}

public interface IWhatsAppSender
{
    Task<Result<bool>> SendWeeklyReportAsync(
        string phoneNumber, string recipientName,
        Report report, CancellationToken ct = default);
}

public interface ITokenEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IIntegrationRepository Integrations { get; }
    IReportRepository Reports { get; }
    IManualMetricRepository ManualMetrics { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

---

## 6. Domain Events

```csharp
public sealed record ReportGeneratedEvent(
    Guid ReportId, Guid UserId, DateRange WeekRange
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record IntegrationConnectedEvent(
    Guid UserId, IntegrationProvider Provider
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record UserUpgradedPlanEvent(
    Guid UserId, PlanType PreviousPlan, PlanType NewPlan
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

---

## 7. Domain Exceptions

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public sealed class InvalidEmailException : DomainException
{
    public InvalidEmailException(string email)
        : base($"Email inválido: {email}") { }
}

public sealed class IntegrationLimitExceededException : DomainException
{
    public IntegrationLimitExceededException(PlanType plan, int maxAllowed)
        : base($"Plano {plan} permite no máximo {maxAllowed} integração(ões).") { }
}

public sealed class ReportAlreadyExistsException : DomainException
{
    public ReportAlreadyExistsException(Guid userId, DateOnly weekStart)
        : base($"Relatório da semana {weekStart:dd/MM} já existe para este usuário.") { }
}
```

---

*Próximo documento: [03-Application-Layer.md] — CQRS Handlers, Behaviors, DTOs*
