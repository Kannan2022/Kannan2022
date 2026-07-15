# PROMPTS — Building the Notification & Audit Service with AI Pair-Programming

This document records how GitHub Copilot (AI pair-programming) was used to build and evolve the
Notification & Audit Service, the prompting techniques that worked, and — importantly — where the
AI's output needed human correction.

## Provenance & honesty note

To keep this record trustworthy, each prompt below is tagged:

- **[actual]** — the verbatim prompt used during the recorded working session (the reorganization,
  the Projects feature, the review, the production rewrite, the tests, and the supporting docs).
- **[representative]** — the foundational **Notifications** and **Audit** features predate the
  recorded session. The prompts shown for them are *representative*: they reproduce the committed
  code when run against [`.github/copilot-instructions.md`](../.github/copilot-instructions.md). They
  illustrate the approach rather than claim to be the exact historical keystrokes.

---

## The standing context: `copilot-instructions.md` as a persistent system prompt

The single most effective "prompt" was not typed per request — it was
[`.github/copilot-instructions.md`](../.github/copilot-instructions.md). It pins the tech stack
(.NET 8, EF Core, SQLite), the layered architecture (Models → Services → Controllers), naming,
logging, validation, security, and testing standards. Because Copilot reads it as ambient context,
every subsequent prompt could stay short ("generate a Project service") and still yield code in the
house style. **Investing in the instructions file paid compounding returns on every later prompt.**

---

## Prompt log by phase

### 1. Foundational data models — Notifications & Audit *[representative]*

> Create EF Core entity models for a Notification (Id, Title, Message, Type, RecipientId,
> RecipientEmail, IsRead, CreatedAt, ReadAt, RelatedEntityType, RelatedEntityId) and an AuditLog
> (Id, EntityName, EntityId, Action, OldValues, NewValues, UserId, UserName, Timestamp, IpAddress,
> UserAgent). Use nullable reference types and `required` where appropriate.

**Produced:** [`Notification.cs`](../src/notifications/Notification.cs),
[`AuditLog.cs`](../src/audit/AuditLog.cs). Copilot correctly applied `required`/`?` per the instructions.

> Create an AuditDbContext with DbSets for both, configure max lengths and indexes.

**Produced:** [`AuditDbContext.cs`](../src/Data/AuditDbContext.cs) with fluent configuration.

### 2. Service layer — interface-first *[representative]*

> Generate an INotificationService interface and implementation with create, get-for-user
> (optional isRead filter), get-by-id, mark-as-read, mark-all-as-read, delete, and delete-old.
> Async throughout, EF Core, constructor DI.

**Produced:** [`INotificationService.cs`](../src/notifications/INotificationService.cs) /
[`NotificationService.cs`](../src/notifications/NotificationService.cs); the Audit equivalents were
generated the same way. Interface-first prompting kept the DI contracts clean.

### 3. API controllers *[representative]*

> Add ASP.NET Core controllers for notifications and audit logs with the documented REST routes,
> `[ApiController]` model validation, and request DTOs.

**Produced:** [`NotificationsController.cs`](../src/notifications/NotificationsController.cs),
[`AuditController.cs`](../src/audit/AuditController.cs), wired in [`Program.cs`](../src/Program.cs).

### 4. Repository reorganization into a feature-based layout *[actual]*

> Understand the code and arrange the folder in [a `src/` structure with `.github/`, feature
> folders, `tests/`, `README.md`, and the dependency file].

**Produced:** files moved with `git mv` into [`src/notifications/`](../src/notifications),
[`src/audit/`](../src/audit), [`src/Data/`](../src/Data); namespaces refactored to match; `.csproj`
hardened to exclude `tests/**`. **Human judgment caught a latent bug** in `Program.cs`
(`WebApplicationBuilder.CreateBuilder` → `WebApplication.CreateBuilder`), found only by building.

### 5. Projects feature — model & service *[actual]*

> Generate a Project model and a Project service with create, update status, get by team, and
> delete functions. Use a database.

**Produced:** [`Project.cs`](../src/projects/Project.cs) (+ `ProjectStatus` enum stored as text),
[`IProjectService.cs`](../src/projects/IProjectService.cs) /
[`ProjectService.cs`](../src/projects/ProjectService.cs), `DbSet<Project>` + config, DI registration,
and a migration. Short prompt, house-style output — the instructions file did the heavy lifting.

### 6. Structured review & hardening *[actual]*

> Create a REVIEW.md file for project service
> … then: review file based on [severity, multi-tenant impact, detection method, fixes, and a
> section on issues Copilot introduced that required human judgment].

**Produced:** [`REVIEW.md`](REVIEW.md). Reviewing the AI's own output through a multi-tenant lens
surfaced a **cross-tenant IDOR** — `UpdateProjectStatusAsync` and `DeleteProjectAsync` looked up by
`id` only. Every automated signal was green; only human judgment caught it. The two critical issues
were then fixed.

### 7. Production rewrite — layered + multi-tenant *[actual]*

> Rewrite the Project Service to production standards: proper layered architecture
> (model → repository → service → controller/route); ORM-based data access; input validation, typed
> request/response contracts, specific error handling, structured logging; multi-tenant isolation
> (users may only access projects belonging to their organisation); documentation on all public methods.

**Produced:** [`IProjectRepository`](../src/projects/IProjectRepository.cs) /
[`ProjectRepository`](../src/projects/ProjectRepository.cs), an org-aware
[`ProjectService`](../src/projects/ProjectService.cs), typed [contracts](../src/projects/Contracts), a
[`ProjectsController`](../src/projects/ProjectsController.cs), and an
[`ITenantContext`](../src/Common/ITenantContext.cs) resolving the tenant **from the authenticated
principal's claims, never from request input**.

### 8. Integration tests proving isolation *[actual]*

> add it under tests/

**Produced:** [`ProjectsIntegrationTests.cs`](../tests/NotificationAuditService.Tests/Projects/ProjectsIntegrationTests.cs)
+ a [`WebApplicationFactory`](../tests/NotificationAuditService.Tests/TestInfrastructure/ProjectApiFactory.cs)
injecting a tenant via header. 11 tests pass, including cross-tenant update/delete returning `404`.

### 9. Supporting documentation *[actual]*

> Create a REVIEW.md / IMPACT_ANALYSIS.md / PROMPTS.md …

**Produced:** [`REVIEW.md`](REVIEW.md), [`IMPACT_ANALYSIS.md`](IMPACT_ANALYSIS.md), and this file.
For `IMPACT_ANALYSIS.md`, the prompt arrived without its details; the correct response was to **ask
rather than invent** the contents.

---

## Where Copilot excelled vs. where a human was essential

**Copilot was strong at:** boilerplate against a clear spec (entities, DbContext, CRUD, controllers,
DTOs); staying consistent with the ambient instructions; mechanical refactors and test scaffolding.

**Human judgment was required for (see [`REVIEW.md`](REVIEW.md)):**
- **Multi-tenant security** — Copilot copied the single-tenant `id`-only lookup into a tenant-scoped
  entity, producing a cross-tenant IDOR. It has no concept that the tenant id is a *security boundary*.
- **Trust boundaries** — the tenant id must come from the authenticated principal, not client input.
- **Blast radius** — pagination, soft-delete, audit trails, retention weren't in the prompt, so didn't appear.
- **Verifying reality** — building/running/testing caught a compile bug and confirmed isolation.

---

## Effective prompt patterns (lessons)

1. **Front-load standards into `copilot-instructions.md`.**
2. **Interface-first prompts** produce cleaner, testable seams.
3. **Name the non-functionals explicitly** ("production standards: layering, validation, isolation, docs").
4. **Review the AI adversarially, in context** — "what is the trust boundary and who calls this?" found the IDOR.
5. **Always verify by execution.**
6. **When a prompt is under-specified, ask.**
