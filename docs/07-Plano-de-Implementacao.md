# WeeklyUp -- 07. Plano de Implementacao Detalhado

> **Objetivo:** Guia completo e rigoroso para implementacao do backend do WeeklyUp, do primeiro arquivo ao deploy.
> **Stack definitiva:** .NET 10 / C# 14 / Mediator (source generator) / Mapperly / FluentValidation / EF Core 10 / PostgreSQL 17 / Redis 7 / Carter / Hangfire

---

## ATENCAO: Diferencas Criticas entre Docs Anteriores e Stack Real

Os documentos 01-06 foram escritos usando MediatR e Mapster/AutoMapper como referencia. O `Directory.Packages.props` real do projeto define:

| Doc Anterior | Stack Real (Directory.Packages.props) | Impacto |
|---|---|---|
| MediatR 12.x | **Mediator 2.1.7** (source generator) | `IRequest<T>` vira `IQuery<T>` / `ICommand<T>`. `IPipelineBehavior` vira `IPipelineBehavior<,>` do Mediator. Sem reflection, compilado em build time. |
| Mapster / AutoMapper | **Riok.Mapperly 4.1.1** | Mappers sao `[Mapper]` partial classes com source generation. Zero reflection. |
| `IRequestHandler<TRequest, TResponse>` | `IQueryHandler<TQuery, TResult>` / `ICommandHandler<TCommand, TResult>` | Separacao explicita de Commands e Queries no tipo. |

Todos os exemplos de codigo neste plano usam a stack **real** do projeto.

---

## 1. Principios e Padroes -- Referencia Rapida

### 1.1 Clean Architecture -- Regras de Dependencia

```
API (Carter Modules)
  |
  v
Application (Commands, Queries, Handlers, DTOs)
  |
  v
Domain (Entities, Value Objects, Interfaces, Events)
  ^
  |
Infrastructure (EF Core, Providers, Email, Cache)
```

**Regras absolutas:**

| Camada | PODE referenciar | NUNCA referencia |
|--------|-----------------|------------------|
| Domain | `System.*` apenas | Application, Infrastructure, Api, EF Core, Mediator |
| Shared | `System.*` apenas | Domain, Application, Infrastructure, Api |
| Application | Domain, Shared, Mediator.Abstractions, FluentValidation, Mapperly | Infrastructure, Api, EF Core |
| Infrastructure | Domain, Application (interfaces), Shared, EF Core, pacotes externos | Api |
| Api | Application (via Mediator), Shared, Carter | Domain.Entities direto, Infrastructure direto |

### 1.2 SOLID -- Aplicacao no Projeto

| Principio | Regra Concreta |
|-----------|---------------|
| **S** - Single Responsibility | Um handler = uma operacao. `GenerateWeeklyReportCommandHandler` nao envia email. |
| **O** - Open/Closed | Novos providers (Instagram, Hotmart) via `IDataSourceProvider` sem alterar codigo existente. |
| **L** - Liskov Substitution | `GoogleAnalyticsProvider` e `StripeProvider` substituiveis via `IDataSourceProvider`. |
| **I** - Interface Segregation | `IReportRepository` separado de `IUserRepository`. `IEmailSender` separado de `IWhatsAppSender`. |
| **D** - Dependency Inversion | Application depende de `IReportRepository`, nunca de `EfReportRepository`. |

### 1.3 Clean Code -- Regras Inegociaveis

| Regra | Limite |
|-------|--------|
| Metodos | Max 20 linhas |
| Classes | Max 200 linhas |
| Parametros de metodo | Max 4 (usar objetos para mais) |
| Nested ifs | Max 2 niveis (usar early return / guard clauses) |
| Nomes | Descritivos, sem abreviacoes (`GenerateWeeklyReportCommand`, nao `GenRptCmd`) |
| Comentarios | Zero comentarios obvios. Codigo auto-documentavel. |
| Magic numbers/strings | Usar constantes nomeadas ou enums |
| Retornos null | Usar `Result<T>` para erros esperados |
| Exceptions | Apenas para erros inesperados. Domain exceptions para regras de negocio. |

### 1.4 CQRS -- Regras

| Aspecto | Commands | Queries |
|---------|----------|---------|
| Interface | `ICommand<TResult>` | `IQuery<TResult>` |
| Handler | `ICommandHandler<TCommand, TResult>` | `IQueryHandler<TQuery, TResult>` |
| Retorno | `Result<TDto>` (nunca entidades) | DTO ou `PagedList<TDto>` |
| Persistencia | `IUnitOfWork` com `SaveChangesAsync` | `AsNoTracking()` obrigatorio |
| Validacao | `AbstractValidator<TCommand>` obrigatorio | Opcional |
| Cache | Invalidacao apos escrita | `HybridCache` / `ICacheService` para reads frequentes |
| CancellationToken | Obrigatorio em todos os metodos async | Obrigatorio |

### 1.5 DDD -- Regras Taticas

| Pattern | Regra |
|---------|-------|
| Entity | Identidade por `Guid Id`. Construtor privado. Factory method `Create()` estatico. |
| AggregateRoot | Herda de Entity. Possui `List<IDomainEvent>`. Unico ponto de acesso do agregado. |
| Value Object | Imutavel. Igualdade por valor. Validacao na criacao. |
| Domain Event | Record imutavel implementando `IDomainEvent`. |
| Repository | Interface no Domain, implementacao na Infrastructure. Apenas para Aggregate Roots. |
| Invariants | Validados dentro da entidade. Retornam `Result<T>` ou lancam `DomainException`. |

### 1.6 Convencoes de Nomenclatura

```
Namespaces:     WeeklyUp.Domain.Entities
Classes:        PascalCase              GenerateWeeklyReportCommandHandler
Interfaces:     I + PascalCase          IDataSourceProvider
Metodos:        PascalCase              CollectMetricsAsync()
Propriedades:   PascalCase              BusinessName
Campos privados: _camelCase             _unitOfWork
Parametros:     camelCase               cancellationToken
Constantes:     PascalCase              MaxIntegrationsForFree
Tabelas DB:     snake_case              report_preferences
Colunas DB:     snake_case              week_start
Arquivos:       PascalCase              GenerateWeeklyReportCommand.cs
```

### 1.7 Qualidade de Build

Definido em `Directory.Build.props`:
- `TreatWarningsAsErrors: true` -- zero warnings permitidos
- `Nullable: enable` -- nullable reference types obrigatorio
- `ImplicitUsings: enable`
- `AnalysisLevel: latest`
- `EnforceCodeStyleInBuild: true`

---

## 2. Estrutura de Pastas Completa e Definitiva

### 2.1 Projeto Shared (`src/Shared/`)

```
src/Shared/
  Shared.csproj
  Constants/
    CacheKeys.cs
    ClaimTypes.cs
    Policies.cs
    JobNames.cs
    PlanLimits.cs
  Extensions/
    StringExtensions.cs
    DateTimeExtensions.cs
    EnumerableExtensions.cs
    QueryableExtensions.cs
  Pagination/
    PagedList.cs
    PaginationParams.cs
  Guards/
    Guard.cs
  Results/
    Result.cs
    Error.cs
    ErrorType.cs
    ValidationError.cs
```

### 2.2 Projeto Domain (`src/Domain/`)

```
src/Domain/
  Domain.csproj
  Common/
    Entity.cs
    AggregateRoot.cs
    ValueObject.cs
    IDomainEvent.cs
  Entities/
    User.cs
    Integration.cs
    Report.cs
    ReportPreference.cs
    ManualMetric.cs
  ValueObjects/
    Email.cs
    Money.cs
    Percentage.cs
    DateRange.cs
    BusinessName.cs
    Demographics.cs
    ReportMetrics.cs
    ReportInsights.cs
  Enums/
    PlanType.cs
    IntegrationProvider.cs
    IntegrationStatus.cs
    BusinessType.cs
    DayOfWeekPreference.cs
    ReportStatus.cs
  Events/
    UserRegisteredEvent.cs
    UserEmailVerifiedEvent.cs
    UserPlanUpgradedEvent.cs
    IntegrationConnectedEvent.cs
    IntegrationDisconnectedEvent.cs
    IntegrationSyncFailedEvent.cs
    ReportGeneratedEvent.cs
    ReportSentEvent.cs
  Exceptions/
    DomainException.cs
    InvalidEmailException.cs
    IntegrationLimitExceededException.cs
    ReportAlreadyExistsException.cs
  Interfaces/
    Repositories/
      IUserRepository.cs
      IIntegrationRepository.cs
      IReportRepository.cs
      IManualMetricRepository.cs
      IReportPreferenceRepository.cs
    Services/
      IDataSourceProvider.cs
      IInsightGenerator.cs
      IEmailSender.cs
      IWhatsAppSender.cs
      ITokenEncryptor.cs
      IReportGeneratorService.cs
    IUnitOfWork.cs
  Specifications/
    ISpecification.cs
    ActiveUsersForReportSpec.cs
```

### 2.3 Projeto Application (`src/Application/`)

```
src/Application/
  Application.csproj
  Common/
    Interfaces/
      ICurrentUserService.cs
      IDateTimeProvider.cs
      ICacheService.cs
      IDataAggregator.cs
    Behaviors/
      ValidationBehavior.cs
      LoggingBehavior.cs
      PerformanceBehavior.cs
      TransactionBehavior.cs
    Exceptions/
      ValidationException.cs
      NotFoundException.cs
      ForbiddenException.cs
    Mappings/
      UserMapper.cs
      ReportMapper.cs
      IntegrationMapper.cs
      DemographicsMapper.cs
    Models/
      CollectedMetrics.cs
      DemographicData.cs
  Users/
    Commands/
      RegisterUser/
        RegisterUserCommand.cs
        RegisterUserCommandHandler.cs
        RegisterUserCommandValidator.cs
      VerifyEmail/
        VerifyEmailCommand.cs
        VerifyEmailCommandHandler.cs
      UpdateUserProfile/
        UpdateUserProfileCommand.cs
        UpdateUserProfileCommandHandler.cs
        UpdateUserProfileCommandValidator.cs
      UpgradePlan/
        UpgradePlanCommand.cs
        UpgradePlanCommandHandler.cs
        UpgradePlanCommandValidator.cs
    Queries/
      GetUserProfile/
        GetUserProfileQuery.cs
        GetUserProfileQueryHandler.cs
      GetUserDashboard/
        GetUserDashboardQuery.cs
        GetUserDashboardQueryHandler.cs
        UserDashboardResponse.cs
    DTOs/
      UserProfileDto.cs
      CurrentWeekSummary.cs
      WeeklyTrendPoint.cs
  Integrations/
    Commands/
      ConnectIntegration/
        ConnectIntegrationCommand.cs
        ConnectIntegrationCommandHandler.cs
        ConnectIntegrationCommandValidator.cs
      DisconnectIntegration/
        DisconnectIntegrationCommand.cs
        DisconnectIntegrationCommandHandler.cs
    Queries/
      GetIntegrations/
        GetIntegrationsQuery.cs
        GetIntegrationsQueryHandler.cs
    DTOs/
      IntegrationDto.cs
  Reports/
    Commands/
      GenerateWeeklyReport/
        GenerateWeeklyReportCommand.cs
        GenerateWeeklyReportCommandHandler.cs
        GenerateWeeklyReportCommandValidator.cs
      SendWeeklyReport/
        SendWeeklyReportCommand.cs
        SendWeeklyReportCommandHandler.cs
      AddManualMetric/
        AddManualMetricCommand.cs
        AddManualMetricCommandHandler.cs
        AddManualMetricCommandValidator.cs
      UpdateReportPreferences/
        UpdateReportPreferencesCommand.cs
        UpdateReportPreferencesCommandHandler.cs
        UpdateReportPreferencesCommandValidator.cs
    Queries/
      GetReportDetail/
        GetReportDetailQuery.cs
        GetReportDetailQueryHandler.cs
      GetReportHistory/
        GetReportHistoryQuery.cs
        GetReportHistoryQueryHandler.cs
    DTOs/
      ReportDto.cs
      ReportSummaryDto.cs
      ReportDetailDto.cs
      MetricsDto.cs
      DemographicsDto.cs
      InsightsDto.cs
    EventHandlers/
      ReportGeneratedEventHandler.cs
      IntegrationConnectedEventHandler.cs
  DependencyInjection.cs
```

### 2.4 Projeto Infrastructure (`src/Infrastructure/`)

```
src/Infrastructure/
  Infrastructure.csproj
  Persistence/
    ApplicationDbContext.cs
    UnitOfWork.cs
    Configurations/
      UserConfiguration.cs
      IntegrationConfiguration.cs
      ReportConfiguration.cs
      ReportPreferenceConfiguration.cs
      ManualMetricConfiguration.cs
    Repositories/
      UserRepository.cs
      IntegrationRepository.cs
      ReportRepository.cs
      ManualMetricRepository.cs
      ReportPreferenceRepository.cs
    Interceptors/
      AuditableEntityInterceptor.cs
      DomainEventDispatchInterceptor.cs
    Migrations/
  DataSources/
    GoogleAnalytics/
      GoogleAnalyticsProvider.cs
      IGoogleAnalyticsClient.cs
      GA4Models.cs
      GA4Mapper.cs
    Stripe/
      StripeDataProvider.cs
      StripeMapper.cs
    Manual/
      ManualDataProvider.cs
    DataAggregator.cs
  AI/
    ClaudeInsightGenerator.cs
    InsightPromptBuilder.cs
  Email/
    ResendEmailSender.cs
    Templates/
      WeeklyReportEmailTemplate.cs
      WelcomeEmailTemplate.cs
  WhatsApp/
    TwilioWhatsAppSender.cs
    WhatsAppMessageBuilder.cs
  Caching/
    RedisCacheService.cs
  Security/
    AesTokenEncryptor.cs
    JwtTokenGenerator.cs
    CurrentUserService.cs
  BackgroundJobs/
    WeeklyReportGenerationJob.cs
    ReportSendingJob.cs
    IntegrationSyncJob.cs
    TokenRefreshJob.cs
  Time/
    DateTimeProvider.cs
  DependencyInjection.cs
```

### 2.5 Projeto Api (`src/Api/`)

```
src/Api/
  Api.csproj
  Endpoints/
    AuthEndpoints.cs
    UserEndpoints.cs
    IntegrationEndpoints.cs
    ReportEndpoints.cs
    DashboardEndpoints.cs
    ManualMetricEndpoints.cs
    WebhookEndpoints.cs
  Middleware/
    ExceptionHandlingMiddleware.cs
    RequestLoggingMiddleware.cs
    CorrelationIdMiddleware.cs
  Extensions/
    ResultExtensions.cs
    ClaimsPrincipalExtensions.cs
  Models/
    ApiResponse.cs
    ErrorResponse.cs
    PaginatedResponse.cs
  Program.cs
  Dockerfile
  appsettings.json
  appsettings.Development.json
```

### 2.6 Projetos de Teste

```
tests/
  WeeklyUp.Domain.Testes/
    WeeklyUp.Domain.Testes.csproj
    Entities/
      UserTests.cs
      IntegrationTests.cs
      ReportTests.cs
      ReportPreferenceTests.cs
      ManualMetricTests.cs
    ValueObjects/
      EmailTests.cs
      MoneyTests.cs
      PercentageTests.cs
      DateRangeTests.cs
      BusinessNameTests.cs
      DemographicsTests.cs
      ReportMetricsTests.cs
    Builders/
      UserBuilder.cs
      ReportBuilder.cs
      IntegrationBuilder.cs
  WeeklyUp.Application.Tests/
    WeeklyUp.Application.Tests.csproj
    Users/
      RegisterUserCommandHandlerTests.cs
      GetUserDashboardQueryHandlerTests.cs
    Reports/
      GenerateWeeklyReportCommandHandlerTests.cs
      SendWeeklyReportCommandHandlerTests.cs
      GetReportHistoryQueryHandlerTests.cs
    Integrations/
      ConnectIntegrationCommandHandlerTests.cs
    Behaviors/
      ValidationBehaviorTests.cs
      PerformanceBehaviorTests.cs
    Builders/
      UserBuilder.cs
      ReportBuilder.cs
  WeeklyUp.Infrastructure.Tests/
    WeeklyUp.Infrastructure.Tests.csproj
    Persistence/
      UserRepositoryTests.cs
      ReportRepositoryTests.cs
    DataSources/
      GoogleAnalyticsProviderTests.cs
      StripeDataProviderTests.cs
      DataAggregatorTests.cs
    AI/
      InsightPromptBuilderTests.cs
    Fixtures/
      DatabaseFixture.cs
  WeeklyUp.Api.Tests/
    WeeklyUp.Api.Tests.csproj
    Endpoints/
      AuthEndpointsTests.cs
      ReportEndpointsTests.cs
      DashboardEndpointsTests.cs
    Middleware/
      ExceptionHandlingMiddlewareTests.cs
    Fixtures/
      WebAppFixture.cs
  WeeklyUp.Architecture.Tests/
    WeeklyUp.Architecture.Tests.csproj
    CleanArchitectureTests.cs
    NamingConventionTests.cs
    CqrsPatternTests.cs
```

---

## 3. Ordem de Implementacao Rigorosa

### Diagrama de Dependencias entre Fases

```
Fase 0 (Setup)
    |
    v
Fase 1 (Shared) -----> Fase 2 (Domain)
                            |
                            v
                        Fase 3 (Application)
                            |
                            v
                        Fase 4 (Infrastructure)
                            |
                            v
                        Fase 5 (API)
                            |
                            v
                        Fase 6 (Testes de Integracao + Architecture)
```

Cada fase so pode iniciar quando a fase anterior estiver 100% concluida com todos os testes passando.

---

## Fase 0: Setup dos Projetos

### Objetivo
Configurar os .csproj com as dependencias corretas e referencias entre projetos.

### Pre-requisitos
- Solution criada (ja existe)
- `Directory.Build.props` configurado (ja existe)
- `Directory.Packages.props` configurado (ja existe)

### Arquivos a Modificar

**`src/Domain/Domain.csproj`** -- Zero pacotes NuGet. Apenas System.
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>WeeklyUp.Domain</RootNamespace>
    <AssemblyName>WeeklyUp.Domain</AssemblyName>
  </PropertyGroup>
</Project>
```

**`src/Shared/Shared.csproj`** -- Zero pacotes NuGet. Zero referencias a outros projetos.
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>WeeklyUp.Shared</RootNamespace>
    <AssemblyName>WeeklyUp.Shared</AssemblyName>
  </PropertyGroup>
</Project>
```

**`src/Application/Application.csproj`** -- Mediator, FluentValidation, Mapperly. Referencia Domain e Shared.
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>WeeklyUp.Application</RootNamespace>
    <AssemblyName>WeeklyUp.Application</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Mediator.Abstractions" />
    <PackageReference Include="Mediator.SourceGenerator" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Riok.Mapperly" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Shared\Shared.csproj" />
  </ItemGroup>
</Project>
```

**`src/Infrastructure/Infrastructure.csproj`** -- EF Core, Redis, Hangfire, Refit, Polly, Serilog, Stripe. Referencia Domain, Application, Shared.
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>WeeklyUp.Infrastructure</RootNamespace>
    <AssemblyName>WeeklyUp.Infrastructure</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Hangfire.Core" />
    <PackageReference Include="Hangfire.PostgreSql" />
    <PackageReference Include="StackExchange.Redis" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
    <PackageReference Include="Refit" />
    <PackageReference Include="Refit.HttpClientFactory" />
    <PackageReference Include="Polly" />
    <PackageReference Include="Polly.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Http.Polly" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.Seq" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Stripe.net" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Shared\Shared.csproj" />
  </ItemGroup>
</Project>
```

**`src/Api/Api.csproj`** -- Carter, JWT, Identity, OpenApi, Scalar, Hangfire.AspNetCore, Health Checks. Referencia Application e Infrastructure.
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <RootNamespace>WeeklyUp.Api</RootNamespace>
    <AssemblyName>WeeklyUp.Api</AssemblyName>
    <UserSecretsId>3bd9b8c6-951d-4f69-97f7-e1bfbe8fd822</UserSecretsId>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <DockerfileContext>..\..</DockerfileContext>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Carter" />
    <PackageReference Include="Scalar.AspNetCore" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
    <PackageReference Include="Hangfire.AspNetCore" />
    <PackageReference Include="AspNetCore.HealthChecks.NpgSql" />
    <PackageReference Include="AspNetCore.HealthChecks.Redis" />
    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### Projetos de Teste

Cada projeto de teste referencia o projeto que testa, mais os pacotes de teste:

```xml
<!-- Pacotes comuns a todos os projetos de teste -->
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="FluentAssertions" />
<PackageReference Include="NSubstitute" />
<PackageReference Include="coverlet.collector" />
```

| Projeto de Teste | Referencia | Pacotes Extras |
|---|---|---|
| WeeklyUp.Domain.Testes | Domain | NSubstitute (para ITokenEncryptor) |
| WeeklyUp.Application.Tests | Application, Domain | NSubstitute |
| WeeklyUp.Infrastructure.Tests | Infrastructure, Domain | EF Core InMemory |
| WeeklyUp.Api.Tests | Api | Microsoft.AspNetCore.Mvc.Testing, EF Core InMemory |
| WeeklyUp.Architecture.Tests | Api, Application, Domain, Infrastructure | NetArchTest.Rules |

### Criterios de Aceitacao
- [ ] `dotnet build` compila sem erros nem warnings
- [ ] Todas as referencias entre projetos seguem a regra de dependencia
- [ ] Domain.csproj nao tem PackageReference algum
- [ ] Shared.csproj nao tem PackageReference algum

---

## Fase 1: Shared Layer

### Objetivo
Implementar tipos utilitarios cross-cutting que serao usados por todas as camadas.

### Pre-requisitos
- Fase 0 concluida (projetos configurados)

### Arquivos a Criar

#### 1.1 Result Pattern (`src/Shared/Results/`)

**`Error.cs`**
```csharp
namespace WeeklyUp.Shared.Results;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("Error.NullValue", "Valor nulo fornecido.", ErrorType.Failure);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);
}
```

**`ErrorType.cs`**
```csharp
namespace WeeklyUp.Shared.Results;

public enum ErrorType
{
    None = 0,
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Forbidden = 5
}
```

**`ValidationError.cs`**
```csharp
namespace WeeklyUp.Shared.Results;

public sealed record ValidationError : Error
{
    public IReadOnlyList<Error> Errors { get; }

    public ValidationError(IReadOnlyList<Error> errors)
        : base("Validation.General", "Erros de validacao encontrados.", ErrorType.Validation)
    {
        Errors = errors;
    }
}
```

**`Result.cs`**
```csharp
namespace WeeklyUp.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Resultado de sucesso nao pode ter erro.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Resultado de falha deve ter erro.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) =>
        new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) =>
        new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Nao e possivel acessar o valor de um resultado falho.");

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);

    public static implicit operator Result<TValue>(Error error) =>
        Failure<TValue>(error);
}
```

#### 1.2 Pagination (`src/Shared/Pagination/`)

**`PaginationParams.cs`**
```csharp
namespace WeeklyUp.Shared.Pagination;

public sealed record PaginationParams
{
    private const int MaxPageSize = 50;
    private const int DefaultPageSize = 10;

    public int PageNumber { get; init; } = 1;

    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}
```

**`PagedList.cs`**
```csharp
namespace WeeklyUp.Shared.Pagination;

public sealed class PagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    private PagedList(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedList<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount) =>
        new(items, pageNumber, pageSize, totalCount);
}
```

#### 1.3 Guards (`src/Shared/Guards/`)

**`Guard.cs`**
```csharp
namespace WeeklyUp.Shared.Guards;

public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} nao pode ser nulo ou vazio.", paramName);
        return value;
    }

    public static T AgainstNull<T>(T? value, string paramName) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        return value;
    }

    public static Guid AgainstEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{paramName} nao pode ser vazio.", paramName);
        return value;
    }

    public static decimal AgainstNegative(decimal value, string paramName)
    {
        if (value < 0)
            throw new ArgumentException($"{paramName} nao pode ser negativo.", paramName);
        return value;
    }

    public static int AgainstNegative(int value, string paramName)
    {
        if (value < 0)
            throw new ArgumentException($"{paramName} nao pode ser negativo.", paramName);
        return value;
    }
}
```

#### 1.4 Constants (`src/Shared/Constants/`)

**`CacheKeys.cs`**
```csharp
namespace WeeklyUp.Shared.Constants;

public static class CacheKeys
{
    public const string DashboardPrefix = "dashboard:";
    public const string UserProfilePrefix = "user-profile:";
    public const string IntegrationsPrefix = "integrations:";
    public const string ReportPrefix = "report:";

    public static string Dashboard(Guid userId) => $"{DashboardPrefix}{userId}";
    public static string UserProfile(Guid userId) => $"{UserProfilePrefix}{userId}";
    public static string Integrations(Guid userId) => $"{IntegrationsPrefix}{userId}";
    public static string Report(Guid reportId) => $"{ReportPrefix}{reportId}";
}
```

**`CustomClaimTypes.cs`**
```csharp
namespace WeeklyUp.Shared.Constants;

public static class CustomClaimTypes
{
    public const string UserId = "uid";
    public const string Plan = "plan";
    public const string BusinessType = "biz_type";
}
```

**`Policies.cs`**
```csharp
namespace WeeklyUp.Shared.Constants;

public static class Policies
{
    public const string ProPlan = "ProPlan";
    public const string BusinessPlan = "BusinessPlan";
}
```

**`JobNames.cs`**
```csharp
namespace WeeklyUp.Shared.Constants;

public static class JobNames
{
    public const string WeeklyReportGeneration = "weekly-report-generation";
    public const string ReportSending = "report-sending";
    public const string IntegrationSync = "integration-sync";
    public const string TokenRefresh = "token-refresh";
}
```

**`PlanLimits.cs`**
```csharp
namespace WeeklyUp.Shared.Constants;

public static class PlanLimits
{
    public const int FreeMaxIntegrations = 1;
    public const int ProMaxIntegrations = 3;
    public const int BusinessMaxIntegrations = int.MaxValue;
}
```

#### 1.5 Extensions (`src/Shared/Extensions/`)

**`StringExtensions.cs`**
```csharp
namespace WeeklyUp.Shared.Extensions;

public static class StringExtensions
{
    public static string TrimSafe(this string? value) =>
        value?.Trim() ?? string.Empty;

    public static bool IsNullOrWhiteSpace(this string? value) =>
        string.IsNullOrWhiteSpace(value);

    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
```

**`DateTimeExtensions.cs`**
```csharp
namespace WeeklyUp.Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateOnly ToDateOnly(this DateTime dateTime) =>
        DateOnly.FromDateTime(dateTime);

    public static DateTime ToStartOfDay(this DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue);

    public static DateTime ToEndOfDay(this DateOnly date) =>
        date.ToDateTime(TimeOnly.MaxValue);

    public static DateOnly GetPreviousMonday(this DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.AddDays(-daysSinceMonday - 7);
    }
}
```

**`EnumerableExtensions.cs`**
```csharp
namespace WeeklyUp.Shared.Extensions;

public static class EnumerableExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) =>
        source is null || !source.Any();
}
```

**`QueryableExtensions.cs`**
```csharp
namespace WeeklyUp.Shared.Extensions;

using WeeklyUp.Shared.Pagination;

public static class QueryableExtensions
{
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await CountAsync(source, cancellationToken);
        var items = await ToListAsync(
            source.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize),
            cancellationToken);

        return PagedList<T>.Create(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }

    // Nota: CountAsync e ToListAsync serao extension methods do EF Core
    // usados via Microsoft.EntityFrameworkCore. Este arquivo vive no Shared
    // mas sera consumido pela Infrastructure. Alternativa: mover para Infrastructure.
    // Decisao: manter no Shared como metodos genericos, a Infrastructure
    // adiciona o adapter EF Core.
    private static Task<int> CountAsync<T>(IQueryable<T> source, CancellationToken ct) =>
        Task.FromResult(source.Count()); // Placeholder - sera substituido por EF Core na Infrastructure

    private static Task<List<T>> ToListAsync<T>(IQueryable<T> source, CancellationToken ct) =>
        Task.FromResult(source.ToList()); // Placeholder
}
```

> **NOTA IMPORTANTE:** O `QueryableExtensions.ToPagedListAsync` na pratica sera implementado na Infrastructure layer com acesso ao EF Core. No Shared, manteremos apenas o `PagedList<T>` e `PaginationParams`. A extension real que usa `EntityFrameworkQueryableExtensions` ficara em `Infrastructure/Persistence/Extensions/`.

### Criterios de Aceitacao -- Fase 1
- [ ] `Result<T>` com `Success`, `Failure`, `Match`, conversao implicita
- [ ] `Error` com factory methods para cada `ErrorType`
- [ ] `ValidationError` com lista de erros
- [ ] `PagedList<T>` com metadados de paginacao
- [ ] `PaginationParams` com limites (max 50, default 10)
- [ ] `Guard` clauses para null, empty, negative
- [ ] Constantes centralizadas e tipadas
- [ ] Extensions utilitarias testadas
- [ ] `dotnet build` sem warnings

### Testes que Devem Passar -- Fase 1
- `Result<T>.Success` retorna `IsSuccess = true`
- `Result<T>.Failure` retorna `IsFailure = true`
- `Result<T>.Match` executa callback correto
- `PagedList<T>.Create` calcula `TotalPages` e `HasNextPage` corretamente
- `PaginationParams.PageSize` respeita `MaxPageSize`
- `Guard.AgainstNull` lanca `ArgumentNullException`
- `Guard.AgainstNullOrWhiteSpace` lanca para strings vazias

---

## Fase 2: Domain Layer

### Objetivo
Implementar todas as entidades, value objects, enums, events, exceptions e interfaces do dominio. Zero dependencias externas.

### Pre-requisitos
- Fase 1 concluida
- Domain.csproj sem PackageReference (confirmado)

### 2.1 Base Classes

**`src/Domain/Common/Entity.cs`**
```csharp
namespace WeeklyUp.Domain.Common;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected init; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Construtor para rehidratacao do EF Core
    protected Entity(Guid id)
    {
        Id = id;
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) =>
        obj is Entity entity && Equals(entity);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) =>
        Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) =>
        !Equals(left, right);
}
```

**`src/Domain/Common/AggregateRoot.cs`**
```csharp
namespace WeeklyUp.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    protected AggregateRoot() { }
    protected AggregateRoot(Guid id) : base(id) { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**`src/Domain/Common/IDomainEvent.cs`**
```csharp
namespace WeeklyUp.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
```

**`src/Domain/Common/ValueObject.cs`**
```csharp
namespace WeeklyUp.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType()) return false;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) =>
        obj is ValueObject vo && Equals(vo);

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}
```

### 2.2 Enums

| Arquivo | Valores |
|---------|---------|
| `PlanType.cs` | `Free = 0, Pro = 1, Business = 2` |
| `IntegrationProvider.cs` | `GoogleAnalytics4 = 1, Stripe = 2, Manual = 3` |
| `IntegrationStatus.cs` | `Connected = 1, Disconnected = 2, Error = 3, Syncing = 4` |
| `BusinessType.cs` | `Ecommerce = 1, Services = 2, Content = 3, Other = 99` |
| `DayOfWeekPreference.cs` | `Monday = 1, ..., Sunday = 0` |
| `ReportStatus.cs` | `Pending = 0, Generating = 1, Generated = 2, Sent = 3, Failed = 4` |

### 2.3 Value Objects -- Especificacao Detalhada

| Value Object | Propriedades | Invariantes | Factory Method |
|---|---|---|---|
| **Email** | `string Value` | Regex `^[^@\s]+@[^@\s]+\.[^@\s]+$`, max 255, lowercase, trimmed | `Result<Email> Create(string email)` |
| **Money** | `decimal Amount, string Currency` | Amount arredondado 2 casas. Currency default "BRL". | `Money.BRL(decimal)`, `Money.Zero` |
| **Percentage** | `decimal Value` | Arredondado 1 casa. | `Percentage.FromValue(decimal)`, `Percentage.CalculateChange(current, previous)` |
| **DateRange** | `DateOnly Start, DateOnly End` | End >= Start. Max 7 dias. | `Result<DateRange> Create(start, end)`, `DateRange.PreviousWeek()`, `DateRange.CurrentWeek()` |
| **BusinessName** | `string Value` | Min 2, max 100, trimmed | `Result<BusinessName> Create(string)` |
| **Demographics** | `GenderDistribution, AgeGroups, TopCities, TopStates, Devices, TrafficSources` | TopCities max 10, TopStates max 5, ordered desc by percentage | Construtor publico com validacao |
| **ReportMetrics** | `Money Revenue, int SalesCount, Money AverageTicket, int NewCustomers, int TotalVisits, int UniqueVisitors, int PageViews, string? TopPage, string? TopTrafficSource, Money? PreviousRevenue, int? PreviousVisits` | Nenhum campo negativo | Construtor com validacao |
| **ReportInsights** | `string Highlight, string Alert, string Tip, DateTime GeneratedAt` | Nenhum campo null | Construtor com validacao |

### 2.4 Entities -- Especificacao Detalhada

#### User (AggregateRoot)

| Propriedade | Tipo | Regras |
|---|---|---|
| Id | Guid | Gerado automaticamente |
| Email | Email (VO) | Unico, imutavel apos criacao |
| Name | string | Max 255, obrigatorio |
| BusinessName | BusinessName (VO) | Obrigatorio |
| BusinessType | BusinessType (enum) | Obrigatorio |
| Timezone | string | Default "America/Sao_Paulo" |
| Plan | PlanType (enum) | Default Free. So pode subir (Free -> Pro -> Business) |
| ExternalAuthId | string? | Para OAuth (Google) |
| PhoneNumber | string? | Para WhatsApp |
| IsActive | bool | Default true |
| IsEmailVerified | bool | Default false |
| Integrations | `IReadOnlyCollection<Integration>` | Navegacao (backing field `_integrations`) |

**Metodos de dominio:**
- `static Result<User> Create(email, name, businessName, businessType, externalAuthId?)` -- Factory
- `Result<Integration> AddIntegration(provider, accessToken, refreshToken, providerAccountId, propertyId?, encryptor)` -- Valida limite do plano
- `Result<bool> RemoveIntegration(provider)` -- Marca como Disconnected
- `Result<bool> UpgradePlan(newPlan)` -- Valida que novo plano > atual
- `void VerifyEmail()` -- Marca `IsEmailVerified = true`
- `void UpdateProfile(name, businessName, businessType)` -- Atualiza campos mutaveis
- `void SetPhoneNumber(phone)` -- Para WhatsApp
- `void Deactivate()` -- Soft delete
- `bool CanAccessDemographics()` -- `Plan >= Pro`
- `bool CanAccessInsights()` -- `Plan >= Pro`
- `bool CanAccessWhatsApp()` -- `Plan >= Business`

**Domain Events disparados:**
- `UserRegisteredEvent` em `Create()`
- `UserEmailVerifiedEvent` em `VerifyEmail()`
- `UserPlanUpgradedEvent` em `UpgradePlan()`

#### Integration (Entity, pertence ao agregado User)

| Propriedade | Tipo | Regras |
|---|---|---|
| Id | Guid | Gerado automaticamente |
| UserId | Guid | FK para User |
| Provider | IntegrationProvider | Obrigatorio |
| AccessToken | string | Criptografado (AES-256) |
| RefreshToken | string | Criptografado |
| ProviderAccountId | string | ID da conta no provider |
| PropertyId | string? | GA4 property ID |
| Status | IntegrationStatus | Default Connected |
| LastSyncAt | DateTime? | Ultima sincronizacao |
| LastError | string? | Ultimo erro |

**Metodos:** `UpdateTokens()`, `MarkSynced()`, `MarkError(string)`, `MarkTokenExpired()`, `Disconnect()`

#### Report (AggregateRoot)

| Propriedade | Tipo | Regras |
|---|---|---|
| Id | Guid | Gerado automaticamente |
| UserId | Guid | FK para User |
| WeekRange | DateRange (VO) | Obrigatorio |
| Status | ReportStatus | Default Pending |
| Metrics | ReportMetrics (VO) | JSONB no PostgreSQL |
| Demographics | Demographics? (VO) | JSONB. Null para plano Free |
| Insights | ReportInsights? (VO) | JSONB. Null para plano Free |
| EmailSentAt | DateTime? | Quando email foi enviado |
| WhatsAppSentAt | DateTime? | Quando WhatsApp foi enviado |

**Metodos:** `static Report Create()`, `AddInsights()`, `SetDemographics()`, `MarkEmailSent()`, `MarkWhatsAppSent()`, `MarkGenerated()`, `MarkSent()`, `MarkFailed(string)`

**Domain Events:** `ReportGeneratedEvent` em `Create()`

#### ReportPreference (Entity)

| Propriedade | Tipo | Regras |
|---|---|---|
| UserId | Guid | FK, unico |
| SendDay | DayOfWeekPreference | Default Monday |
| SendTime | TimeOnly | Default 07:00 |
| Language | string | Default "pt-BR" |
| EnabledSections | `List<string>` | JSONB |

**Metodos:** `static CreateDefault(userId)`, `Update(day, time, sections)`

#### ManualMetric (Entity)

| Propriedade | Tipo | Regras |
|---|---|---|
| UserId | Guid | FK |
| WeekStart | DateOnly | Deve ser uma segunda-feira |
| Revenue | decimal? | >= 0 |
| SalesCount | int? | >= 0 |
| NewCustomers | int? | >= 0 |
| Visits | int? | >= 0 |

**Metodos:** `static Create(...)`, `Update(...)`

### 2.5 Domain Events

| Evento | Campos | Disparado por |
|---|---|---|
| `UserRegisteredEvent` | UserId, Email | `User.Create()` |
| `UserEmailVerifiedEvent` | UserId | `User.VerifyEmail()` |
| `UserPlanUpgradedEvent` | UserId, PreviousPlan, NewPlan | `User.UpgradePlan()` |
| `IntegrationConnectedEvent` | UserId, Provider | `User.AddIntegration()` |
| `IntegrationDisconnectedEvent` | UserId, Provider | `User.RemoveIntegration()` |
| `IntegrationSyncFailedEvent` | IntegrationId, Provider, Error | `Integration.MarkError()` |
| `ReportGeneratedEvent` | ReportId, UserId, WeekRange | `Report.Create()` |
| `ReportSentEvent` | ReportId, UserId, Channel (Email/WhatsApp) | `Report.MarkEmailSent()` / `MarkWhatsAppSent()` |

### 2.6 Interfaces

**Repository Interfaces:**

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

public interface IReportPreferenceRepository
{
    Task<ReportPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(ReportPreference pref, CancellationToken ct = default);
    void Update(ReportPreference pref);
}
```

**Service Interfaces:**

```csharp
public interface IDataSourceProvider
{
    IntegrationProvider ProviderType { get; }
    Task<CollectedMetrics> CollectMetricsAsync(Integration integration, DateRange range, CancellationToken ct = default);
    Task<DemographicData?> CollectDemographicsAsync(Integration integration, DateRange range, CancellationToken ct = default);
    Task<bool> ValidateConnectionAsync(Integration integration, CancellationToken ct = default);
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
    Task<Result<bool>> SendWeeklyReportAsync(string recipientEmail, string recipientName, Report report, CancellationToken ct = default);
    Task<Result<bool>> SendWelcomeAsync(string recipientEmail, string recipientName, CancellationToken ct = default);
}

public interface IWhatsAppSender
{
    Task<Result<bool>> SendWeeklyReportAsync(string phoneNumber, string recipientName, Report report, CancellationToken ct = default);
}

public interface ITokenEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public interface IReportGeneratorService
{
    Task<Result<Report>> GenerateForUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IIntegrationRepository Integrations { get; }
    IReportRepository Reports { get; }
    IManualMetricRepository ManualMetrics { get; }
    IReportPreferenceRepository ReportPreferences { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

> **NOTA:** `CollectedMetrics` e `DemographicData` sao modelos definidos na Application layer (sao DTOs de dados coletados, nao entidades de dominio). As interfaces do Domain referenciam esses tipos por meio de um pacote de abstractions ou os tipos vivem no Domain como records simples.

### Criterios de Aceitacao -- Fase 2
- [ ] Domain.csproj sem nenhum PackageReference
- [ ] Todas as entidades com construtores privados e factory methods
- [ ] Value Objects imutaveis com validacao na criacao
- [ ] `User.AddIntegration` respeita limite do plano
- [ ] `User.UpgradePlan` so permite upgrade (nao downgrade)
- [ ] Domain Events disparados nos momentos corretos
- [ ] 100% de cobertura de testes no Domain

### Testes que Devem Passar -- Fase 2

**User:**
- `Create_ValidData_ReturnsSuccess`
- `Create_InvalidEmail_ReturnsFailure`
- `Create_EmptyName_ReturnsFailure`
- `AddIntegration_FreePlan_LimitOne`
- `AddIntegration_ProPlan_LimitThree`
- `AddIntegration_DuplicateProvider_ReturnsFailure`
- `RemoveIntegration_Existing_DisconnectsSuccessfully`
- `RemoveIntegration_NotFound_ReturnsFailure`
- `UpgradePlan_FreeToProSucceeds`
- `UpgradePlan_ProToFree_Fails`
- `CanAccessDemographics_Free_False`
- `CanAccessDemographics_Pro_True`
- `CanAccessWhatsApp_OnlyBusiness`
- `Create_RaisesUserRegisteredEvent`
- `UpgradePlan_RaisesUserPlanUpgradedEvent`

**Email (VO):**
- `Create_ValidEmail_Success`
- `Create_InvalidFormat_Failure`
- `Create_TooLong_Failure`
- `Create_NormalizesToLowercase`

**Money (VO):**
- `BRL_RoundsTwoDecimals`
- `Add_SameCurrency_Works`
- `Add_DifferentCurrency_ThrowsDomainException`
- `ToFormattedString_ShowsBRL`
- `Zero_ReturnsZeroAmount`

**DateRange (VO):**
- `Create_EndBeforeStart_Failure`
- `Create_MoreThan7Days_Failure`
- `PreviousWeek_ReturnsMonToSun`
- `Contains_DateInRange_True`
- `Contains_DateOutOfRange_False`

**Percentage (VO):**
- `CalculateChange_Growth_Positive`
- `CalculateChange_Decline_Negative`
- `CalculateChange_PreviousZero_Returns100`

**Report:**
- `Create_SetsStatusAndRaisesEvent`
- `AddInsights_SetsInsightsAndUpdatesTimestamp`
- `MarkEmailSent_SetsTimestamp`

---

## Fase 3: Application Layer

### Objetivo
Implementar todos os Commands, Queries, Handlers, DTOs, Validators e Pipeline Behaviors usando Mediator (source generator) e Mapperly.

### Pre-requisitos
- Fase 2 concluida (Domain completo com testes passando)

### 3.1 Diferenca Critica: Mediator vs MediatR

O pacote **Mediator** (martinothamar) usa source generation. As interfaces sao diferentes:

```csharp
// MediatR (docs anteriores - NAO USAR)
public sealed record MyCommand : IRequest<Result<MyDto>>;
public sealed class MyHandler : IRequestHandler<MyCommand, Result<MyDto>>

// Mediator (stack real - USAR)
public sealed record MyCommand : ICommand<Result<MyDto>>;
public sealed class MyHandler : ICommandHandler<MyCommand, Result<MyDto>>

// Para queries:
public sealed record MyQuery : IQuery<Result<MyDto>>;
public sealed class MyHandler : IQueryHandler<MyQuery, Result<MyDto>>

// Pipeline behaviors sao iguais na interface:
public sealed class ValidationBehavior<TMessage, TResponse>
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
```

### 3.2 Mapperly -- Como Usar

```csharp
// src/Application/Common/Mappings/ReportMapper.cs
using Riok.Mapperly.Abstractions;

namespace WeeklyUp.Application.Common.Mappings;

[Mapper]
public static partial class ReportMapper
{
    public static partial ReportDto ToDto(this Report report);
    public static partial ReportSummaryDto ToSummaryDto(this Report report);
    public static partial MetricsDto ToDto(this ReportMetrics metrics);
    public static partial DemographicsDto ToDto(this Demographics demographics);
    public static partial InsightsDto ToDto(this ReportInsights insights);

    // Mapeamentos customizados quando necessario
    [MapProperty(nameof(Report.WeekRange), nameof(ReportDto.WeekLabel))]
    private static string MapWeekLabel(DateRange range) => range.ToString();
}
```

### 3.3 Pipeline Behaviors

| Behavior | Ordem | Responsabilidade |
|----------|-------|-----------------|
| `ValidationBehavior` | 1 | Executa FluentValidation. Lanca `ValidationException` se invalido. |
| `LoggingBehavior` | 2 | Loga request name + UserId. Mede tempo. Warn se > 500ms. |
| `PerformanceBehavior` | 3 | Loga metricas de performance para slow requests. |
| `TransactionBehavior` | 4 | Wrapa Commands em transacao (via `IUnitOfWork`). |

**Exemplo ValidationBehavior com Mediator:**

```csharp
namespace WeeklyUp.Application.Common.Behaviors;

using FluentValidation;
using Mediator;

public sealed class ValidationBehavior<TMessage, TResponse>
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IEnumerable<IValidator<TMessage>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TMessage>> validators) =>
        _validators = validators;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken ct,
        MessageHandlerDelegate<TMessage, TResponse> next)
    {
        if (!_validators.Any())
            return await next(message, ct);

        var context = new ValidationContext<TMessage>(message);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new Exceptions.ValidationException(failures);

        return await next(message, ct);
    }
}
```

### 3.4 Commands -- Especificacao Completa

#### RegisterUser

```csharp
// Command
public sealed record RegisterUserCommand(
    string Email,
    string Name,
    string BusinessName,
    string Timezone,
    BusinessType BusinessType,
    string? ExternalAuthId = null
) : ICommand<Result<UserProfileDto>>;

// Validator
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .WithMessage("Email invalido.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255)
            .WithMessage("Nome e obrigatorio (max 255 caracteres).");
        RuleFor(x => x.BusinessName).NotEmpty().MinimumLength(2).MaximumLength(100)
            .WithMessage("Nome do negocio deve ter entre 2 e 100 caracteres.");
        RuleFor(x => x.Timezone).NotEmpty()
            .WithMessage("Timezone e obrigatorio.");
    }
}

// Handler
public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _uow;

    // 1. Verifica email duplicado
    // 2. Cria User via factory method
    // 3. Cria ReportPreference default
    // 4. Persiste e retorna DTO
}
```

#### Todos os Commands

| Command | Request Fields | Response | Validator Rules |
|---------|---------------|----------|-----------------|
| `RegisterUserCommand` | Email, Name, BusinessName, Timezone, BusinessType | `Result<UserProfileDto>` | Email valido, Name max 255, BusinessName 2-100 |
| `VerifyEmailCommand` | UserId, Token | `Result<bool>` | UserId nao vazio, Token nao vazio |
| `UpdateUserProfileCommand` | UserId, Name, BusinessName, BusinessType | `Result<UserProfileDto>` | Name max 255, BusinessName 2-100 |
| `UpgradePlanCommand` | UserId, NewPlan | `Result<bool>` | UserId nao vazio, NewPlan valido |
| `ConnectIntegrationCommand` | UserId, Provider, AuthCode | `Result<IntegrationDto>` | UserId nao vazio, AuthCode nao vazio |
| `DisconnectIntegrationCommand` | UserId, IntegrationId | `Result<bool>` | Ambos nao vazios |
| `AddManualMetricCommand` | UserId, WeekStart, Revenue?, SalesCount?, NewCustomers?, Visits? | `Result<bool>` | UserId nao vazio, valores >= 0 |
| `UpdateReportPreferencesCommand` | UserId, SendDay, SendTime | `Result<bool>` | UserId nao vazio, SendTime valido |
| `GenerateWeeklyReportCommand` | UserId, WeekStart? | `Result<ReportDto>` | UserId nao vazio |
| `SendWeeklyReportCommand` | ReportId | `Result<bool>` | ReportId nao vazio |

### 3.5 Queries -- Especificacao Completa

| Query | Request Fields | Response | Cache TTL |
|-------|---------------|----------|-----------|
| `GetUserDashboardQuery` | UserId, WeekStart? | `Result<UserDashboardResponse>` | 30 min |
| `GetReportHistoryQuery` | UserId, PaginationParams | `Result<PagedList<ReportSummaryDto>>` | 5 min |
| `GetReportDetailQuery` | UserId, ReportId | `Result<ReportDetailDto>` | 1 hora |
| `GetUserProfileQuery` | UserId | `Result<UserProfileDto>` | 24 horas |
| `GetIntegrationsQuery` | UserId | `Result<List<IntegrationDto>>` | 5 min |

### 3.6 DependencyInjection.cs

```csharp
namespace WeeklyUp.Application;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Mediator e registrado via source generator automaticamente
        // Apenas registrar Validators
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline behaviors sao registrados via Mediator options no Program.cs

        return services;
    }
}
```

### Criterios de Aceitacao -- Fase 3
- [ ] Todos os Commands retornam `Result<TDto>`, nunca entidades
- [ ] Todos os Queries usam ICacheService quando aplicavel
- [ ] Todos os Commands tem Validator correspondente
- [ ] ValidationBehavior funcional com Mediator pipeline
- [ ] Mapperly mappers compilam sem erros
- [ ] CancellationToken propagado em todos os handlers
- [ ] Application.csproj nao referencia EF Core
- [ ] Testes com > 80% de cobertura

### Testes que Devem Passar -- Fase 3

**Handlers:**
- `RegisterUser_ValidData_CreatesUserAndReturnsDto`
- `RegisterUser_DuplicateEmail_ReturnsFailure`
- `GenerateWeeklyReport_UserNotFound_ReturnsFailure`
- `GenerateWeeklyReport_DuplicateReport_ReturnsFailure`
- `GenerateWeeklyReport_ValidUser_CreatesReportAndSaves`
- `SendWeeklyReport_ReportNotFound_ReturnsFailure`
- `SendWeeklyReport_AlreadySent_ReturnsFailure`
- `GetUserDashboard_CacheHit_ReturnsCachedData`
- `GetUserDashboard_CacheMiss_QueriesDbAndCaches`

**Behaviors:**
- `ValidationBehavior_NoValidators_CallsNext`
- `ValidationBehavior_WithFailures_ThrowsValidationException`
- `PerformanceBehavior_SlowRequest_LogsWarning`

---

## Fase 4: Infrastructure Layer

### Objetivo
Implementar todas as classes concretas: EF Core, repositories, providers externos, cache, email, AI, jobs.

### Pre-requisitos
- Fase 3 concluida (Application layer completa)

### 4.1 EF Core -- ApplicationDbContext

```csharp
namespace WeeklyUp.Infrastructure.Persistence;

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

### 4.2 Entity Configurations

Cada entidade tem sua propria `IEntityTypeConfiguration<T>`:

| Entidade | Tabela | JSONB Columns | Indices |
|----------|--------|---------------|---------|
| User | `users` | - | `ix_users_email` (unique), `ix_users_external_auth_id` |
| Integration | `integrations` | - | `ix_integrations_user_provider` (unique, filtered active) |
| Report | `reports` | `metrics`, `demographics`, `insights` | `ix_reports_user_created` (desc), `ix_reports_user_week` (unique) |
| ReportPreference | `report_preferences` | `enabled_sections` | `ix_report_preferences_user` (unique) |
| ManualMetric | `manual_metrics` | `custom_metrics` | `ix_manual_metrics_user_week` (unique) |

**Convencoes de mapeamento:**

```csharp
// Value Objects mapeados via OwnsOne
builder.OwnsOne(u => u.Email, email =>
{
    email.Property(e => e.Value)
        .HasColumnName("email")
        .HasMaxLength(255)
        .IsRequired();
});

// JSONB para objetos complexos
builder.OwnsOne(r => r.Metrics, m => m.ToJson("metrics"));
builder.OwnsOne(r => r.Demographics, d => d.ToJson("demographics"));
builder.OwnsOne(r => r.Insights, i => i.ToJson("insights"));

// Enums como string
builder.Property(u => u.Plan)
    .HasColumnName("plan")
    .HasConversion<string>()
    .HasDefaultValue(PlanType.Free);

// Ignorar DomainEvents
builder.Ignore(u => u.DomainEvents);
```

### 4.3 Interceptors

| Interceptor | Responsabilidade |
|---|---|
| `AuditableEntityInterceptor` | Seta `UpdatedAt = DateTime.UtcNow` em entidades modificadas |
| `DomainEventDispatchInterceptor` | Coleta domain events de AggregateRoots, salva, despacha via Mediator apos `SaveChanges` |

### 4.4 External Providers

| Provider | Interface | Detalhes |
|---|---|---|
| `GoogleAnalyticsProvider` | `IDataSourceProvider` | Refit client para GA4 Data API. Retry com Polly (3 tentativas, backoff exponencial). Circuit breaker (5 falhas, 30s break). |
| `StripeDataProvider` | `IDataSourceProvider` | Stripe.net SDK. Lista charges e customers por periodo. |
| `ManualDataProvider` | `IDataSourceProvider` | Le do `IManualMetricRepository`. |
| `ClaudeInsightGenerator` | `IInsightGenerator` | HTTP client para Anthropic API. Prompt em portugues. Fallback para insights estaticos. |
| `ResendEmailSender` | `IEmailSender` | HTTP client para Resend API. Template HTML responsivo. |
| `TwilioWhatsAppSender` | `IWhatsAppSender` | Twilio SDK. Mensagem formatada com emojis. |
| `AesTokenEncryptor` | `ITokenEncryptor` | AES-256 com key/IV do configuration. |

### 4.5 Background Jobs (Hangfire)

| Job | Schedule | Logica |
|---|---|---|
| `WeeklyReportGenerationJob` | Toda segunda 6h (America/Sao_Paulo) | Busca usuarios ativos com preferencia para Monday. Dispara `GenerateWeeklyReportCommand` para cada. |
| `ReportSendingJob` | Apos geracao (enqueued por domain event) | Envia por email. Se Business e tem telefone, envia por WhatsApp. |
| `IntegrationSyncJob` | Diario 3h | Sincroniza dados de todas as integracoes ativas. Atualiza `LastSyncAt`. |
| `TokenRefreshJob` | A cada 6h | Busca integracoes com tokens proximos de expirar. Renova via OAuth refresh flow. |

### 4.6 Cache (Redis)

| Chave | TTL | Invalidacao |
|---|---|---|
| `dashboard:{userId}` | 30 min | Quando novo report e gerado (`ReportGeneratedEventHandler`) |
| `user-profile:{userId}` | 24h | Quando perfil e atualizado |
| `integrations:{userId}` | 5 min | Quando integracao e adicionada/removida |
| `report:{reportId}` | 1h | Imutavel (report nao muda apos gerado) |

### 4.7 DependencyInjection.cs da Infrastructure

Registro completo de todos os servicos:
- DbContext com interceptors
- Repositories (Scoped)
- UnitOfWork (Scoped)
- Data Providers (Scoped)
- External services (Scoped/Singleton conforme caso)
- Cache (Singleton para connection, Scoped para service)
- Refit clients com Polly policies
- Hangfire com PostgreSQL storage

### Criterios de Aceitacao -- Fase 4
- [ ] Migration gera corretamente e aplica no PostgreSQL
- [ ] Repositories CRUD funcionais contra banco real
- [ ] UnitOfWork despacha domain events apos save
- [ ] Refit clients configurados com retry e circuit breaker
- [ ] Cache Redis funcional com serialization JSON
- [ ] Hangfire registra jobs recurring corretamente
- [ ] Tokens OAuth criptografados/descriptografados corretamente

---

## Fase 5: API Layer (Carter Modules)

### Objetivo
Implementar todos os endpoints, middleware e o Program.cs completo.

### Pre-requisitos
- Fase 4 concluida

### 5.1 Endpoints

| Modulo | Rota | Metodo | Handler (Mediator) | Auth |
|--------|------|--------|--------------------|------|
| **AuthEndpoints** | `/api/auth/register` | POST | `RegisterUserCommand` | Nao |
| | `/api/auth/login` | POST | `LoginCommand` | Nao |
| | `/api/auth/verify-email` | POST | `VerifyEmailCommand` | Nao |
| | `/api/auth/refresh` | POST | `RefreshTokenCommand` | Nao |
| **UserEndpoints** | `/api/users/me` | GET | `GetUserProfileQuery` | Sim |
| | `/api/users/me` | PUT | `UpdateUserProfileCommand` | Sim |
| | `/api/users/me/integrations` | GET | `GetIntegrationsQuery` | Sim |
| **IntegrationEndpoints** | `/api/integrations/connect` | POST | `ConnectIntegrationCommand` | Sim |
| | `/api/integrations/{id}` | DELETE | `DisconnectIntegrationCommand` | Sim |
| **ReportEndpoints** | `/api/reports` | GET | `GetReportHistoryQuery` | Sim |
| | `/api/reports/{id}` | GET | `GetReportDetailQuery` | Sim |
| | `/api/reports/dashboard` | GET | `GetUserDashboardQuery` | Sim |
| **MetricsEndpoints** | `/api/metrics/manual` | POST | `AddManualMetricCommand` | Sim |
| | `/api/metrics/manual` | GET | `GetManualMetricsQuery` | Sim |
| **WebhookEndpoints** | `/api/webhooks/stripe` | POST | `ProcessStripeWebhookCommand` | API Key |

### 5.2 Padrao de um Carter Module

```csharp
namespace WeeklyUp.Api.Endpoints;

using Carter;
using Mediator;
using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Reports.Queries.GetReportDetail;

public sealed class ReportEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .RequireAuthorization()
            .WithTags("Reports");

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetReportDetailQuery(ctx.User.GetUserId(), id);
            var result = await mediator.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("GetReportDetail")
        .Produces<ApiResponse<ReportDetailDto>>(200)
        .Produces<ErrorResponse>(404);
    }
}
```

### 5.3 Middleware

| Middleware | Ordem | Responsabilidade |
|---|---|---|
| `CorrelationIdMiddleware` | 1 | Gera/propaga correlation ID no header |
| `ExceptionHandlingMiddleware` | 2 | Captura exceptions, mapeia para HTTP status codes |
| `RequestLoggingMiddleware` | 3 | Loga metodo, path, status code, duracao |

**Mapeamento de exceptions para HTTP status:**

| Exception | HTTP Status | Error Code |
|---|---|---|
| `ValidationException` | 400 | `VALIDATION_ERROR` |
| `NotFoundException` | 404 | `NOT_FOUND` |
| `ForbiddenException` | 403 | `FORBIDDEN` |
| `DomainException` | 422 | `DOMAIN_ERROR` |
| Qualquer outra | 500 | `INTERNAL_ERROR` |

### 5.4 Program.cs -- Estrutura Completa

```csharp
// 1. Builder configuration
var builder = WebApplication.CreateBuilder(args);

// 2. Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// 3. Clean Architecture DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Mediator (source generated -- registrado automaticamente)
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

// 5. Carter
builder.Services.AddCarter();

// 6. OpenAPI
builder.Services.AddOpenApi();

// 7. JWT Auth
builder.Services.AddAuthentication(...)
    .AddJwtBearer(...);
builder.Services.AddAuthorization();

// 8. Rate Limiting
builder.Services.AddRateLimiter(...);

// 9. CORS
builder.Services.AddCors(...);

// 10. Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(...)
    .AddRedis(...);

// --- Build ---
var app = builder.Build();

// 11. Middleware pipeline (ordem importa!)
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// 12. OpenAPI + Scalar (dev only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("WeeklyUp API").WithTheme(ScalarTheme.Moon));
}

// 13. Auth + CORS
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// 14. Carter (mapeia todos os ICarterModule)
app.MapCarter();

// 15. Health checks
app.MapHealthChecks("/health");

// 16. Hangfire
app.UseHangfireDashboard("/hangfire");
// Registrar recurring jobs...

app.Run();

public partial class Program; // Para WebApplicationFactory nos testes
```

### Criterios de Aceitacao -- Fase 5
- [ ] Todos os endpoints respondem com status codes corretos
- [ ] Endpoints protegidos retornam 401 sem JWT
- [ ] Rate limiting funcional (429 apos limite)
- [ ] Scalar UI acessivel em /scalar/v1 (dev mode)
- [ ] Health check retorna healthy com DB e Redis
- [ ] ExceptionHandlingMiddleware mapeia todas as exceptions corretamente
- [ ] CORS configurado apenas para frontend URL

---

## Fase 6: Testes

### Objetivo
Garantir coverage minimo e integridade arquitetural.

### 6.1 Architecture Tests (NetArchTest)

```csharp
// Testes obrigatorios
[Fact] Domain_Not_Depend_On_Application
[Fact] Domain_Not_Depend_On_Infrastructure
[Fact] Domain_Not_Depend_On_EFCore
[Fact] Domain_Not_Depend_On_Mediator
[Fact] Application_Not_Depend_On_Infrastructure
[Fact] Application_Not_Depend_On_Api
[Fact] Handlers_Should_Be_Sealed
[Fact] Endpoints_Should_Be_Sealed
[Fact] Entities_Should_Have_Private_Constructors
[Fact] Commands_Should_End_With_Command
[Fact] Queries_Should_End_With_Query
[Fact] Handlers_Should_End_With_Handler
[Fact] Validators_Should_End_With_Validator
```

### 6.2 Cobertura por Camada

| Camada | Cobertura Minima | Foco |
|--------|-----------------|------|
| Domain | 100% | Entities, Value Objects, Domain Logic |
| Application | 80% | Handlers (happy + error paths) |
| Infrastructure | 60% | Repositories, Mappers, Providers |
| Api | 50% | Endpoints via WebApplicationFactory |

### 6.3 Builders para Testes

```csharp
// Padrao Builder para criar entidades em testes
public sealed class UserBuilder
{
    private string _email = "test@example.com";
    private string _name = "Test User";
    private string _businessName = "Test Business";
    private BusinessType _businessType = BusinessType.Ecommerce;
    private PlanType _plan = PlanType.Free;

    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithName(string name) { _name = name; return this; }
    public UserBuilder WithPlan(PlanType plan) { _plan = plan; return this; }
    public UserBuilder WithBusinessName(string name) { _businessName = name; return this; }

    public User Build()
    {
        var user = User.Create(_email, _name, _businessName, _businessType).Value!;
        if (_plan != PlanType.Free)
        {
            if (_plan >= PlanType.Pro) user.UpgradePlan(PlanType.Pro);
            if (_plan >= PlanType.Business) user.UpgradePlan(PlanType.Business);
        }
        return user;
    }
}
```

### Criterios de Aceitacao -- Fase 6
- [ ] Todos os architecture tests passando
- [ ] Domain coverage >= 100%
- [ ] Application coverage >= 80%
- [ ] Zero testes falhando
- [ ] Builders criados para User, Report, Integration

---

## 10. Configuracoes e DevOps

### 10.1 appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=weeklyup;Username=wu_user;Password=<user-secrets>",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "<user-secrets>",
    "Issuer": "WeeklyUp",
    "Audience": "WeeklyUp.Client",
    "ExpirationMinutes": 60,
    "RefreshExpirationDays": 7
  },
  "Encryption": {
    "Key": "<user-secrets>",
    "IV": "<user-secrets>"
  },
  "Claude": {
    "ApiKey": "<user-secrets>",
    "Model": "claude-sonnet-4-5-20250514",
    "MaxTokens": 500
  },
  "Resend": {
    "ApiKey": "<user-secrets>",
    "FromEmail": "relatorio@weeklyup.com.br"
  },
  "Twilio": {
    "AccountSid": "<user-secrets>",
    "AuthToken": "<user-secrets>",
    "WhatsAppFrom": "+14155238886"
  },
  "Google": {
    "ClientId": "<user-secrets>",
    "ClientSecret": "<user-secrets>"
  },
  "Frontend": {
    "Url": "http://localhost:3000"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "Seq", "Args": { "serverUrl": "http://localhost:5341" } }
    ]
  }
}
```

### 10.2 Docker Compose

```yaml
services:
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

### 10.3 GitHub Actions CI

```yaml
name: CI
on:
  pull_request: { branches: [main] }
  push: { branches: [main] }

jobs:
  build-test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:17-alpine
        env: { POSTGRES_DB: wu_test, POSTGRES_USER: test, POSTGRES_PASSWORD: test }
        ports: ["5432:5432"]

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release --collect:"XPlat Code Coverage"
```

---

## 11. Checklist de Qualidade por Fase

### Fase 0 -- Setup
- [ ] `dotnet build` compila sem erros/warnings
- [ ] Referencias entre projetos respeitam Clean Architecture
- [ ] Central Package Management ativo

### Fase 1 -- Shared
- [ ] `Result<T>` com match pattern funcional
- [ ] `PagedList<T>` calcula paginacao corretamente
- [ ] Constantes centralizadas e sem duplicacao
- [ ] Testes unitarios passando

### Fase 2 -- Domain
- [ ] Zero PackageReference no Domain.csproj
- [ ] Entities com construtores privados e factory methods
- [ ] Value Objects imutaveis com validacao
- [ ] Domain Events disparados corretamente
- [ ] 100% cobertura de testes
- [ ] Architecture test: Domain nao depende de nada

### Fase 3 -- Application
- [ ] Commands retornam `Result<TDto>` (nunca entidades)
- [ ] Queries usam `ICacheService` quando aplicavel
- [ ] FluentValidation em todos os Commands
- [ ] Mapperly compila source-generated mappers
- [ ] Mediator pipeline behaviors funcionais
- [ ] CancellationToken propagado
- [ ] > 80% cobertura de testes
- [ ] Architecture test: Application nao depende de Infrastructure

### Fase 4 -- Infrastructure
- [ ] Migration aplica corretamente
- [ ] JSONB mapping funcional para Metrics/Demographics/Insights
- [ ] Repositories CRUD testados
- [ ] UnitOfWork despacha domain events
- [ ] Refit + Polly configurados
- [ ] Cache Redis funcional
- [ ] Hangfire registra jobs

### Fase 5 -- API
- [ ] Todos os endpoints retornam status codes corretos
- [ ] JWT auth funcional
- [ ] Rate limiting ativo
- [ ] Scalar UI acessivel
- [ ] Health checks respondendo
- [ ] CORS configurado

### Fase 6 -- Testes
- [ ] Architecture tests passando
- [ ] Domain 100%, Application 80%
- [ ] Integration tests contra DB real
- [ ] API tests via WebApplicationFactory
- [ ] Zero testes falhando

---

## 12. Padroes de Codigo com Exemplos

### 12.1 Entity Correta

```csharp
namespace WeeklyUp.Domain.Entities;

using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Events;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

public sealed class User : AggregateRoot
{
    // Propriedades com setters privados (encapsulamento)
    public Email Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public BusinessName BusinessName { get; private set; } = null!;
    public BusinessType BusinessType { get; private set; }
    public PlanType Plan { get; private set; }
    public bool IsActive { get; private set; }

    // Colecao navegacao com backing field
    private readonly List<Integration> _integrations = [];
    public IReadOnlyCollection<Integration> Integrations => _integrations.AsReadOnly();

    // Construtor privado (obriga uso do factory method)
    private User() { }

    // Factory method com validacao
    public static Result<User> Create(
        string email,
        string name,
        string businessName,
        BusinessType businessType)
    {
        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
            return emailResult.Error;

        var bizNameResult = BusinessName.Create(businessName);
        if (bizNameResult.IsFailure)
            return bizNameResult.Error;

        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("User.Name", "Nome e obrigatorio.");

        var user = new User
        {
            Email = emailResult.Value,
            Name = name.Trim(),
            BusinessName = bizNameResult.Value,
            BusinessType = businessType,
            Plan = PlanType.Free,
            IsActive = true
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, email));
        return user;
    }

    // Metodo de dominio com logica de negocio
    public Result<bool> UpgradePlan(PlanType newPlan)
    {
        if (newPlan <= Plan)
            return Error.Validation("User.Plan", "Novo plano deve ser superior ao atual.");

        var previous = Plan;
        Plan = newPlan;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserPlanUpgradedEvent(Id, previous, newPlan));
        return true;
    }

    // Feature flags baseadas no plano
    public bool CanAccessDemographics() => Plan >= PlanType.Pro;
    public bool CanAccessInsights() => Plan >= PlanType.Pro;
    public bool CanAccessWhatsApp() => Plan >= PlanType.Business;
}
```

### 12.2 Value Object Correto

```csharp
namespace WeeklyUp.Domain.ValueObjects;

using System.Text.RegularExpressions;
using WeeklyUp.Domain.Common;
using WeeklyUp.Shared.Results;

public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation("Email.Empty", "Email nao pode ser vazio.");

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 255)
            return Error.Validation("Email.TooLong", "Email nao pode exceder 255 caracteres.");

        if (!EmailRegex().IsMatch(email))
            return Error.Validation("Email.InvalidFormat", "Formato de email invalido.");

        return new Email(email);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
```

### 12.3 Command + Handler Correto (Mediator)

```csharp
// Command (record imutavel)
namespace WeeklyUp.Application.Users.Commands.RegisterUser;

using Mediator;
using WeeklyUp.Application.Users.DTOs;
using WeeklyUp.Shared.Results;

public sealed record RegisterUserCommand(
    string Email,
    string Name,
    string BusinessName,
    BusinessType BusinessType
) : ICommand<Result<UserProfileDto>>;

// Handler (sealed, uma responsabilidade)
namespace WeeklyUp.Application.Users.Commands.RegisterUser;

using Mediator;
using Microsoft.Extensions.Logging;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Application.Users.DTOs;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUnitOfWork uow,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async ValueTask<Result<UserProfileDto>> Handle(
        RegisterUserCommand command,
        CancellationToken ct)
    {
        // 1. Verificar duplicidade
        var existing = await _uow.Users.GetByEmailAsync(command.Email, ct);
        if (existing is not null)
            return Error.Conflict("User.EmailDuplicate", "Email ja cadastrado.");

        // 2. Criar entidade via factory method
        var userResult = User.Create(
            command.Email,
            command.Name,
            command.BusinessName,
            command.BusinessType);

        if (userResult.IsFailure)
            return userResult.Error;

        // 3. Criar preferencias default
        var preference = ReportPreference.CreateDefault(userResult.Value.Id);

        // 4. Persistir
        await _uow.Users.AddAsync(userResult.Value, ct);
        await _uow.ReportPreferences.AddAsync(preference, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} registered with email {Email}",
            userResult.Value.Id, command.Email);

        // 5. Retornar DTO (nunca entidade)
        return userResult.Value.ToProfileDto();
    }
}
```

### 12.4 Carter Module Correto

```csharp
namespace WeeklyUp.Api.Endpoints;

using Carter;
using Mediator;
using WeeklyUp.Api.Extensions;
using WeeklyUp.Api.Models;
using WeeklyUp.Application.Users.Commands.RegisterUser;
using WeeklyUp.Application.Users.DTOs;
using WeeklyUp.Application.Users.Queries.GetUserProfile;

public sealed class UserEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization()
            .WithTags("Users");

        group.MapGet("/me", GetProfile)
            .WithName("GetProfile")
            .Produces<ApiResponse<UserProfileDto>>(200)
            .Produces<ErrorResponse>(404);

        group.MapPut("/me", UpdateProfile)
            .WithName("UpdateProfile")
            .Produces<ApiResponse<UserProfileDto>>(200)
            .Produces<ErrorResponse>(400);
    }

    private static async Task<IResult> GetProfile(
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetUserProfileQuery(ctx.User.GetUserId());
        var result = await mediator.Send(query, ct);
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateProfile(
        UpdateUserProfileCommand command,
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var cmd = command with { UserId = ctx.User.GetUserId() };
        var result = await mediator.Send(cmd, ct);
        return result.ToApiResult();
    }
}
```

### 12.5 Repository Correto

```csharp
namespace WeeklyUp.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) =>
        _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByIdWithIntegrationsAsync(Guid id, CancellationToken ct = default) =>
        await _context.Users
            .Include(u => u.Integrations)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email, ct);

    public async Task<IReadOnlyList<User>> GetActiveUsersForReportAsync(
        DayOfWeekPreference day, CancellationToken ct = default)
    {
        // Join com ReportPreference para filtrar por dia de envio
        var userIds = await _context.ReportPreferences
            .Where(p => p.SendDay == day)
            .Select(p => p.UserId)
            .ToListAsync(ct);

        return await _context.Users
            .Include(u => u.Integrations)
            .Where(u => u.IsActive && userIds.Contains(u.Id))
            .ToListAsync(ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);

    public void Update(User user) =>
        _context.Users.Update(user);
}
```

### 12.6 Teste xUnit + FluentAssertions + NSubstitute

```csharp
namespace WeeklyUp.Application.Tests.Users;

using FluentAssertions;
using NSubstitute;
using WeeklyUp.Application.Users.Commands.RegisterUser;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using Xunit;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _handler = new RegisterUserCommandHandler(
            _uow,
            Substitute.For<ILogger<RegisterUserCommandHandler>>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndReturnsDto()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "joao@email.com",
            Name: "Joao",
            BusinessName: "Loja do Joao",
            BusinessType: BusinessType.Ecommerce);

        _uow.Users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("joao@email.com");
        result.Value.Name.Should().Be("Joao");

        await _uow.Users.Received(1).AddAsync(
            Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictError()
    {
        // Arrange
        var existingUser = new UserBuilder().Build();
        _uow.Users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var command = new RegisterUserCommand(
            Email: "joao@email.com",
            Name: "Joao",
            BusinessName: "Loja",
            BusinessType: BusinessType.Ecommerce);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailDuplicate");

        await _uow.Users.DidNotReceive().AddAsync(
            Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "invalid",
            Name: "Joao",
            BusinessName: "Loja",
            BusinessType: BusinessType.Ecommerce);

        _uow.Users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
```

---

## Resumo Executivo

| Fase | Foco | Arquivos | Testes | Dependencia |
|------|------|----------|--------|-------------|
| **0** | Setup .csproj | 5 .csproj + 5 test .csproj | `dotnet build` | Nenhuma |
| **1** | Shared (Result, Pagination, Guards, Constants) | ~15 arquivos | ~20 testes | Fase 0 |
| **2** | Domain (Entities, VOs, Events, Interfaces) | ~35 arquivos | ~50 testes (100%) | Fase 1 |
| **3** | Application (Commands, Queries, Behaviors, Mappers) | ~45 arquivos | ~40 testes (80%) | Fase 2 |
| **4** | Infrastructure (EF, Repos, Providers, Jobs, Cache) | ~30 arquivos | ~20 testes | Fase 3 |
| **5** | API (Carter Modules, Middleware, Program.cs) | ~15 arquivos | ~15 testes | Fase 4 |
| **6** | Testes finais (Architecture, Integration, E2E) | ~10 arquivos | ~30 testes | Fase 5 |

**Total estimado:** ~150 arquivos de codigo + ~175 testes

**Principio fundamental:** Cada fase so inicia quando a anterior esta 100% completa com todos os testes passando. Quality-first, nao speed-first.

---

*Este documento e a referencia definitiva para implementacao do WeeklyUp. Todos os exemplos de codigo usam a stack real (Mediator source generator, Mapperly, .NET 10, C# 14).*
