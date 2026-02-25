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

### Frontend Web
| Tecnologia | Versão |
|-----------|--------|
| Next.js (App Router) | 14.x |
| React + TypeScript | 18.x / 5.x |
| Tailwind CSS + shadcn/ui | 3.x |

### Mobile
| Tecnologia | Versão |
|-----------|--------|
| .NET MAUI 10 | 10.x |

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
│   └── WeeklyUp.Shared/              # Constants, Extensions
├── tests/
│   ├── WeeklyUp.Domain.Tests/        # 100% coverage
│   ├── WeeklyUp.Application.Tests/   # >80% coverage
│   ├── WeeklyUp.Infrastructure.Tests/
│   ├── WeeklyUp.Api.Tests/
│   └── WeeklyUp.Architecture.Tests/  # NetArchTest rules
├── frontend/                          # Next.js 14
├── mobile/                            # .NET MAUI 10
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
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/)

## Rodando Local

```bash
# Subir infraestrutura
docker compose -f docker/docker-compose.yml up -d

# Backend
dotnet restore
dotnet run --project src/WeeklyUp.Api

# Frontend
cd frontend && npm install && npm run dev
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

---

## Licença

Projeto privado. Todos os direitos reservados.
