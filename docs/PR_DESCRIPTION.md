# PR: Restructure repository + add multi-tenant Project service (with tests & docs)

## Summary — what was built and why

This PR reorganizes the service into a clean, feature-based layout and adds a **production-grade,
multi-tenant Project capability** alongside the existing Notifications and Audit features.

**What changed:**
- **Repository restructure** — moved the flat `NotificationAuditService/` project into a
  feature-foldered `src/` layout (`src/notifications`, `src/audit`, `src/projects`, `src/Data`),
  with the `.csproj`, `README.md`, and config at the repo root; namespaces realigned to match.
- **New Project feature**, layered model → repository → service → controller:
  [`Project`](../src/projects/Project.cs) + [`ProjectStatus`](../src/projects/ProjectStatus.cs),
  [`IProjectRepository`](../src/projects/IProjectRepository.cs)/[impl](../src/projects/ProjectRepository.cs),
  [`IProjectService`](../src/projects/IProjectService.cs)/[impl](../src/projects/ProjectService.cs),
  typed [contracts](../src/projects/Contracts), and
  [`ProjectsController`](../src/projects/ProjectsController.cs).
- **Multi-tenant isolation** — added an `OrganizationId` tenant key (+ EF migration) and an
  [`ITenantContext`](../src/Common/ITenantContext.cs) that resolves the organisation from the
  authenticated principal's `org_id` claim, never from client input. Every data-access path is
  organisation-scoped.
- **Tests** — a [`WebApplicationFactory`](../tests/NotificationAuditService.Tests/TestInfrastructure/ProjectApiFactory.cs)
  integration suite proving cross-tenant isolation.
- **Docs** — `REVIEW.md`, `IMPACT_ANALYSIS.md`, `PROMPTS.md`, `SPEC.md`, `TOOL_STRATEGY.md`, `ARCHITECTURE.md`.

**Why:** the original Project code shipped a cross-tenant IDOR (id-only lookups) and lacked layering,
validation, and tests. In a multi-tenant B2B SaaS, tenant isolation is a hard requirement, so the
service was rebuilt to enforce it structurally and prove it with tests.

## AI Tool Disclosure

**Copilot features used** (detail in [`SPEC.md`](SPEC.md)):
- **Copilot Chat / Ask** with `@workspace` — planning, review, docs.
- **Copilot Edits** — multi-file generation (model/service/DTOs/controller).
- **Copilot Agent mode** — multi-step build-out, running the app and tests.
- **`/tests` slash command** — test scaffolding.
- **Custom instructions** — [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) as
  standing context for every request.

**Where AI output was accepted vs. overridden:**
- **Accepted (largely as-generated):** CRUD/repository boilerplate, DTOs, EF configuration, DI
  registration, XML docs, and the test scaffolding — all consistent with the instructions file.
- **Overridden / corrected (human judgment):** the **cross-tenant IDOR** (added org-scoped lookups),
  **missing architectural layers** (added repository + DTOs + controller), the **tenant key**
  (`OrganizationId` from claims), **pagination** limits, and two compile bugs
  (`WebApplication.CreateBuilder`, a missing `using`). Full list in
  [`SPEC.md` → Post-Generation Corrections](SPEC.md).

**Estimated AI-generated vs. hand-written** (approximate; not measured by tooling):
- **Code: ~85% AI-generated, ~15% hand-authored/modified.** The 15% is where the value concentrated —
  security fixes, architectural decisions, tenant design, and bug fixes.
- **Docs: ~90% AI-drafted**, with human-directed scope, framing, and honesty/provenance notes.

## Service integration & inter-service contracts

The system is a **modular monolith**: the **Project service** and the existing **Notification &
Audit service** run in one ASP.NET Core process over a single EF Core `AuditDbContext` (SQLite),
exposed under one `/api` surface. "Integration" is therefore in-process today, with explicit contracts:

- **In-process interface contracts:** `IProjectService`/`IProjectRepository`, `IAuditService`,
  `INotificationService`, and the cross-cutting `ITenantContext`.
- **HTTP contracts:** typed request/response DTOs
  ([`CreateProjectRequest`](../src/projects/Contracts/CreateProjectRequest.cs),
  [`UpdateProjectStatusRequest`](../src/projects/Contracts/UpdateProjectStatusRequest.cs),
  [`ProjectResponse`](../src/projects/Contracts/ProjectResponse.cs)), RFC-7807 `ProblemDetails` errors,
  and enums serialized as strings.
- **Tenancy contract (shared across services):** an `org_id` claim → `ITenantContext.OrganizationId`
  → organisation-scoped persistence. This is the contract that makes every feature tenant-safe.

**Planned inter-service contract (designed, not yet wired):** Project lifecycle events
(create/update-status/delete) will be recorded through the Audit service via
`IAuditService.LogChangeAsync(entityName: "Project", entityId, action, oldValues, newValues, userId,
ipAddress, userAgent)`. This is specified in [`REVIEW.md`](REVIEW.md) (finding M1) and
[`IMPACT_ANALYSIS.md`](IMPACT_ANALYSIS.md) but is **intentionally out of scope for this PR** —
flagged here so reviewers know the Project service does **not** currently emit audit records.

## Testing coverage & known gaps

**Covered — 11 integration tests, all passing** ([`ProjectsIntegrationTests`](../tests/NotificationAuditService.Tests/Projects/ProjectsIntegrationTests.cs)):
- **Multi-tenant isolation:** cross-tenant list returns empty; cross-tenant status-update and delete
  return `404` and leave the target unchanged (verified against the owning org).
- **Happy paths:** create → `NotStarted`; own status-update persists + sets `UpdatedAt`; own delete
  removes; status filter returns only matches.
- **Fail-closed:** requests without an organisation return `401` (GET/POST/DELETE).
- **Validation:** missing/empty name and missing teamId return `400` (`[Theory]`).
- The EF SQL captured during runs confirms real scoping (`WHERE OrganizationId = @org AND TeamId = @team`).

**Known gaps:**
- **No unit tests** for `ProjectService` in isolation (with a mocked repository).
- **No tests for Notifications/Audit** features (pre-existing).
- **Coverage not formally measured** — the instructions' 80% target is unverified (no Coverlet run).
- **Auth is stubbed** via a test header, not a real JWT/identity provider; production claim wiring is untested.
- **Untested because unbuilt:** the Project→Audit integration; retention purge; optimistic concurrency.
- No load/performance testing; pagination boundary and invalid-enum cases are only partially covered.

## Risks & trade-offs in the multi-service design

**Primary trade-off — shared database / modular monolith.** All features share one
`AuditDbContext` and one SQLite database. This buys simplicity (one deploy, transactional
consistency, no network hops) at the cost of **coupling**: a schema change or migration affects every
feature, features can't be scaled or deployed independently, and the database is a single point of
contention/failure. If these become true microservices, this shared store must be split — a
significant future migration. *Chosen deliberately* for delivery speed at the current scale.

**Secondary risk — tenant isolation is application-enforced, not database-enforced.** Isolation lives
in the repository's `WHERE OrganizationId = …` filters, not in a DB row-level-security policy or an
EF global query filter. It is centralized (single choke point) and test-covered, but a future query
that bypasses the repository would silently leak across tenants. Defense-in-depth (a global query
filter driven by `ITenantContext`) is recommended in `REVIEW.md`/`IMPACT_ANALYSIS.md`.

## Self-review checklist (run before submitting)

- [x] `dotnet build` clean — **0 warnings / 0 errors**.
- [x] `dotnet test` — **11/11 passing**.
- [x] App runs (`dotnet run`): startup OK, migrations apply, live `201`/`401` verified, Swagger lists all endpoints.
- [x] Multi-tenant isolation proven by tests (cross-tenant writes → `404`; unscoped read → empty).
- [x] EF migration generated and applies cleanly; `Projects` table + tenant-first indexes present.
- [x] No hardcoded secrets or connection strings; `.gitignore` covers `bin/`, `obj/`, `*.db`, `Migrations/`.
- [x] Namespaces consistent with the new folder layout; `using`s updated.
- [x] Public methods/interfaces documented with XML docs; DTOs validated with data annotations.
- [x] Main project excludes `tests/**` from its build; test project references the app via `ProjectReference`.
- [x] Docs cross-referenced and honest about scope (provenance notes in `PROMPTS.md`/`SPEC.md`).
- [ ] **Not done (called out above):** code-coverage measurement, real JWT auth, Project→Audit wiring,
      CORS tightening (still `AllowAll`), performance testing.
