# Contributing

Thanks for contributing to the Notification & Audit Service. This guide covers local setup, the
conventions we follow, and the pull-request process.

## Prerequisites

- **.NET 8 SDK** (`dotnet --version` → 8.x)
- **EF Core CLI** for migrations: `dotnet tool install --global dotnet-ef`

## Build, test, run

```bash
# from the repository root
dotnet build NotificationAuditService.csproj          # build the app
dotnet test tests/NotificationAuditService.Tests/NotificationAuditService.Tests.csproj   # run tests
dotnet run --project NotificationAuditService.csproj  # run (Swagger at /swagger in Development)
```

## Database & migrations

The app applies migrations on startup (`context.Database.Migrate()`).

> **Note:** `Migrations/` is currently git-ignored, so migrations are generated locally. After a
> schema change, run:
> ```bash
> dotnet ef migrations add <Name> --project NotificationAuditService.csproj
> dotnet ef database update --project NotificationAuditService.csproj
> ```
> Also update the reference schema in `src/Database/InitialCreate.sql` to match.

## Project layout

```
src/
├── notifications/  audit/        # existing domains
├── projects/  transactions/  expenses/   # feature slices (model, repository, service, controller, Contracts/)
├── Common/         # cross-cutting (ITenantContext)
├── Data/           # AuditDbContext
└── Program.cs
tests/              # xUnit integration tests (WebApplicationFactory)
docs/               # architecture, review, and AI-usage documentation
```

Each feature follows **Model → Repository → Service → Controller**. The main project excludes
`tests/**` from its build; the test project references it via `ProjectReference`.

## Coding standards

Follow [`.github/copilot-instructions.md`](.github/copilot-instructions.md): layered architecture,
interface-first services, async with the `Async` suffix, constructor DI, XML docs on public members,
structured logging (never log secrets/PII), nullable reference types, and EF Core patterns.

### Multi-tenancy (non-negotiable)

`OrganizationId` is the tenant boundary. It **must** come from the authenticated principal via
`ITenantContext` — never from request input — and **every** data-access path must be scoped by it.
New read/write methods without tenant scoping will not be approved.

## Tests

- xUnit + `Microsoft.AspNetCore.Mvc.Testing`; name tests `MethodName_Condition_ExpectedResult` (AAA).
- Any data-access change must include a **cross-tenant isolation** test.
- Keep the suite green before opening a PR.

## Branches & commits

- Branch off `main`; do not commit directly to `main`.
- Use [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `docs:`,
  `test:`, `refactor:`, `chore:`.

## Pull requests

1. Fill in the PR template (`.github/pull_request_template.md`) completely.
2. Ensure build + tests pass and paste the result.
3. Update docs: the README index and any relevant `docs/*`, plus `CHANGELOG.md` (Unreleased).
4. Disclose AI-tool usage (mode, accept-vs-override) — see `docs/PROMPT_ENGINEERING.md`.
5. Request review from a code owner (`.github/CODEOWNERS`).
