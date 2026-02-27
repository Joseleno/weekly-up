# 📊 WeeklyUp

> *Seu negócio na semana passada, em 2 minutos.*

**WeeklyUp** é um SaaS que envia relatórios semanais automáticos por email para donos de pequenos negócios. Sem dashboards complexos, sem login diário — apenas um email claro toda segunda-feira de manhã com os números que importam, segmentados por público.

---

## O Problema

Donos de pequenos negócios não têm tempo (nem paciência) para abrir Google Analytics, Instagram Insights ou Stripe Dashboard. Tomam decisões no escuro porque não conseguem interpretar dados espalhados em 5 ferramentas diferentes.

## A Solução

Um email semanal bonito e acionável que entrega:

- **Métricas-chave** com comparativo semanal (↑↓%)
- **Segmentação demográfica** — quem são os visitantes (gênero, idade, cidade)
- **Insights com IA** — frases claras explicando o que os números significam
- **Alertas** — o que precisa de atenção imediata
- **Dica da semana** — sugestão prática baseada nos dados

---

## Stack

### Backend
| Tecnologia | Versão |
|-----------|--------|
| .NET 10 LTS (C# 14) | 10.x |
| ASP.NET Core 10 — Minimal APIs + Carter | 10.x |
| Entity Framework Core 10 | 10.x |
| PostgreSQL | 17 |
| Redis | 7 |
| MediatR (CQRS) | 12.x |
| Hangfire (Background Jobs) | 1.8.x |

### Frontend (PWA)
| Tecnologia | Versão |
|-----------|--------|
| Blazor WebAssembly Standalone | .NET 10 |
| MudBlazor | 8.x |
| BlazorApexCharts | 3.x |
| Blazored.LocalStorage | 4.x |

### Infraestrutura
Docker · GitHub Actions · Nginx · Resend (email) · Twilio (WhatsApp) · Sentry · Seq

---

## Arquitetura

```
Clean Architecture · SOLID · Clean Code · CQRS · DDD Tactical Patterns
```

```
┌─────────────────────────────────────────────────┐
│  Presentation    (Minimal APIs + Carter)        │
├─────────────────────────────────────────────────┤
│  Application     (MediatR Handlers, DTOs)       │
├─────────────────────────────────────────────────┤
│  Domain          (Entities, Value Objects)       │  ← zero dependências
├─────────────────────────────────────────────────┤
│  Infrastructure  (EF Core, APIs, Email, Cache)  │
└─────────────────────────────────────────────────┘
```

## Estrutura do Repositório

```
weeklyup/
├── src/
│   ├── WeeklyUp.Domain/              # Entities, Value Objects, Interfaces
│   ├── WeeklyUp.Application/         # Commands, Queries, Handlers (CQRS)
│   ├── WeeklyUp.Infrastructure/      # EF Core, Providers, Email, AI, Jobs
│   ├── WeeklyUp.Api/                 # Minimal API Endpoints (Carter)
│   ├── WeeklyUp.Shared/              # Constants, Extensions
│   └── WeeklyUp.WebApp/             # Blazor WASM + MudBlazor (PWA)
├── tests/
│   ├── WeeklyUp.Domain.Tests/        # 100% coverage
│   ├── WeeklyUp.Application.Tests/   # >80% coverage
│   ├── WeeklyUp.Infrastructure.Tests/
│   ├── WeeklyUp.Api.Tests/
│   ├── WeeklyUp.Architecture.Tests/  # NetArchTest rules
│   └── WeeklyUp.WebApp.Tests/       # bUnit component tests
├── docker/                            # docker-compose.yml
└── docs/                              # Architecture Decision Records
```

## Integrações (MVP)

| Fonte | Dados |
|-------|-------|
| **Google Analytics 4** | Visitas, tráfego, gênero, idade, localidade, dispositivo |
| **Stripe** | Vendas, receita, ticket médio, novos clientes |
| **Entrada manual** | Qualquer métrica via formulário rápido |

## Modelo de Negócio

| | Free | Pro (R$ 29/mês) | Business (R$ 79/mês) |
|---|:---:|:---:|:---:|
| Relatório semanal | ✅ | ✅ | ✅ |
| Métricas básicas | ✅ | ✅ | ✅ |
| Segmentação demográfica | ❌ | ✅ | ✅ |
| Insights com IA | ❌ | ✅ | ✅ |
| WhatsApp | ❌ | ❌ | ✅ |
| PDF + Multi-negócio | ❌ | ❌ | ✅ |

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/)

## Rodando Local

```bash
# Subir infraestrutura
docker compose -f docker/docker-compose.yml up -d

# Backend
dotnet restore
dotnet run --project src/WeeklyUp.Api

# WebApp (Blazor WASM)
dotnet run --project src/WeeklyUp.WebApp
```

## Testes

```bash
# Todos os testes
dotnet test

# Apenas architecture tests
dotnet test tests/WeeklyUp.Architecture.Tests/

# Com coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## Documentação

| Doc | Conteúdo |
|-----|----------|
| `docs/01-Arquitetura-e-Padroes.md` | Clean Architecture, SOLID, stack, design patterns |
| `docs/02-Domain-Layer.md` | Entities, Value Objects, interfaces, events |
| `docs/03-Application-Layer.md` | CQRS handlers, behaviors, DTOs |
| `docs/04-Infrastructure-Layer.md` | EF Core, providers, email, WhatsApp, jobs |
| `docs/05-API-Tests-DevOps.md` | Endpoints, testes, Docker, CI/CD |
| `docs/06-Cronograma-Checklist.md` | Roadmap dia-a-dia, checklist de qualidade |
| `docs/07-Plano-de-Implementacao.md` | Plano detalhado Backend (Fases 0-6) + WebApp Blazor (Fases F0-F7) |

---

## Licença

Projeto privado. Todos os direitos reservados.
