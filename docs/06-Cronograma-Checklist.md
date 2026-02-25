# 📊 WeeklyUp — 06. Cronograma e Checklist de Implementação

> **Prazo:** 5 semanas (Backend + Web) + 3 semanas (Mobile)  
> **Abordagem:** Backend-first → Frontend → Mobile

---

## Visão Geral das Fases

```
Semana 1 ████████ Fundação + Domain Layer
Semana 2 ████████ Integrações + Data Collection  
Semana 3 ████████ Relatórios + IA + Email + WhatsApp
Semana 4 ████████ API completa + Dashboard Web
Semana 5 ████████ Polish + Testes + Deploy
Semana 6-8 ██████ Mobile (.NET MAUI)
```

---

## Semana 1 — Fundação + Domain

### Dia 1: Setup

- [ ] Criar solution `WeeklyUp.sln` com 5 projetos (Domain, Application, Infrastructure, Api, Shared)
- [ ] Criar projetos de teste (Domain.Tests, Application.Tests, Infrastructure.Tests, Api.Tests, Architecture.Tests)
- [ ] Configurar `.editorconfig` (indent, namespace style, var preferences)
- [ ] Configurar `Directory.Build.props` (TreatWarningsAsErrors, Nullable enable, ImplicitUsings)
- [ ] Configurar `Directory.Packages.props` (Central Package Management)
- [ ] Instalar NuGet packages em cada projeto (MediatR, FluentValidation, Mapster, EF Core, etc.)
- [ ] Criar `docker-compose.yml` (PostgreSQL 17 + Redis 7 + Seq)
- [ ] Testar: `docker compose up` → banco acessível na porta 5432
- [ ] Criar `.gitignore` e inicializar repositório Git

### Dia 2: Domain Layer — Base + Value Objects

- [ ] Implementar `Entity.cs`, `AggregateRoot.cs`, `ValueObject.cs`, `IDomainEvent.cs`
- [ ] Implementar `Result<T>.cs`
- [ ] Implementar todos os Enums (PlanType, IntegrationProvider, IntegrationStatus, BusinessType, DayOfWeekPreference)
- [ ] Implementar Value Objects: `Email`, `Money`, `Percentage`, `DateRange`, `BusinessName`
- [ ] Implementar Value Objects complexos: `Demographics`, `ReportMetrics`, `ReportInsights`
- [ ] Implementar records auxiliares: `GenderDistribution`, `DeviceDistribution`, `LocationEntry`
- [ ] Implementar Domain Exceptions: `DomainException`, `InvalidEmailException`, etc.
- [ ] Testes: 100% dos Value Objects testados (Email válido/inválido, Money Add, Percentage.CalculateChange, DateRange.PreviousWeek)

### Dia 3: Domain Layer — Entities + Interfaces

- [ ] Implementar `User` entity (Create, AddIntegration, RemoveIntegration, UpgradePlan, CanAccess*)
- [ ] Implementar `Integration` entity (Create, UpdateTokens, Disconnect, MarkError)
- [ ] Implementar `Report` entity (Create, AddInsights, SetDemographics, Mark*Sent)
- [ ] Implementar `ReportPreference` entity
- [ ] Implementar `ManualMetric` entity
- [ ] Implementar Domain Events: `ReportGeneratedEvent`, `IntegrationConnectedEvent`, `UserUpgradedPlanEvent`
- [ ] Implementar Repository Interfaces: `IUserRepository`, `IReportRepository`, `IIntegrationRepository`, `IManualMetricRepository`
- [ ] Implementar Service Interfaces: `IDataSourceProvider`, `IInsightGenerator`, `IEmailSender`, `IWhatsAppSender`, `ITokenEncryptor`
- [ ] Implementar `IUnitOfWork`
- [ ] Testes: User.Create, User.AddIntegration (free limit), User.UpgradePlan, Report.Create
- [ ] Testes: Architecture tests (Domain não depende de nada)

### Dia 4: Application Layer — Common + Users

- [ ] Implementar `ICurrentUserService`, `IDateTimeProvider`, `ICacheService`, `IDataAggregator`
- [ ] Implementar `ValidationBehavior<,>`, `LoggingBehavior<,>`, `PerformanceBehavior<,>`
- [ ] Implementar `ValidationException`, `NotFoundException`, `ForbiddenException`
- [ ] Implementar `DependencyInjection.cs` (AddApplication)
- [ ] Implementar `RegisterUserCommand` + Handler + Validator
- [ ] Implementar `UpdateProfileCommand` + Handler + Validator
- [ ] Implementar `UpgradePlanCommand` + Handler + Validator
- [ ] Implementar `GetUserProfileQuery` + Handler + Response DTO
- [ ] Testes: RegisterUser (sucesso + email duplicado), ValidationBehavior

### Dia 5: Infrastructure — Persistence

- [ ] Implementar `ApplicationDbContext`
- [ ] Implementar `AuditableEntityInterceptor`
- [ ] Implementar Entity Configurations: UserConfiguration, IntegrationConfiguration, ReportConfiguration, ReportPreferenceConfiguration, ManualMetricConfiguration
- [ ] Gerar primeira migration: `dotnet ef migrations add InitialCreate`
- [ ] Testar migration: `dotnet ef database update` (contra Docker Postgres)
- [ ] Implementar Repositories: UserRepository, IntegrationRepository, ReportRepository, ManualMetricRepository
- [ ] Implementar `UnitOfWork`
- [ ] Implementar `AesTokenEncryptor`
- [ ] Implementar `DependencyInjection.cs` (AddInfrastructure)
- [ ] Testes: Repository integration tests (CRUD básico contra banco real)

---

## Semana 2 — Integrações + Data Collection

### Dia 1: Auth

- [ ] Implementar ASP.NET Identity (User entity mapeada)
- [ ] Implementar `JwtTokenGenerator` (gerar + validar JWT)
- [ ] Implementar `CurrentUserService` (extrair UserId do JWT)
- [ ] Implementar `AuthEndpoints` (Register, Login, GoogleCallback)
- [ ] Configurar Google OAuth (Client ID + Secret para GA4)
- [ ] Testar: Register → Login → Receber JWT → Acessar endpoint protegido

### Dia 2: Google Analytics Provider

- [ ] Implementar `IGoogleAnalyticsClient` (Refit interface)
- [ ] Implementar modelos GA4: `GA4ReportRequest`, `GA4ReportResponse`, `GA4DateRange`, `GA4Metric`, `GA4Dimension`
- [ ] Implementar `GA4Mapper` (converter response GA4 → `CollectedMetrics` + `DemographicData`)
- [ ] Implementar `GoogleAnalyticsProvider` (CollectMetrics, CollectDemographics, ValidateConnection)
- [ ] Implementar `ConnectGoogleAnalyticsCommand` + Handler (salvar tokens OAuth)
- [ ] Testes: GA4Mapper unit tests, Provider com mock HTTP

### Dia 3: Stripe Provider

- [ ] Implementar `StripeDataProvider` (CollectMetrics usando Stripe .NET SDK)
- [ ] Implementar `StripeMapper`
- [ ] Implementar `ConnectStripeCommand` + Handler
- [ ] Implementar `DisconnectIntegrationCommand` + Handler
- [ ] Implementar `GetUserIntegrationsQuery` + Handler
- [ ] Testes: StripeProvider com mock, DisconnectIntegration

### Dia 4: Data Collection + Aggregation

- [ ] Implementar `CollectedMetrics` model (dados brutos de cada provider)
- [ ] Implementar `DemographicData` model (dados demográficos brutos)
- [ ] Implementar `DataAggregator` (implementa IDataAggregator — mescla dados de múltiplos providers)
- [ ] Implementar `CollectWeeklyDataCommand` + Handler
- [ ] Implementar `ManualDataProvider` (implementa IDataSourceProvider para entrada manual)
- [ ] Implementar `SubmitManualMetricsCommand` + Handler + Validator
- [ ] Testes: DataAggregator (merging de múltiplas fontes), ManualMetrics

### Dia 5: Integrações + Endpoints

- [ ] Implementar `IntegrationEndpoints` (GET all, POST connect GA, POST connect Stripe, DELETE disconnect)
- [ ] Implementar `ManualMetricEndpoints` (POST submit)
- [ ] Implementar `TokenRefreshJob` (Hangfire — refresh OAuth tokens que vão expirar)
- [ ] Configurar Polly retry policies para chamadas externas
- [ ] Testes: Endpoint integration tests (via WebApplicationFactory)

---

## Semana 3 — Relatórios + IA + Email + WhatsApp

### Dia 1: Geração de Relatório

- [ ] Implementar `GenerateWeeklyReportCommand` + Handler (o handler principal — ver doc 03)
- [ ] Implementar `GenerateWeeklyReportCommandValidator`
- [ ] Garantir que Handler: coleta → agrega → cria Report → salva
- [ ] Testes: Handler com mocks (user não encontrado, relatório duplicado, fluxo completo)

### Dia 2: Insights com IA

- [ ] Implementar `InsightPromptBuilder` (construir prompt com métricas + demographics)
- [ ] Implementar `ClaudeInsightGenerator` (chamar API Anthropic, parsear resposta)
- [ ] Implementar parsing de resposta (DESTAQUE / ALERTA / DICA)
- [ ] Implementar fallback se IA falhar (insights genéricos baseados nos dados)
- [ ] Testes: PromptBuilder output, InsightGenerator com mock HTTP

### Dia 3: Email (Resend)

- [ ] Implementar `ResendEmailSender` (implementa IEmailSender)
- [ ] Implementar `WeeklyReportEmailTemplate` (HTML responsivo e bonito)
  - [ ] Header com logo + período
  - [ ] Bloco de métricas (receita ↑↓%, vendas, tráfego)
  - [ ] Bloco de demographics (gênero, idade, cidade) — visual com barras coloridas
  - [ ] Bloco de insights (destaque, alerta, dica)
  - [ ] Footer (links para dashboard + preferências)
- [ ] Implementar `WelcomeEmailTemplate`
- [ ] Implementar `SendReportEmailCommand` + Handler
- [ ] Configurar SPF/DKIM/DMARC no domínio
- [ ] Testes: enviar email de teste real

### Dia 4: WhatsApp (Twilio)

- [ ] Implementar `WhatsAppMessageBuilder` (formatar mensagem com emojis + markdown do WhatsApp)
- [ ] Implementar `TwilioWhatsAppSender` (implementa IWhatsAppSender)
- [ ] Implementar `SendReportWhatsAppCommand` + Handler (verifica plano Business)
- [ ] Configurar Twilio sandbox para testes
- [ ] Testes: enviar mensagem de teste real

### Dia 5: Background Job + Event Handlers

- [ ] Implementar `WeeklyReportJob` (Hangfire — gerar + enviar para todos os usuários)
- [ ] Registrar recurring job (toda segunda 7h horário de Brasília)
- [ ] Implementar `ReportGeneratedEventHandler` (invalida cache do dashboard)
- [ ] Implementar `ReportEndpoints` (GET report, GET history, GET demographics)
- [ ] Testes: Job execution com mocks, event handler

---

## Semana 4 — API Completa + Dashboard Web

### Dia 1: API — Dashboard + Finalização

- [ ] Implementar `GetUserDashboardQuery` + Handler (com cache Redis)
- [ ] Implementar `DashboardEndpoints`
- [ ] Implementar Health check endpoint
- [ ] Implementar `WebhookEndpoints` (Stripe webhooks para pagamento)
- [ ] Implementar rate limiting (60 req/min por IP)
- [ ] Implementar request logging middleware
- [ ] Implementar correlation ID middleware
- [ ] OpenAPI 3.1 nativo + Scalar UI para documentação da API
- [ ] Testes: todos os endpoints via WebApplicationFactory

### Dia 2: Frontend — Setup + Landing Page

- [ ] Setup Next.js 14 (App Router) + TypeScript + Tailwind + shadcn/ui
- [ ] Configurar API client (Axios ou fetch wrapper com JWT)
- [ ] Implementar layout base (Header, Footer, MobileNav)
- [ ] Landing page (hero, features, pricing, CTA)
  - [ ] Seção "Como funciona" (3 passos)
  - [ ] Seção "Exemplo do email" (mockup visual)
  - [ ] Seção "Planos" (Free, Pro R$29, Business R$79)
  - [ ] CTA "Comece grátis"
- [ ] Deploy landing page (Vercel)

### Dia 3: Frontend — Auth + Onboarding

- [ ] Login page (email/senha + Google OAuth)
- [ ] Register page
- [ ] Fluxo de onboarding:
  - [ ] Step 1: "Como se chama seu negócio?"
  - [ ] Step 2: "Qual o tipo?" (selecionar BusinessType)
  - [ ] Step 3: "Conecte suas fontes" (botões OAuth GA4 + Stripe + manual)
  - [ ] Step 4: "Pronto! Relatório chega segunda às 7h"
- [ ] Proteção de rotas (redirect se não logado)

### Dia 4: Frontend — Dashboard

- [ ] Dashboard layout (sidebar + content area)
- [ ] `MetricCard` component (receita, vendas, tráfego com ↑↓%)
- [ ] `RevenueChart` (Recharts — linha de tendência últimas 12 semanas)
- [ ] `TrafficChart` (Recharts — barras)
- [ ] `DemographicsPanel`:
  - [ ] `GenderPieChart` (pizza feminino/masculino)
  - [ ] `AgeBarChart` (barras horizontais por faixa etária)
  - [ ] `LocationList` (top 5 cidades)
  - [ ] `DeviceSplit` (mobile vs desktop)
  - [ ] `TrafficSourceChart` (donut chart com fontes)
- [ ] `InsightCard` (destaque, alerta, dica — com ícones e cores)
- [ ] `WeekComparison` (side-by-side esta semana vs anterior)
- [ ] Lock overlay para features Pro (blur + "Faça upgrade" CTA)

### Dia 5: Frontend — Reports + Settings

- [ ] Reports History page (timeline de relatórios passados)
- [ ] Report Detail page (relatório completo de uma semana)
- [ ] Demographics Detail page (detalhamento completo)
- [ ] Integrations page (conectar/desconectar, status)
- [ ] Settings page (preferências do relatório, dia/hora de envio)
- [ ] Billing page (planos, upgrade, integração Stripe Checkout)
- [ ] Responsividade mobile (testar todas as telas)

---

## Semana 5 — Polish + Testes + Deploy

### Dia 1-2: Testes + Cobertura

- [ ] Domain Tests: verificar 100% coverage
- [ ] Application Tests: verificar >80% coverage
- [ ] Infrastructure Tests: testar providers contra mocks
- [ ] API Tests: testar todos os endpoints (happy path + erros)
- [ ] Architecture Tests: todas as regras passando
- [ ] Frontend: testar fluxos críticos manualmente

### Dia 3: Segurança + Performance

- [ ] Revisar todas as rotas (autenticação obrigatória)
- [ ] Revisar rate limiting
- [ ] Revisar encryption de tokens OAuth
- [ ] Verificar que dados demográficos não vazam para plano Free
- [ ] Load test básico (verificar que 100 relatórios geram em <5 min)
- [ ] Otimizar queries com índices (verificar plano de execução)
- [ ] Configurar Redis cache TTLs adequados

### Dia 4: Deploy

- [ ] Deploy API: Azure App Service ou Railway (Docker)
- [ ] Deploy PostgreSQL: Supabase ou Neon (managed)
- [ ] Deploy Redis: Upstash (serverless Redis)
- [ ] Deploy Frontend: Vercel
- [ ] Configurar domínio + SSL (Let's Encrypt)
- [ ] Configurar Sentry (error tracking)
- [ ] Configurar Seq ou equivalente (logs)
- [ ] Testar Hangfire job em produção (trigger manual)
- [ ] Testar envio de email real
- [ ] Testar envio de WhatsApp real (sandbox)

### Dia 5: Beta

- [ ] Criar 3-5 contas de teste com dados reais
- [ ] Trigger manual do job de relatório
- [ ] Verificar email recebido (visual, dados corretos, links funcionam)
- [ ] Verificar dashboard (métricas, gráficos, demographics)
- [ ] Verificar WhatsApp (mensagem recebida, formatação)
- [ ] Fix bugs encontrados
- [ ] Convidar 5-10 beta users reais

---

## Semanas 6-8 — Mobile (.NET MAUI)

### Semana 6: Setup + Telas Base

- [ ] Setup projeto .NET MAUI (Android + iOS)
- [ ] Design system (cores, fontes, espaçamentos)
- [ ] Implementar `ApiService` (Refit — mesma API do backend)
- [ ] Implementar `AuthService` (JWT storage via SecureStorage)
- [ ] Telas: LoginPage, RegisterPage
- [ ] Shell navigation (tabs: Dashboard, Reports, Settings)

### Semana 7: Dashboard + Reports

- [ ] DashboardPage (métricas, mini-charts com LiveChartsCore)
- [ ] DemographicsPage (gráficos de gênero, idade, localidade)
- [ ] ReportDetailPage (relatório completo)
- [ ] ReportsListPage (histórico)
- [ ] Pull-to-refresh em todas as telas
- [ ] Loading states + empty states

### Semana 8: Polish + Publish

- [ ] IntegrationsPage (status das integrações)
- [ ] SettingsPage (preferências, plano, conta)
- [ ] Push notifications via Firebase (quando relatório é gerado)
- [ ] Testar em Android (emulador + device real)
- [ ] Testar em iOS (simulador)
- [ ] Preparar assets (ícone, splash, screenshots)
- [ ] Publicar na Google Play Store
- [ ] Publicar na Apple App Store

---

## Checklist de Qualidade (antes de cada deploy)

### Code Quality
- [ ] Zero warnings no build
- [ ] Todos os testes passando
- [ ] Architecture tests passando
- [ ] Domain coverage >= 100%
- [ ] Application coverage >= 80%
- [ ] Sem TODOs ou FIXMEs pendentes
- [ ] Code review feito (se em equipe)

### Security
- [ ] Tokens OAuth criptografados (AES-256)
- [ ] JWT com expiração adequada (1h access, 7d refresh)
- [ ] Rate limiting ativo
- [ ] CORS configurado corretamente
- [ ] SPF/DKIM/DMARC configurados
- [ ] Sem secrets no código (usar environment variables)
- [ ] HTTPS obrigatório

### Performance
- [ ] Queries com índices (verificar EXPLAIN)
- [ ] Cache Redis em endpoints de leitura
- [ ] Sem N+1 queries
- [ ] Requests externos com timeout + retry (Polly)
- [ ] Relatório gera em < 5s por usuário
- [ ] Dashboard carrega em < 2s

### Monitoring
- [ ] Serilog configurado com Seq
- [ ] Sentry capturando exceptions
- [ ] Health checks respondendo
- [ ] Hangfire dashboard acessível
- [ ] Alertas configurados (job falhou, error rate alto)

---

## API Endpoints Summary

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| POST | `/api/auth/register` | Cadastro | ❌ |
| POST | `/api/auth/login` | Login (retorna JWT) | ❌ |
| POST | `/api/auth/google` | Login com Google OAuth | ❌ |
| POST | `/api/auth/refresh` | Refresh token | ❌ |
| GET | `/api/users/profile` | Perfil do usuário | ✅ |
| PUT | `/api/users/profile` | Atualizar perfil | ✅ |
| GET | `/api/dashboard` | Dashboard completo | ✅ |
| GET | `/api/reports/{id}` | Relatório específico | ✅ |
| GET | `/api/reports/history` | Histórico de relatórios | ✅ |
| GET | `/api/reports/{id}/demographics` | Demographics detalhado | ✅ Pro+ |
| POST | `/api/reports/manual` | Submeter métricas manuais | ✅ |
| GET | `/api/integrations` | Listar integrações | ✅ |
| POST | `/api/integrations/google-analytics` | Conectar GA4 | ✅ |
| POST | `/api/integrations/stripe` | Conectar Stripe | ✅ |
| DELETE | `/api/integrations/{provider}` | Desconectar | ✅ |
| POST | `/api/webhooks/stripe` | Stripe webhook | API Key |
| GET | `/health` | Health check | ❌ |

---

*Fim do plano de implementação — WeeklyUp*
