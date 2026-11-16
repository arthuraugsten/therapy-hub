# AGENTS.md — TherapyHub

System for psychologists to manage patients, appointments, and clinical records.
Personal project. C# / .NET 11, ASP.NET Core Minimal APIs (REPR + endpoint groups), EF Core (SQL Server),
modular monolith (Clean Architecture + DDD, modules separated by namespace), YARP gateway, Keycloak (OIDC).

This file holds the rules **every** agent needs. Role-specific rules live in the agent definitions:

- `.claude/agents/dotnet-backend-engineer.md` — production code in `src/`: architecture and dependency rule, DDD,
  Application, REPR/endpoint groups, auth, gateway, EF Core, code style.
- `.claude/agents/qa-engineer.md` — tests in `tests/`: what to test where, how to write tests, end-to-end checks.

Agents that don't load those files must read the relevant one before changing `src/` or `tests/`.

## Commands

First-time setup:

```bash
git config core.hooksPath .githooks            # enables hooks that enforce signed commits (README.md, Signed commits)
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

# EF Core migrations: one DbContext per module, so always pass both projects and --context
dotnet ef migrations add <Name> --project src/TherapyHub.Infrastructure --startup-project src/TherapyHub.Api --context PatientsDbContext --output-dir Patients/Persistence/Migrations
dotnet ef database update      --project src/TherapyHub.Infrastructure --startup-project src/TherapyHub.Api --context PatientsDbContext
```

If Windows Application Control blocks `dotnet ef` from loading the Debug build, add `--configuration Release`.

NuGet versions live only in `Directory.Packages.props` (central package management). Shared build settings
(net11.0, nullable, warnings as errors) live in `Directory.Build.props`. Don't repeat them in `.csproj` files.
All text files use LF line endings (`.gitattributes`, `.editorconfig`).

Before finishing any task: build passes (0 warnings), tests pass, `dotnet format` is clean.

## Solution layout

```text
src/
  BuildingBlocks/                              # shared code only, never module code
    TherapyHub.BuildingBlocks.Domain/          # Result, Error, Entity, AggregateRoot, IDomainEvent, Email, Cpf, PsychologistId. No dependencies.
    TherapyHub.BuildingBlocks.Application/     # ICommand/IQuery + handlers, IDomainEventHandler, ICurrentUser, IUnitOfWork, handler scanning.
    TherapyHub.BuildingBlocks.Infrastructure/  # ModuleDbContext base, domain event dispatcher, encrypted converter, Data Protection keys.
    TherapyHub.BuildingBlocks.Presentation/    # IEndpoint/IEndpointGroup, ValidationFilter, ErrorExtensions, CurrentUser.
  TherapyHub.Domain/          # Module code only: <Module>/ aggregates, IDs, errors, events.
  TherapyHub.Application/     # Module code only: <Module>/ use cases, ports, DTOs; <Module>/PublicApi/ cross-module contracts.
  TherapyHub.Infrastructure/  # Module code only: <Module>/ repositories, queries, <Module>Module DI; <Module>/Persistence/ DbContext, configs, migrations.
  TherapyHub.Api/             # Minimal API host (Program.cs, composition root, auth settings) + Features/<Module>/ REPR endpoints.
  TherapyHub.Gateway/         # YARP reverse proxy: the only public entry point. JWT validation + rate limiting.
tests/
  TherapyHub.BuildingBlocks.Domain.Tests/  # shared value objects
  TherapyHub.Domain.Tests/
  TherapyHub.Application.Tests/
  TherapyHub.ArchitectureTests/            # NetArchTest: module boundaries and layer rules
  TherapyHub.Api.IntegrationTests/         # WebApplicationFactory + Testcontainers (SQL Server)
  TherapyHub.Gateway.IntegrationTests/     # WebApplicationFactory + stub backend (no API, no Docker)
infra/keycloak/                            # reproducible local Keycloak realm setup
```

Request flow: client → Gateway (authenticate, rate limit 30 req / 10 s per user, authorize) → Api (authenticate,
authorize, tenant scoping) → SQL Server.

## Modules

The API is a modular monolith: one deployable host, one binary per layer, and one **module** per bounded context.
A module is a namespace slice through every layer project: `TherapyHub.Domain.<Module>`,
`TherapyHub.Application.<Module>`, `TherapyHub.Infrastructure.<Module>` and `TherapyHub.Api.Features.<Module>`.

| Module | Status | Schema | Owns |
| --- | --- | --- | --- |
| Patients | built | `patients` | `Patient` (demographics, contact) |
| Psychologists | planned | `psychologists` | `Psychologist` (CRP, settings) |
| Scheduling | planned | `scheduling` | `Appointment` (time, status; never clinical content) |
| ClinicalRecords | planned | `clinical` | `ClinicalRecord` + `SessionNote` (encrypted, audited) |

Rules (`TherapyHub.ArchitectureTests` enforces them; these tests are the only guard, since modules share assemblies):

1. **BuildingBlocks hold shared code only.** They never reference the layer projects. Layer projects hold only module
   namespaces, plus the composition roots (`Application/DependencyInjection.cs`, `Infrastructure/DependencyInjection.cs`,
   `Api/Program.cs`, `Api/Authentication/AuthenticationSettings.cs`).
2. **Cross-module calls go only through `TherapyHub.Application.<Module>.PublicApi`.** Module X may use BuildingBlocks,
   its own namespaces and other modules' `PublicApi` namespaces. It may not use another module's domain types,
   handlers, repositories, DbContext, endpoints or domain events.
3. A `PublicApi` namespace holds only interfaces and `sealed record` contracts with primitive types (no entities,
   value objects or strongly typed IDs). The implementation is `internal` and lives outside the namespace (e.g.
   `Application/Patients/PatientsApi.cs`). Contracts carry the minimum data the consumer needs (LGPD data
   minimisation). Add a method only when a consumer needs it.
4. PublicApi implementations are tenant-scoped through `ICurrentUser`, like handlers. Another psychologist's data is
   reported as not found.
5. Each module has its own `ModuleDbContext` subclass, SQL schema and `__EFMigrationsHistory` table in that schema.
   Each context applies only its own configurations. No cross-schema joins or FKs, no use of another module's DbContext.
   Other modules' aggregates are referenced by ID value only.
6. There are no cross-module transactions. Domain events stay inside their module. Asynchronous reactions across
   modules (e.g. patient archived → cancel appointments) need integration events with an outbox. Build those before
   the first such case instead of calling another module's handler.
7. Extracting a module later means moving its namespaces into `TherapyHub.Modules.<Module>` projects and its
   `PublicApi` into a `.PublicApi` project. Keep rules 2–5 so that move stays mechanical.

## Shared conventions

- Naming: `PascalCase` types/members, `_camelCase` private fields, `Async` suffix on every `Task`/`ValueTask` method,
  tests `Method_State_ExpectedResult`.

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
**Ask first:** adding NuGet packages, adding a module or changing module/aggregate boundaries, schema changes that drop/rename columns, changing auth,
changing rate-limit values or scope.
**Never:** commit secrets or `.env`, log PHI, use real patient data, disable nullable or analyzers to make code compile,
skip or weaken a failing test to get green.
