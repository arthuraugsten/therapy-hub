# AGENTS.md — TherapyHub

System for psychologists to manage patients, appointments, and clinical records.
Personal project. C# / .NET 11, ASP.NET Core Minimal APIs, EF Core, Clean Architecture + DDD.

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

Before finishing any task: build passes, tests pass, `dotnet format` is clean.

## Solution layout

```bash
src/
  TherapyHub.Domain/          # Entities, aggregates, value objects, domain events, domain errors. No dependencies.
  TherapyHub.Application/     # Use cases (commands/queries + handlers), ports (interfaces), DTOs. Depends on Domain.
  TherapyHub.Infrastructure/  # EF Core DbContext, configurations, migrations, repositories, external services.
  TherapyHub.Api/             # Minimal API host, REPR endpoints, DI composition root, auth, middleware.
  TherapyHub.Gateway/         # YARP reverse proxy: the only public entry point. JWT validation + rate limiting.
tests/
  TherapyHub.Domain.Tests/
  TherapyHub.Application.Tests/
  TherapyHub.Api.IntegrationTests/       # WebApplicationFactory + Testcontainers (SQL Server)
  TherapyHub.Gateway.IntegrationTests/   # WebApplicationFactory + stub backend (no API, no Docker)
```

Request flow: client → Gateway (authenticate, rate limit, authorize) → Api (authenticate, authorize, tenant scoping) → SQL Server.

## Dependency rule (never violate)

- Domain → nothing (no EF Core, no ASP.NET, no NuGet beyond BCL).
- Application → Domain only. Defines interfaces (`IPatientRepository`, `IUnitOfWork`, `ICurrentUser`).
- Infrastructure → Application + Domain. Implements those interfaces.
- Api → Application (+ Infrastructure only for DI registration in `Program.cs`).
- Endpoints never use `DbContext` directly.
- Gateway → nothing in the solution. It knows routes, not features; never reference Domain/Application from it.

## Domain (DDD) rules

- Aggregates (four, kept separate):
  - `Psychologist`: the professional (name, CRP registration, settings). Linked to the external identity.
  - `Patient`: demographics and contact info, owned by one `PsychologistId`.
  - `Appointment`: scheduling only (time, duration, status, `PatientId`). **Never holds clinical content.**
  - `ClinicalRecord`: one per patient; contains session notes (`SessionNote` entities, optionally referencing an `AppointmentId`).
    It is the only aggregate holding clinical data, so it is the only one that needs encryption and access auditing.
- Reference other aggregates **by ID only**, never by navigation property.
- Strongly-typed IDs: `readonly record struct PatientId(Guid Value)`.
- Entities: private setters, no public parameterless constructor (private one for EF only),
  creation via static factory `Patient.Create(...)` returning `Result<Patient>`.
- All invariants enforced inside the aggregate. State changes via intention-revealing methods
  (`appointment.Reschedule(...)`, `appointment.Cancel(reason)`), never property assignment from outside.
- Value objects as `record` / `sealed record` with validation in the factory (`Email`, `Cpf`, `PhoneNumber`, `SessionDuration`).
- Domain events raised from aggregates (`AppointmentScheduled`), dispatched by Infrastructure after `SaveChanges`.
- Expected failures use `Result` / `Result<T>` with typed `Error`s (in `Domain/Errors`). Exceptions only for programmer errors.
- Use the injected `TimeProvider`; never `DateTime.Now` / `DateTime.UtcNow`. Store UTC (`DateTimeOffset`).

## Application rules

- One use case = one folder: `Patients/RegisterPatient/{RegisterPatientCommand, RegisterPatientHandler}`.
- Handlers depend on interfaces only, return `Result<T>`.
- No mediator library. Use our own abstractions in `Application/Abstractions/Messaging`:
  `ICommand<TResult>`, `ICommandHandler<TCommand, TResult>`, `IQuery<TResult>`, `IQueryHandler<TQuery, TResult>`.
  Handlers are registered automatically through assembly scanning. Cross-cutting concerns (logging, transactions)
  are decorators around these interfaces, not code inside handlers.
- Validation of input shape: FluentValidation, one validator per Request in the feature folder, run by an endpoint filter.
  Business rules belong in Domain.
- Queries may bypass repositories and use a read-only query service returning DTOs (`AsNoTracking`, projections).

## API — REPR pattern (Request-Endpoint-Response)

**Reference implementation:** `Features/Patients/RegisterPatient` (command) and `Features/Patients/GetPatientById` (query),
with their handlers in `Application/Patients/` and tests in all three test projects. Copy their shape for new features.

One feature = one folder with one endpoint group; one endpoint per class:

```text
Api/Features/Patients/
  PatientsEndpointGroup.cs       // implements IEndpointGroup: prefix, tags, authorization
  RegisterPatient/
    RegisterPatientRequest.cs    // record, API contract only
    RegisterPatientResponse.cs   // record, API contract only
    RegisterPatientEndpoint.cs   // implements IEndpoint<PatientsEndpointGroup>
    RegisterPatientValidator.cs
```

```csharp
internal sealed class PatientsEndpointGroup : IEndpointGroup
{
    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app) =>
        app.MapGroup("/patients")
            .WithTags("Patients")
            .RequireAuthorization();
}

internal sealed class RegisterPatientEndpoint : IEndpoint<PatientsEndpointGroup>
{
    public void MapEndpoint(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)                          // relative to the group prefix
            .WithName("RegisterPatient")
            .WithRequestValidation<RegisterPatientRequest>();    // FluentValidation filter; documents the 400 response

    private static async Task<Results<Created<RegisterPatientResponse>, ProblemHttpResult>> HandleAsync(
        RegisterPatientRequest request,
        ICommandHandler<RegisterPatientCommand, PatientDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/patients/{result.Value.Id}", RegisterPatientResponse.From(result.Value))
            : result.Error.ToProblem();
    }
}
```

- Endpoints are thin: map Request → Command, call handler, map Result → Response. No business logic.
- Always `TypedResults` and explicit `Results<...>` return types (keeps OpenAPI accurate).
- Errors returned as RFC 7807 ProblemDetails.
- Endpoint groups: every endpoint implements `IEndpoint<TGroup>` and maps a route **relative** to its group
  (`"/"`, `"/{id:guid}"`). The group (`<Feature>EndpointGroup`, one per feature folder) owns what all its endpoints share:
  the prefix, `WithTags`, `RequireAuthorization()`, and group-wide filters. Don't repeat those on individual endpoints.
  An endpoint only adds what is specific to it (`WithName`, `WithRequestValidation<T>`, `AllowAnonymous()` if ever needed).
- Groups and endpoints are discovered by `AddEndpoints(assembly)` and mapped by `app.MapEndpoints()`. An endpoint that
  implements plain `IEndpoint` without a group fails at startup. Never map routes in `Program.cs` (only `/health`).
- Never expose domain entities in Requests/Responses.
- Always accept and pass `CancellationToken`.

## Authentication & authorization

- Authentication is handled by Keycloak (OIDC). Local development runs Keycloak in Docker (realm `therapy-hub`).
  The API only validates JWT bearer tokens (`AddAuthentication().AddJwtBearer(...)`); authority/audience come from configuration.
  Use only standard OIDC/JWT APIs, with no Keycloak-specific SDKs in application code.
  Keycloak's default `aud` is `account`, so the client needs an audience mapper that adds `therapy-hub-api` to tokens
  (`infra/keycloak/setup.sh` creates it). Realm changes go into that script so a fresh setup stays reproducible.
- The API never stores passwords or issues tokens itself.
- `ICurrentUser` (Application port, implemented in Api) exposes the current `PsychologistId`, resolved from the token's `sub` claim.
- Every endpoint group calls `RequireAuthorization()`; endpoints inherit it. Only health checks are public.
- Tenant isolation: every query and command is scoped to `ICurrentUser.PsychologistId`. A psychologist can never read or
  change another psychologist's patients, appointments, or records. Return 404 (not 403) for resources that belong to someone else.
- Integration tests use a test authentication handler that issues fake claims; never call the real provider.

## Gateway (YARP reverse proxy)

- Routes and clusters live in `src/TherapyHub.Gateway/appsettings*.json` under `ReverseProxy`. A single catch-all route
  forwards everything to the API, so new API endpoints need no gateway change. Add a route only for a new backend.
- No business logic, no database, no domain types in the gateway.
- Rate limiting happens **only at the gateway** (policy `per-user`, `RateLimiting/UserRateLimiting.cs`):
  fixed window, **30 requests per 10 seconds**, queue 0, values in `RateLimiting:PerUser`.
  - Scope is the current user: the `sub` claim of the validated token. Anonymous requests are scoped by client IP.
  - Rejections return 429 ProblemDetails with a `Retry-After` header. `/health` is not rate limited.
  - Don't add rate limiting to the API, and don't scope limits by anything a client can spoof (headers, query strings).
- Middleware order is deliberate: `UseAuthentication` → `UseRateLimiter` → `UseAuthorization`. Don't reorder.
- The gateway validates the same Keycloak JWT (same authority/audience) and requires authentication on the API route.
  The API still enforces its own auth and tenant scoping (defense in depth). Never trust the gateway alone.
- Production: only the gateway is publicly reachable. Set the API address with
  `ReverseProxy__Clusters__api__Destinations__api__Address`. With more than one gateway instance the in-memory limiter
  counts per instance; switch to a distributed limiter before scaling out.

## Persistence (EF Core)

- Database: SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`). Connection string from user-secrets/env, never committed.
- One `IEntityTypeConfiguration<T>` per aggregate in `Infrastructure/Persistence/Configurations`. No data annotations in Domain.
- Value objects mapped with `ComplexProperty` / owned types; strongly-typed IDs with value converters.
- Repositories exist only for aggregate roots. `SaveChangesAsync` via `IUnitOfWork`, called by the handler.
- Soft delete for patients and clinical records (global query filter). Clinical records must never be hard-deleted
  before the CFP minimum retention period (5 years, CFP Resolution 001/2009). Any purge job must check this.
- Migrations: never edit a migration that has already been applied; create a new one.

## SOLID / code style

- `sealed` by default; `internal` unless needed across projects.
- File-scoped namespaces, nullable enabled, warnings as errors.
- Constructor injection only; no service locator; no static mutable state.
- Prefer small interfaces specific to each consumer over a generic `IRepository<T>`.
- Async all the way. Every method returning `Task`/`ValueTask` ends in `Async` (`HandleAsync`, `GetByIdAsync`).
  Never block on async code (`.Result`, `.Wait()`).
- Naming: `PascalCase` types/members, `_camelCase` private fields, tests `Method_State_ExpectedResult`.
- **No magic strings or numbers.** Any literal with meaning becomes a named `const` (or `static readonly`), defined
  once next to the type that owns it and reused everywhere else, tests included. Before adding a literal, search for an
  existing constant; if the same value appears a second time, extract it.
  - Covers: configuration keys and section names, claim types, header names, policy names, error codes
    (as `Error` objects in `*Errors` classes), lengths, limits, timeouts.
  - Prefer framework constants over your own: `JwtRegisteredClaimNames.Sub`, `StatusCodes.Status404NotFound`,
    `HttpStatusCode`, `nameof(...)`.
  - Compose related constants instead of repeating fragments: `KeysPathKey = SectionName + ":KeysPath"`.
  - Where they live: `Domain/**/*Errors.cs` (errors), value objects (`Email.MaxLength`), `*Settings` classes
    (configuration keys, e.g. `AuthenticationSettings`, `DataProtectionSettings`, `GatewaySettings`), options classes
    (`Default*` values). The Gateway can't reference the Api, so it keeps its own copies of shared keys in `GatewaySettings`.
  - Tests compare against the production constant (`Assert.Equal(CpfErrors.Invalid, result.Error)`,
    `factory.RateLimits.PermitLimit`), never a re-typed literal. Test input data, including request paths,
    is declared once per test class as `const`/`static readonly` fields.
  - **Exceptions (write literals inline):**
    - Endpoint definitions: group prefixes and tags (in the endpoint group), route templates, `WithName`, and `Created`
      locations (in the endpoint class), and `MapHealthChecks("/health")` stay inline. Don't create `*Routes` classes.
    - The CPF algorithm (`Cpf.cs` check digits, weights, modulus) and the formatted-CPF length in validators.
    - `0`/`1` as loop bounds, increments and empty checks; `string.Empty`; human-readable log, exception and
      ProblemDetails `detail` text; values inside `[InlineData]` that are the test case itself.

## Testing

- Domain: pure unit tests, no mocks needed. Every invariant has a test.
- Application: handler tests with NSubstitute for ports (repositories, `IUnitOfWork`, `TimeProvider` via `FakeTimeProvider`).
- API: integration tests via `WebApplicationFactory` against SQL Server in Testcontainers (Docker required),
  one happy path + main failure per endpoint. Never use the EF Core InMemory provider.
- Gateway: integration tests replace the backend with a stub `IForwarderHttpClientFactory` and use a test auth scheme.
  Any change to routes, auth, or rate limiting needs a test there. Keep it a separate project: the API and the gateway
  both generate a global `Program` class, so one test project can't reference both.
- New behavior → new test in the same change.

## Sensitive data (patient health information)

This system stores clinical data. Health data is _sensitive personal data_ under LGPD (Lei 13.709/2018, art. 11),
and record keeping follows CFP rules (Resolution 001/2009) and the Psychologist's Code of Ethics (professional secrecy).

- Every access to a clinical record is written to an audit trail (who, when, which record), with no clinical content in the log.
- Patients can request their data (LGPD data subject rights). Keep export of a patient's data possible.
- Never log patient names, documents, contact info, or clinical notes. Log IDs only.
- Never put real or realistic patient data in seeds, tests, or examples. Use obviously fake data.
- Clinical note content is encrypted at the application level: an EF Core value converter in Infrastructure
  (`Persistence/Encryption`) uses ASP.NET Core Data Protection. Domain sees plain text; the database stores only ciphertext.
  Encrypted columns cannot be filtered or searched in SQL, so don't write queries that depend on their content.
- Data Protection keys are never stored in the database or the repo. Production: keys persisted to Azure Blob Storage
  and protected by Azure Key Vault (`PersistKeysToAzureBlobStorage` + `ProtectKeysWithAzureKeyVault`).
  Development: a local file-system folder outside the repo. Losing the keys means losing the clinical notes, so never rotate or delete keys by hand.
- A psychologist can only access their own patients (see Authentication & authorization).
- No secrets in `appsettings*.json`; use user-secrets / environment variables.

## Boundaries

**Always:** follow the dependency rule, add tests, run build+test+format before finishing.
**Ask first:** adding NuGet packages, changing aggregate boundaries, schema changes that drop/rename columns, changing auth,
changing rate-limit values or scope.
**Never:** reference EF Core or ASP.NET from Domain, call `DbContext` from endpoints, commit secrets,
log PHI, edit applied migrations, disable nullable or analyzers to make code compile,
introduce magic strings/numbers or re-type a value that already has a constant.
