# Code Review — Project Service

**Scope:** The `Project` feature (model, service interface, EF Core implementation) and its database wiring.
**Standard:** [`.github/copilot-instructions.md`](../.github/copilot-instructions.md).
**Context:** Reviewed as a shared building block in a **multi-tenant B2B SaaS** platform, where
`TeamId`/`OrganizationId` is the **tenant boundary** and this service is expected to be called by other services.
**Date:** 2026-07-14
**Reviewer verdict:** Approved **only after** the two critical tenant-isolation defects were fixed. Remaining items are recommended follow-ups.

---

## How this review was conducted (process)

The `Project` feature was AI-generated ("Copilot") by asking for a model and a service with
create / update-status / get-by-team / delete, backed by a database. Copilot produced a clean,
consistent, standards-compliant slice very quickly. The review then combined tool-assisted checks
with manual, context-aware reasoning:

**Tool / Copilot-assisted (mechanical correctness):**
- `dotnet build` → 0 warnings / 0 errors.
- `dotnet ef dbcontext info` and a generated+applied migration → confirmed the EF model maps and the
  `Projects` table + indexes are created (`Status` persisted as `TEXT`).
- Pattern conformance: Copilot correctly mirrored the existing `NotificationService` / `AuditService`
  shape — interface-first, async, DI, structured logging, XML docs, indexed queries.

**Human judgment (what the tools/AI could not see):**
- Read every method asking *"what is the trust boundary here, and who calls it?"* rather than
  *"does it compile and match the pattern?"*
- Recognized that `TeamId`/`OrganizationId` is not just a column — it is the **tenant partition key**,
  which makes id-only lookups a cross-tenant access-control defect.
- Weighed compliance/operational concerns (audit trail, data retention, noisy-neighbor, blast radius)
  that only matter because the service is *shared* and *multi-tenant*.

The single most important insight — the tenant-isolation gap in §C1/§C2 — came entirely from human
judgment. Every automated signal (build, migration, tests-would-pass, pattern match) was green.

---

## Severity legend

| Severity | Meaning |
|----------|---------|
| 🔴 Critical | Exploitable cross-tenant access / data loss; fix before any use |
| 🟠 High | Serious correctness, availability, or compliance risk in production |
| 🟡 Medium | Should fix; degrades safety/quality of a shared dependency |
| ⚪ Low | Minor / hygiene |

---

## Findings

### 🔴 C1 — Cross-tenant status update (IDOR / broken tenant isolation) — **FIXED**
- **What:** `UpdateProjectStatusAsync` looked up the project by `id` **only**, ignoring the tenant.
- **Where:** [`src/projects/ProjectService.cs`](../src/projects/ProjectService.cs) (and the interface contract).
- **Impact (multi-tenant B2B):** Any caller that knows or guesses a numeric project id could change
  the status of **any other tenant's** project — an IDOR and a direct breach of tenant isolation.
  Project ids are sequential auto-increment integers, so they are trivially enumerable.
- **How detected:** Human judgment. Copilot copied the single-tenant `MarkAsReadAsync(int id)` pattern.
- **Fix applied:** Scope the lookup to the owning tenant; return `null` when no project matches.

### 🔴 C2 — Cross-tenant delete (IDOR / broken tenant isolation) — **FIXED**
- **What:** `DeleteProjectAsync` looked up and removed by `id` **only**.
- **Where:** [`src/projects/ProjectService.cs`](../src/projects/ProjectService.cs).
- **Impact (multi-tenant B2B):** Enumerable ids allow **destructive, irreversible** deletion of
  another tenant's data — the highest blast-radius defect: cross-tenant *data loss*.
- **How detected:** Human judgment (same root cause as C1).
- **Fix applied:** Scope delete to the owning tenant.

### 🟠 H1 — Tenant id trusted as authorization, with no identity check — **RECOMMENDED**
- **What:** The service trusts whatever tenant id the caller supplies; it never verifies the caller
  is authorized for that tenant.
- **Where:** all methods in [`ProjectService.cs`](../src/projects/ProjectService.cs).
- **Impact:** Tenant scoping is only as trustworthy as the id argument.
- **Recommended fix:** Derive the tenant from the authenticated principal (claims), never from input;
  enforce with `[Authorize]` + a tenant-resolution filter. *(Addressed in the production rewrite via `ITenantContext`.)*

### 🟠 H2 — Unbounded read in `GetProjectsByTeamAsync` — **RECOMMENDED**
- **What:** Returns **all** matching rows with no page size / cap.
- **Where:** [`src/projects/ProjectService.cs`](../src/projects/ProjectService.cs).
- **Impact:** A large tenant can pull an unbounded set, degrading latency for **all** tenants (noisy neighbor).
- **Recommended fix:** Add `skip`/`take` with a default cap. *(Addressed in the production rewrite.)*

### 🟠 H3 — Hard delete with no retention/soft-delete — **RECOMMENDED**
- **Where:** [`src/projects/ProjectService.cs`](../src/projects/ProjectService.cs).
- **Impact:** No recovery from accidental/malicious deletion; conflicts with B2B retention/audit obligations.
- **Recommended fix:** Soft delete with a global query filter; reserve hard delete for an audited retention job.

### 🟡 M1 — Lifecycle changes are not audited, despite `IAuditService` being present — **RECOMMENDED**
- **Where:** [`ProjectService.cs`](../src/projects/ProjectService.cs) (no `IAuditService` dependency).
- **Impact:** "Who changed/deleted this project and when" is a baseline traceability requirement.
- **Recommended fix:** Inject `IAuditService` and log create/update/delete with tenant + acting user.

### 🟡 M2 — No optimistic concurrency control — **RECOMMENDED**
- **Impact:** Concurrent status updates are last-writer-wins → silent lost updates across consumers.
- **Recommended fix:** Add a `rowversion` token and handle `DbUpdateConcurrencyException`.

### 🟡 M3 — Length validation deferred to the database — **RECOMMENDED**
- **Where:** `CreateProjectAsync` vs config in [`src/Data/AuditDbContext.cs`](../src/Data/AuditDbContext.cs).
- **Impact:** Over-length input surfaces as `DbUpdateException` (→ HTTP 500) instead of a clean 400.
- **Recommended fix:** Validate lengths in guard clauses / a request DTO. *(Addressed in the production rewrite.)*

### 🟡 M4 — Tenant isolation is manual per-query (no global query filter) — **RECOMMENDED**
- **Where:** [`src/Data/AuditDbContext.cs`](../src/Data/AuditDbContext.cs).
- **Impact:** Root cause that made §C1/§C2 possible; one forgotten filter = cross-tenant leak.
- **Recommended fix:** Add a tenant `HasQueryFilter` fed by an `ITenantContext`.

### ⚪ L1–L3 — Minor
- No `CancellationToken` support; potentially sensitive data in logs (project name at Info);
  implicit enum default / no DB check constraint. *(CancellationToken addressed in the production rewrite.)*

---

## Test coverage

Recommended `ProjectServiceTests` (xUnit + in-memory/SQLite), per [`tests/README.md`](../tests/README.md):
`Create…_WithValidInput_…`, `Create…_WithEmpty*_ThrowsArgumentException` (`[Theory]`),
`Update…_WithUnknownId_ReturnsNull`, `GetByTeam…_OrderedByCreatedAtDescending`,
`GetByTeam…_WithStatusFilter_…`, `Delete…_WithExistingId_ReturnsTrue`, `Delete…_WithUnknownId_ReturnsFalse`.

## Verdict

**Approved with minor suggestions.** The critical tenant-isolation defects were fixed; the remaining
items are situational enhancements, most of which were addressed in the subsequent production rewrite.
