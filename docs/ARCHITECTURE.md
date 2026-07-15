# Architecture

**Service relationship & integration contract.** The Project Service and the Notification & Audit Service are feature slices in one modular-monolith (ASP.NET Core process, shared EF Core `AuditDbContext`/SQLite). They integrate in-process through the interface contract `IAuditService.LogChangeAsync(entityName:"Project", entityId, action, oldValues, newValues, userId, ipAddress, userAgent)` — the agreed way project lifecycle events are recorded. (This contract is established and documented; the wiring is deferred, see `REVIEW.md` M1 / `IMPACT_ANALYSIS.md`.)

**Layered architecture & data flow.** Request → **Controller** (typed DTO validation, tenant resolved from the `org_id` claim via `ITenantContext`) → **Service** (business rules, structured logging) → **Repository** (ORM access, every query scoped by `OrganizationId`) → **DbContext** → SQLite. A change persists a Project row and, per the contract above, an `AuditLog` row; user-facing events flow the same path into `Notification` rows.

**Why it fits multi-tenant B2B SaaS.** Tenant identity comes from the authenticated principal (never client input) and is enforced at a single choke point (the repository), so isolation is centralized and test-provable. Layering keeps that boundary auditable, and the shared store gives transactional consistency across project/audit/notification writes.

**Key decisions & trade-offs.** Modular monolith over microservices — simplicity and consistency now, at the cost of independent scaling/deploy (a future split). Application-enforced isolation over DB row-level security — simpler with EF, but one un-scoped query would leak, so a global query filter is recommended as defense-in-depth. Enum status stored as text for readable, reorder-safe rows.
