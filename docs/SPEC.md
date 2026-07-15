# SPEC — Prompt Chain & Technique Log

The ordered chain of prompts used to build and evolve the service, the Copilot feature chosen for
each, the prompting technique applied, and the rationale. A coverage matrix and a
**Post-Generation Corrections** log close the document.

> **Note on provenance:** prompts 1–10 are the verbatim requests executed in this working session
> (see [`PROMPTS.md`](PROMPTS.md) for how the earlier Notifications/Audit foundations were built).
> "Copilot feature" names the tool surface each request maps to in a Copilot-based workflow.

---

## Prompt chain (in execution order)

### Prompt 1 — Understand & reorganize the repository
- **Exact text:** *"Understand the code and arrange the folder in below structure"* followed by the
  target tree (`.github/`, `src/projects/{model,service}`, `src/notifications/`, `tests/`,
  `README.md`, dependency file).
- **Copilot feature:** Copilot Chat with **`@workspace`** (whole-repo context).
- **Technique:** **Specificity** — the exact target tree removed ambiguity.
- **Rationale:** Reorganization needs full-repo awareness; the precise structure let the model plan
  `git mv`s and namespace changes instead of guessing.

### Prompt 2 — Generate the Projects model & service
- **Exact text:** *"Generate a Project model and a Project service with create, update status, get by
  team, and delete functions. Use a database."*
- **Copilot feature:** **Copilot Edits** (multi-file generation).
- **Technique:** **Decomposition** — the feature was broken into a model plus four named operations.
- **Rationale:** Enumerating operations produces a complete, well-scoped service; the standing
  instructions supplied the unstated conventions.

### Prompt 3 — Draft the review document
- **Exact text:** *"Create a REVIEW.md file for project service"*
- **Copilot feature:** Copilot Chat (Ask).
- **Technique:** **Role-based** — implicitly asks the model to act as a code reviewer.
- **Rationale:** Establishes a first-pass artifact to iterate on before constraining it.

### Prompt 4 — Refine the review with required structure
- **Exact text:** *"review file based on A structured code review documenting every issue you found:
  What the issue is, where it is, its severity, and its impact (especially in a multi-tenant B2B
  SaaS context); How you detected it … where Copilot helped and where your own judgment was needed;
  The fix you applied or recommend; A section at the end: 'Architectural & Security Issues Copilot
  Introduced That Required Human Judgment' …"*
- **Copilot feature:** Copilot Chat (Ask), same thread.
- **Technique:** **Iterative refinement** — reshapes Prompt 3's output with an explicit rubric.
- **Rationale:** The first draft was generic; the multi-tenant lens + fixed sections forced the
  deeper analysis that uncovered the IDOR.

### Prompt 5 — Rewrite to production standards
- **Exact text:** *"Rewrite the Project Service to production standards: Proper layered architecture:
  model → repository → service → controller/route; ORM-based data access (no raw database driver);
  Input validation, typed request/response contracts, specific error handling, structured logging;
  Multi-tenant isolation: users may only access projects belonging to their organisation;
  Documentation on all public methods/functions."*
- **Copilot feature:** **Copilot Agent mode** / Copilot Edits (multi-file build-out).
- **Technique:** **Constraint** — an explicit checklist of non-functional requirements.
- **Rationale:** Naming each requirement is far more reliable than "make it production-ready"; each
  became a verifiable artifact.

### Prompt 6 — Add the test suite
- **Exact text:** *"add it under tests/"*
- **Copilot feature:** Copilot **slash command `/tests`**.
- **Technique:** **Few-shot** — the test examples in
  [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) (AAA,
  `Method_Condition_ExpectedResult`) act as exemplars.
- **Rationale:** With worked examples in the instructions, a terse follow-up produces tests in the
  expected shape; focus was proving isolation end-to-end.

### Prompt 7 — Run and verify
- **Exact text:** *"Run the app"*
- **Copilot feature:** **Copilot Agent mode** (terminal / dev server).
- **Technique:** **Iterative refinement** (verification loop).
- **Rationale:** Running confirmed startup, migrations, live 201/401, and Swagger — closing the loop.

### Prompt 8 — Impact analysis
- **Exact text:** *"Create IMPACT_ANALYSIS.md"* with details: *"Every file, module, and data model
  affected … (additive, breaking, migration required); Security and compliance risks introduced by
  capturing IP addresses …; Recommended implementation approach and sequencing."*
- **Copilot feature:** Copilot Chat with `@workspace`.
- **Technique:** **Constraint** + **specificity** (three mandated sections).
- **Rationale:** The first send arrived without details, so the correct move was to **ask rather than
  invent**; once supplied, the fixed sections produced a grounded analysis.

### Prompt 9 — Prompt documentation
- **Exact text:** *"Create PROMPTS.md documenting how you used GitHub Copilot to build the
  Notification & Audit Service."*
- **Copilot feature:** Copilot Chat (Ask).
- **Technique:** **Role-based** (act as author documenting the workflow).
- **Rationale:** Captures the workflow and the human-judgment boundary as an artifact.

### Prompt 10 — This specification
- **Exact text:** *"create SPEC.md: The prompt chain … For each prompt: the exact text, which Copilot
  feature … which prompting technique … a brief rationale … at least 2 different Copilot features and
  at least 3 different prompting techniques … a closing section: 'Post-Generation Corrections'."*
- **Copilot feature:** Copilot Chat with `@workspace`.
- **Technique:** **Constraint** (mandated contents + minimum counts).
- **Rationale:** A meta-prompt satisfied by the matrix and corrections log below.

---

## Coverage matrix

**Copilot features used (≥ 2 required — 4 used):**

| Feature | Prompt(s) |
|---------|-----------|
| Copilot Chat / Ask (incl. `@workspace`) | 1, 3, 4, 8, 9, 10 |
| Copilot Edits (multi-file) | 2, 5 |
| Copilot Agent mode (multi-step / terminal) | 5, 7 |
| Slash command `/tests` | 6 |
| Custom instructions (`copilot-instructions.md`) | standing context for all |

**Prompting techniques used (≥ 3 required — 6 used):**

| Technique | Prompt(s) |
|-----------|-----------|
| Specificity | 1, 8 |
| Decomposition | 2 |
| Role-based | 3, 9 |
| Iterative refinement | 4, 7 |
| Constraint | 5, 8, 10 |
| Few-shot | 6 |

---

## Post-Generation Corrections

Every change made to the AI's generated output, what was wrong, and how it was fixed. Split into
**applied** (changed in code) and **identified** (recommended, tracked in the docs).

### Applied

1. **Broken entry point — [`src/Program.cs`](../src/Program.cs).**
   *Wrong:* generated code called `WebApplicationBuilder.CreateBuilder(args)` → `CS0117`.
   *Fix:* corrected to `WebApplication.CreateBuilder(args)`. *Detected by:* build.

2. **Namespaces not aligned to the new layout (reorg).**
   *Wrong:* after moving files, namespaces/usings still referenced `…Models/…Services/…Controllers`.
   *Fix:* refactored to `…Notifications`/`…Audit`; updated usings in
   [`AuditDbContext.cs`](../src/Data/AuditDbContext.cs) and `Program.cs`. *Detected by:* build.

3. **Cross-tenant IDOR (critical) — `UpdateProjectStatusAsync` / `DeleteProjectAsync`.**
   *Wrong:* id-only lookups let any tenant mutate/delete another's project.
   *Fix:* org-scoped lookups; production rewrite resolves the tenant from the principal and enforces
   it in [`ProjectRepository`](../src/projects/ProjectRepository.cs). *Detected by:* human review (Prompt 4).

4. **No pagination cap — `GetProjectsByTeamAsync`.**
   *Wrong:* returned all rows. *Fix:* `skip`/`take` with clamping in
   [`ProjectService`](../src/projects/ProjectService.cs) + `Skip/Take` in the repository.

5. **Missing architectural layers.**
   *Wrong:* no repository, DTOs, or dedicated controller. *Fix:* added
   [`IProjectRepository`](../src/projects/IProjectRepository.cs), typed
   [contracts](../src/projects/Contracts), and [`ProjectsController`](../src/projects/ProjectsController.cs).

6. **No tenant key on the model.**
   *Wrong:* `Project` had no organisation field. *Fix:* added `OrganizationId` to
   [`Project`](../src/projects/Project.cs) + `AuditDbContext` config + migration; tenant-first indexes.

7. **Test project would not compile — [`ProjectApiFactory.cs`](../tests/NotificationAuditService.Tests/TestInfrastructure/ProjectApiFactory.cs).**
   *Wrong:* `RequestDelegate` unresolved (`CS0246`). *Fix:* added `using Microsoft.AspNetCore.Http;`.

8. **Entry point not testable.**
   *Wrong:* top-level `Program` is `internal`. *Fix:* added `public partial class Program { }`.

9. **Main build would sweep in test files.**
   *Wrong:* root `.csproj` default globbing would compile `tests/**`. *Fix:* `Compile/Content/None
   Remove="tests/**"` in `NotificationAuditService.csproj`.

### Identified (recommended; see [`REVIEW.md`](REVIEW.md) / [`IMPACT_ANALYSIS.md`](IMPACT_ANALYSIS.md))

10. **Caller-supplied IP is forgeable — [`AuditController`](../src/audit/AuditController.cs).** Capture
    server-side via `IHttpContextAccessor` + `UseForwardedHeaders`.
11. **Audit store not tenant-scoped or access-controlled.** Add tenant column + RBAC before storing PII.
12. **Overly permissive CORS — `Program.cs` (`AllowAll`).** Restrict to known origins for production.
13. **Retention not enforced.** `DeleteOldAuditLogsAsync` exists but is never scheduled — add a purge service.
