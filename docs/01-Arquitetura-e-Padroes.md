# 📊 WeeklyUp — 01. Arquitetura e Padrões de Qualidade

> **Stack:** C# 14 / ASP.NET Core 10 — Minimal APIs (Backend) + Next.js 14 (Frontend Web) + .NET MAUI 10 (Mobile)  
> **Padrões:** Clean Architecture · SOLID · Clean Code · CQRS · DDD Tactical Patterns

---

## 1. Princípios Inegociáveis

### 1.1 Clean Architecture — Regra de Dependência

Todas as dependências apontam para dentro (em direção ao Domain). Nenhuma camada interna conhece camadas externas.

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│           (Minimal API Endpoints, Middleware)                │
│         Depende de: Application                             │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                         │
│          (Use Cases, Commands, Queries, DTOs)               │
│         Depende de: Domain                                  │
├─────────────────────────────────────────────────────────────┤
│                      Domain Layer                           │
│       (Entities, Value Objects, Interfaces, Events)         │
│         Depende de: NADA (zero dependências externas)       │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                        │
│     (EF Core, APIs externas, Email, Storage, Cache)         │
│         Implementa interfaces do Domain                     │
│         Depende de: Domain + Application (interfaces only)  │
└─────────────────────────────────────────────────────────────┘
```

**Regras absolutas:**

| Camada | ✅ PODE referenciar | ❌ NUNCA referencia |
|--------|--------------------|--------------------|
| **Domain** | System.* apenas | Application, Infrastructure, Api, EF Core |
| **Application** | Domain, MediatR, FluentValidation, Mapster | Infrastructure, Api, EF Core |
| **Infrastructure** | Domain, Application (interfaces) | Api |
| **Api** | Application (via MediatR) | Domain.Entities direto, Infrastructure direto |

### 1.2 SOLID — Aplicação Rigorosa

| Princípio | Aplicação no WeeklyUp |
|-----------|----------------------|
| **S — Single Responsibility** | Cada handler faz UMA coisa. `GenerateReportHandler` não envia email. `SendReportEmailHandler` não gera relatório. |
| **O — Open/Closed** | Novas integrações (Instagram, Hotmart) adicionadas via `IDataSourceProvider` sem alterar código existente. Strategy Pattern. |
| **L — Liskov Substitution** | `GoogleAnalyticsProvider` e `StripeProvider` são intercambiáveis via `IDataSourceProvider`. |
| **I — Interface Segregation** | `IReportRepository` separado de `IUserRepository`. `IEmailSender` separado de `IWhatsAppSender`. |
| **D — Dependency Inversion** | Application depende de `IReportRepository` (abstração), nunca de `EfReportRepository` (implementação). |

### 1.3 Clean Code — Regras do Projeto

| Regra | Enforcement |
|-------|-------------|
| Métodos com no máximo 20 linhas | Code review + analyzer |
| Classes com no máximo 200 linhas | Dividir quando exceder |
| Nomes descritivos (sem abreviações) | `GenerateWeeklyReportCommand`, não `GenRptCmd` |
| Zero comentários óbvios | Código deve se auto-documentar |
| Sem magic numbers/strings | Constantes nomeadas ou enums |
| Sem nested ifs (máx 2 níveis) | Early return, guard clauses |
| Sem `null` retornado | `Result<T>` pattern para erros esperados |
| Exceptions apenas para erros inesperados | Domain exceptions para regras de negócio |
| 100% de cobertura no Domain layer | Unit tests obrigatórios |
| >80% de cobertura no Application layer | Unit + Integration tests |

### 1.4 Convenções de Nomenclatura

```
Namespaces:     WeeklyUp.Domain.Entities
Classes:        PascalCase          → GenerateWeeklyReportCommandHandler
Interfaces:     I + PascalCase      → IDataSourceProvider
Métodos:        PascalCase + Async  → CollectMetricsAsync()
Propriedades:   PascalCase          → BusinessName
Campos privados: _camelCase         → _unitOfWork
Parâmetros:     camelCase           → cancellationToken (ou ct)
Constantes:     PascalCase          → MaxIntegrationsForFree
Tabelas DB:     snake_case          → report_preferences
Colunas DB:     snake_case          → week_start
Arquivos:       PascalCase          → GenerateWeeklyReportCommand.cs
Endpoints:      PascalCase          → ReportEndpoints.cs
```

---

## 2. Stack Tecnológica (100% Open Source)

### 2.1 Backend (C# 14 / .NET 10)

| Categoria | Tecnologia | Versão | Justificativa |
|-----------|-----------|--------|---------------|
| **Runtime** | .NET 10 LTS | 10.x | LTS até nov/2028, performance recorde, C# 14 |
| **Web Framework** | ASP.NET Core 10 — Minimal APIs | 10.x | Zero boilerplate, alta performance, validação nativa |
| **Endpoint Organization** | Carter | 8.x | Organiza Minimal APIs em módulos (como controllers, mas sem o overhead) |
| **ORM** | Entity Framework Core 10 | 10.x | JSON nativo, vector search, migrations, LINQ avançado |
| **DB Provider** | Npgsql.EntityFrameworkCore | 10.x | Provider PostgreSQL para EF Core |
| **CQRS/Mediator** | MediatR | 12.x | Commands/Queries desacoplados, pipeline behaviors |
| **Validation** | FluentValidation | 11.x | Validação declarativa e testável |
| **Mapping** | Mapster | 7.x | Mais rápido que AutoMapper, menos config |
| **Auth** | ASP.NET Identity + JWT Bearer | Nativo | Autenticação/autorização robusta |
| **Logging** | Serilog | 4.x | Structured logging, múltiplos sinks |
| **Health Checks** | AspNetCore.Diagnostics | Nativo | Monitoramento de saúde da API |
| **Rate Limiting** | AspNetCore.RateLimiting | Nativo | Proteção contra abuso |
| **API Docs** | Microsoft.AspNetCore.OpenApi | Nativo | OpenAPI 3.1 nativo no .NET 10, sem Swashbuckle |
| **Scalar** | Scalar.AspNetCore | Latest | UI bonita para explorar a API (substituto do Swagger UI) |
| **Background Jobs** | Hangfire + PostgreSQL storage | 1.8.x | Jobs recorrentes, dashboard, retry |
| **Cache** | StackExchange.Redis | 2.x | Cache distribuído, pub/sub |
| **Resilience** | Polly | 8.x | Retry, circuit breaker, timeout |
| **HTTP Client** | Refit | 7.x | Typed HTTP clients declarativos |
| **Testes** | xUnit + FluentAssertions + NSubstitute | Latest | Stack de testes moderna |
| **Arch Tests** | NetArchTest.Rules | 1.x | Enforcement de Clean Architecture |
| **Encryption** | Built-in (System.Security.Cryptography) | Nativo | AES-256 para tokens OAuth |

### 2.2 Banco de Dados

| Tecnologia | Uso | Justificativa |
|-----------|-----|---------------|
| **PostgreSQL 17** | Banco principal | Open source, JSONB nativo, performance |
| **Redis 7** | Cache, sessões, rate limit state | In-memory, alta performance, pub/sub |

### 2.3 Frontend Web (Next.js)

| Tecnologia | Versão | Uso |
|-----------|--------|-----|
| **Next.js** | 14.x (App Router) | Framework React com SSR/SSG |
| **React** | 18.x | UI library |
| **TypeScript** | 5.x | Type safety obrigatório |
| **Tailwind CSS** | 3.x | Utility-first CSS |
| **shadcn/ui** | Latest | Componentes acessíveis (Radix primitives) |
| **Recharts** | 2.x | Gráficos do dashboard |
| **TanStack Query** | 5.x | Server state management + cache |
| **Zustand** | 4.x | Client state management (leve) |
| **React Hook Form** | 7.x | Formulários performáticos |
| **Zod** | 3.x | Schema validation (compartilha com API types) |
| **next-auth** | 5.x | Autenticação (sincroniza com backend JWT) |

### 2.4 Mobile (.NET MAUI 10)

| Tecnologia | Uso |
|-----------|-----|
| **.NET MAUI 10** | Framework cross-platform (Android + iOS) — com XAML compilado e global namespaces |
| **CommunityToolkit.MAUI** | Componentes extras, behaviors |
| **CommunityToolkit.Mvvm** | MVVM com source generators |
| **Refit** | HTTP client tipado (mesma API) |
| **SkiaSharp / LiveChartsCore** | Gráficos nativos no dashboard |
| **Plugin.Firebase.CloudMessaging** | Push notifications |
| **SecureStorage** | Armazenamento seguro de JWT |

### 2.5 Infraestrutura / DevOps

| Tecnologia | Uso |
|-----------|-----|
| **Docker** + **Docker Compose** | Containerização + ambiente local |
| **GitHub Actions** | CI/CD pipelines |
| **Nginx** | Reverse proxy em produção |
| **Let's Encrypt** (Certbot) | SSL/TLS gratuito |
| **Resend** | Envio de emails transacionais |
| **Twilio / Z-API** | Envio de WhatsApp (plano Business) |
| **Sentry** | Error tracking + performance monitoring |
| **Seq** (Datalust) | Log aggregation com Serilog |

---

## 3. Por que Minimal APIs + Carter (e não Controllers)

### 3.1 Comparação

```csharp
// ❌ ANTES — Controller (verboso, cerimonial)
[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WeeklyReportResponse>), 200)]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWeeklyReportQuery(User.GetUserId(), id), ct);
        return result.Match<IActionResult>(
            data => Ok(ApiResponse<WeeklyReportResponse>.Ok(data)),
            error => NotFound(ErrorResponse.From(error)));
    }
}

// ✅ AGORA — Carter Module (enxuto, direto)
public sealed class ReportEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .RequireAuthorization()
            .WithTags("Reports");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetWeeklyReportQuery(ctx.User.GetUserId(), id), ct);
            return result.Match(Results.Ok, error => Results.NotFound(ErrorResponse.From(error)));
        }).WithName("GetReport");
    }
}
```

### 3.2 Vantagens

| Minimal APIs + Carter | Controllers |
|-----------------------|-------------|
| Menos boilerplate, código 40% menor | Herança de ControllerBase, atributos verbosos |
| Performance superior (sem overhead MVC) | Pipeline MVC completo mesmo sem usar |
| DI nativa nos parâmetros do handler | Constructor injection obrigatório |
| Agrupamento por feature com `MapGroup` | Route attributes espalhados |
| Ideal para CQRS (endpoint = 1 handler) | Classe gorda com N actions |
| OpenAPI 3.1 nativo no .NET 10 | Dependia de Swashbuckle |

---

## 4. Estrutura da Solution

```
WeeklyUp/
│
├── src/
│   ├── WeeklyUp.Domain/                        # 🔵 DOMAIN (zero dependências)
│   │   ├── Common/
│   │   │   ├── Entity.cs
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── ValueObject.cs
│   │   │   ├── IDomainEvent.cs
│   │   │   └── Result.cs
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Integration.cs
│   │   │   ├── Report.cs
│   │   │   ├── ReportPreference.cs
│   │   │   └── ManualMetric.cs
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs
│   │   │   ├── BusinessName.cs
│   │   │   ├── DateRange.cs
│   │   │   ├── Percentage.cs
│   │   │   ├── Money.cs
│   │   │   ├── Demographics.cs
│   │   │   └── ReportMetrics.cs
│   │   ├── Enums/
│   │   │   ├── PlanType.cs
│   │   │   ├── IntegrationProvider.cs
│   │   │   ├── IntegrationStatus.cs
│   │   │   ├── BusinessType.cs
│   │   │   └── DayOfWeekPreference.cs
│   │   ├── Events/
│   │   │   ├── ReportGeneratedEvent.cs
│   │   │   ├── IntegrationConnectedEvent.cs
│   │   │   └── UserUpgradedPlanEvent.cs
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs
│   │   │   ├── InvalidEmailException.cs
│   │   │   └── IntegrationLimitExceededException.cs
│   │   ├── Interfaces/
│   │   │   ├── Repositories/
│   │   │   │   ├── IUserRepository.cs
│   │   │   │   ├── IIntegrationRepository.cs
│   │   │   │   ├── IReportRepository.cs
│   │   │   │   └── IManualMetricRepository.cs
│   │   │   ├── Services/
│   │   │   │   ├── IDataSourceProvider.cs
│   │   │   │   ├── IInsightGenerator.cs
│   │   │   │   ├── IEmailSender.cs
│   │   │   │   ├── IWhatsAppSender.cs
│   │   │   │   └── ITokenEncryptor.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── Specifications/
│   │       ├── ISpecification.cs
│   │       └── ActiveUsersForReportSpec.cs
│   │
│   ├── WeeklyUp.Application/                   # 🟢 APPLICATION (depende só do Domain)
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   ├── IDateTimeProvider.cs
│   │   │   │   └── ICacheService.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── PerformanceBehavior.cs
│   │   │   ├── Mappings/
│   │   │   │   └── MappingConfig.cs
│   │   │   └── Exceptions/
│   │   │       ├── ValidationException.cs
│   │   │       ├── NotFoundException.cs
│   │   │       └── ForbiddenException.cs
│   │   ├── Users/
│   │   │   ├── Commands/  (RegisterUser, UpdateProfile, UpgradePlan)
│   │   │   ├── Queries/   (GetUserProfile, GetUserDashboard)
│   │   │   └── DTOs/
│   │   ├── Integrations/
│   │   │   ├── Commands/  (ConnectGA, ConnectStripe, Disconnect)
│   │   │   ├── Queries/   (GetUserIntegrations)
│   │   │   └── DTOs/
│   │   ├── Reports/
│   │   │   ├── Commands/  (GenerateWeeklyReport, SendEmail, SendWhatsApp, SubmitManual)
│   │   │   ├── Queries/   (GetWeeklyReport, GetHistory, GetDemographicsDetail)
│   │   │   ├── DTOs/
│   │   │   └── EventHandlers/
│   │   ├── DataCollection/
│   │   │   ├── Commands/  (CollectWeeklyData)
│   │   │   ├── Models/    (CollectedMetrics, DemographicData)
│   │   │   └── Interfaces/ (IDataAggregator)
│   │   └── DependencyInjection.cs
│   │
│   ├── WeeklyUp.Infrastructure/                # 🟠 INFRASTRUCTURE (implementações)
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── UnitOfWork.cs
│   │   │   ├── Configurations/  (Fluent API por entity)
│   │   │   ├── Repositories/    (implementações)
│   │   │   ├── Migrations/
│   │   │   └── Interceptors/    (Auditable, SoftDelete)
│   │   ├── DataSources/
│   │   │   ├── GoogleAnalytics/  (Provider, Client, Mapper)
│   │   │   ├── Stripe/           (Provider, ClientWrapper, Mapper)
│   │   │   └── Manual/           (Provider)
│   │   ├── AI/
│   │   │   ├── ClaudeInsightGenerator.cs
│   │   │   └── InsightPromptBuilder.cs
│   │   ├── Email/
│   │   │   ├── ResendEmailSender.cs
│   │   │   └── Templates/
│   │   ├── WhatsApp/
│   │   │   ├── TwilioWhatsAppSender.cs
│   │   │   └── WhatsAppMessageBuilder.cs
│   │   ├── Caching/  (RedisCacheService)
│   │   ├── Security/ (AesTokenEncryptor, JwtTokenGenerator, CurrentUserService)
│   │   ├── BackgroundJobs/ (WeeklyReportJob, TokenRefreshJob)
│   │   └── DependencyInjection.cs
│   │
│   ├── WeeklyUp.Api/                           # 🔴 PRESENTATION (Minimal APIs + Carter)
│   │   ├── Endpoints/                           # ← Carter modules (substituem Controllers)
│   │   │   ├── AuthEndpoints.cs
│   │   │   ├── UserEndpoints.cs
│   │   │   ├── IntegrationEndpoints.cs
│   │   │   ├── ReportEndpoints.cs
│   │   │   ├── DashboardEndpoints.cs
│   │   │   ├── ManualMetricEndpoints.cs
│   │   │   └── WebhookEndpoints.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   └── CorrelationIdMiddleware.cs
│   │   ├── Extensions/
│   │   │   ├── ResultExtensions.cs              # Result<T> → IResult
│   │   │   └── ClaimsPrincipalExtensions.cs     # GetUserId()
│   │   ├── Models/
│   │   │   ├── ApiResponse.cs
│   │   │   ├── ErrorResponse.cs
│   │   │   └── PaginatedResponse.cs
│   │   ├── Dockerfile
│   │   └── Program.cs
│   │
│   └── WeeklyUp.Shared/                        # ⚪ CROSS-CUTTING (constantes, extensions)
│       ├── Constants/ (CacheKeys, Roles, Limits)
│       └── Extensions/ (String, DateTime, Enumerable)
│
├── tests/
│   ├── WeeklyUp.Domain.Tests/                   # Unit tests — 100% coverage
│   ├── WeeklyUp.Application.Tests/              # Unit tests — >80% coverage
│   ├── WeeklyUp.Infrastructure.Tests/           # Integration tests
│   ├── WeeklyUp.Api.Tests/                      # API integration tests
│   └── WeeklyUp.Architecture.Tests/             # NetArchTest enforcement
│
├── frontend/                                     # Next.js 14 Web App
├── mobile/                                       # .NET MAUI 10 App
├── docker/
│   ├── docker-compose.yml
│   └── docker-compose.override.yml
│
├── docs/
│   └── architecture-decision-records/
│
├── WeeklyUp.sln
├── .editorconfig
├── .gitignore
├── Directory.Build.props
├── Directory.Packages.props
└── README.md
```

### 4.1 Organização CQRS por Feature

Cada feature segue a mesma estrutura vertical (Feature Slice):

```
Reports/
├── Commands/
│   └── GenerateWeeklyReport/
│       ├── GenerateWeeklyReportCommand.cs       # Record imutável
│       ├── GenerateWeeklyReportCommandHandler.cs # Sealed, uma responsabilidade
│       └── GenerateWeeklyReportCommandValidator.cs # FluentValidation
├── Queries/
│   └── GetWeeklyReport/
│       ├── GetWeeklyReportQuery.cs
│       ├── GetWeeklyReportQueryHandler.cs
│       └── WeeklyReportResponse.cs              # DTO de resposta
├── DTOs/
│   └── ReportDto.cs
└── EventHandlers/
    └── ReportGeneratedEventHandler.cs
```

---

## 5. Design Patterns Utilizados

| Pattern | Onde | Por quê |
|---------|------|---------|
| **CQRS** | Application Layer | Separa leitura de escrita. Queries podem usar cache. Commands passam por validação. |
| **Mediator** | MediatR pipeline | Desacopla endpoints dos handlers. Pipeline de cross-cutting concerns. |
| **Strategy** | `IDataSourceProvider` | Cada integração (GA4, Stripe) implementa a mesma interface. Adicionar novas não altera código existente. |
| **Repository** | Domain interfaces | Abstrai persistência. Domain não sabe que EF Core existe. |
| **Unit of Work** | `IUnitOfWork` | Transações atômicas. Um SaveChanges por operação. |
| **Result** | `Result<T>` | Erros esperados sem exceções. Match pattern para fluxo claro. |
| **Value Object** | Email, Money, DateRange, Demographics | Imutáveis, validados na criação, equality por valor. |
| **Domain Event** | `ReportGeneratedEvent` | Desacopla efeitos colaterais (enviar email quando relatório é gerado). |
| **Specification** | Queries complexas | Encapsula regras de filtro reutilizáveis. |
| **Builder** | `InsightPromptBuilder`, `WhatsAppMessageBuilder` | Construção fluente de objetos complexos. |
| **Factory Method** | `User.Create()`, `Report.Create()` | Criação controlada com validação. Construtores privados. |

---

## 6. Regras de Validação por Camada

```
Request chega no Endpoint (Minimal API)
        │
        ▼
[FluentValidation] ← Valida formato (email válido, campos obrigatórios)
        │                (via MediatR ValidationBehavior)
        ▼
[Command Handler]  ← Valida regras de negócio (usuário existe, plano permite)
        │
        ▼
[Domain Entity]    ← Valida invariantes (Result<T>, guard clauses)
        │
        ▼
[EF Core]          ← Valida constraints (unique, foreign key)
```

**Princípio:** Validação de formato no Application (via MediatR pipeline). Regras de negócio no Domain. Constraints no banco.

---

*Próximo documento: [02-Domain-Layer.md] — Entities, Value Objects, Interfaces e Events*
