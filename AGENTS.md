# AGENTS.md — TherapyHub

System for psychologists to manage patients, appointments, and clinical records.
Personal project. C# / .NET 11, ASP.NET Core Minimal APIs (REPR + endpoint groups), EF Core (SQL Server),
Clean Architecture + DDD, YARP gateway, Keycloak (OIDC).

This file holds the rules **every** agent needs. Role-specific rules live in the agent definitions:

- `.claude/agents/dotnet-backend-engineer.md` — production code in `src/`: architecture and dependency rule, DDD,
  Application, REPR/endpoint groups, auth, gateway, EF Core, code style.
- `.claude/agents/qa-engineer.md` — tests in `tests/`: what to test where, how to write tests, end-to-end checks.

Agents that don't load those files must read the relevant one before changing `src/` or `tests/`.

## Commands

First-time setup:

```bash
dotnet tool restore                            # installs dotnet-ef (local tool, see dotnet-tools.json)
cp .env.example .env                           # then set the passwords in .env (never commit it)
docker compose up -d                           # SQL Server on :1433, Keycloak on :8080
pwsh ./infra/keycloak/setup.ps1                # realm therapy-hub, clients, audience mapper, fake dev user
dotnet user-secrets set ConnectionStrings:TherapyHub "Server=localhost,1433;Database=TherapyHub;User Id=sa;Password=<from .env>;TrustServerCertificate=true" --project src/TherapyHub.Api
dotnet ef database update --project src/TherapyHub.Infrastructure --startup-project src/TherapyHub.Api
```

Getting a token locally (client `therapy-hub-dev`, user `dev.psychologist`, password `KEYCLOAK_DEV_USER_PASSWORD` in `.env`):
`POST http://localhost:8080/realms/therapy-hub/protocol/openid-connect/token` with
`grant_type=password&client_id=therapy-hub-dev&username=dev.psychologist&password=<...>`.
The password grant is for local development only.

Daily:

```bash
dotnet build                                   # build solution
dotnet test                                    # run all tests (integration tests need Docker running)
dotnet run --project src/TherapyHub.Api        # run API     → http://localhost:5239 (internal; behind the gateway)
dotnet run --project src/TherapyHub.Gateway    # run gateway → http://localhost:5134 (clients call this one)
dotnet format --verify-no-changes              # lint/format check (must pass)

# EF Core migrations (always specify both projects)
dotnet ef migrations add <Name> --project src/TherapyHub.Infrastructure --startup-project src/TherapyHub.Api --output-dir Persistence/Migrations
dotnet ef database update      --project src/TherapyHub.Infrastructure --startup-project src/TherapyHub.Api
```

NuGet versions live only in `Directory.Packages.props` (central package management). Shared build settings
(net11.0, nullable, warnings as errors) live in `Directory.Build.props`. Don't repeat them in `.csproj` files.
All text files use LF line endings (`.gitattributes`, `.editorconfig`).

Before finishing any task: build passes (0 warnings), tests pass, `dotnet format` is clean.

## Solution layout

```text
src/
  TherapyHub.Domain/          # Entities, aggregates, value objects, domain events, domain errors. No dependencies.
  TherapyHub.Application/     # Use cases (commands/queries + handlers), ports (interfaces), DTOs. Depends on Domain.
  TherapyHub.Infrastructure/  # EF Core DbContext, configurations, migrations, repositories, external services.
  TherapyHub.Api/             # Minimal API host, REPR endpoints in feature groups, DI composition root, auth.
  TherapyHub.Gateway/         # YARP reverse proxy: the only public entry point. JWT validation + rate limiting.
tests/
  TherapyHub.Domain.Tests/
  TherapyHub.Application.Tests/
  TherapyHub.Api.IntegrationTests/       # WebApplicationFactory + Testcontainers (SQL Server)
  TherapyHub.Gateway.IntegrationTests/   # WebApplicationFactory + stub backend (no API, no Docker)
infra/keycloak/                          # reproducible local Keycloak realm setup
```

Request flow: client → Gateway (authenticate, rate limit 30 req / 10 s per user, authorize) → Api (authenticate,
authorize, tenant scoping) → SQL Server.

## Shared conventions

- Naming: `PascalCase` types/members, `_camelCase` private fields, `Async` suffix on every `Task`/`ValueTask` method,
  tests `Method_State_ExpectedResult`.
- **No magic strings or numbers.** Any literal with meaning becomes a named `const` (or `static readonly`), defined once
  next to the type that owns it and reused everywhere else, tests included. Before adding a literal, search for an existing
  constant; if the same value appears a second time, extract it. Prefer framework constants (`JwtRegisteredClaimNames.Sub`,
  `StatusCodes`, `HttpStatusCode`, `nameof`).
  - Covers: configuration keys and section names, claim types, header names, policy names, error codes
    (as `Error` objects in `*Errors` classes), lengths, limits, timeouts.
  - Exceptions (inline is fine): endpoint definitions (group prefixes, tags, route templates, `WithName`, `Created`
    locations, `/health`); the CPF algorithm; `0`/`1` as loop bounds and empty checks; `string.Empty`; human-readable
    log/exception/ProblemDetails text; `[InlineData]` values that are the test case itself.
  - Where constants live and how tests use them: see the agent files.

## Sensitive data (patient health information)

This system stores clinical data. Health data is _sensitive personal data_ under LGPD (Lei 13.709/2018, art. 11),
and record keeping follows CFP rules (Resolution 001/2009) and the Psychologist's Code of Ethics (professional secrecy).

- Never log patient names, documents, contact info, or clinical notes. Log IDs only.
- Never put real or realistic patient data in seeds, tests, or examples. Use obviously fake data.
- Every access to a clinical record is audited (who, when, which record), with no clinical content in the log.
- Clinical note content is encrypted at the application level (ASP.NET Core Data Protection). Keys are never stored in
  the database or the repo (production: Azure Blob Storage + Key Vault; development: a folder outside the repo).
  Losing the keys means losing the clinical notes, so never rotate or delete keys by hand.
- A psychologist can only access their own patients; another psychologist's data is reported as 404.
- Patients can request their data (LGPD data subject rights). Keep export of a patient's data possible.
- No secrets in `appsettings*.json` or anywhere in the repo; use user-secrets / environment variables / `.env` (ignored).

## Boundaries

**Always:** follow your role's rules, keep build/tests/format green, report results faithfully (failures included).
**Ask first:** adding NuGet packages, changing aggregate boundaries, schema changes that drop/rename columns, changing auth,
changing rate-limit values or scope.
**Never:** commit secrets or `.env`, log PHI, use real patient data, disable nullable or analyzers to make code compile,
skip or weaken a failing test to get green, introduce magic strings/numbers or re-type a value that already has a constant.
