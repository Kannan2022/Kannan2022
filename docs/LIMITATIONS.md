# Copilot Limitations Encountered

Real situations from this assessment where GitHub Copilot produced incorrect, incomplete, or
inappropriate output — how each was detected and fixed, and what to do differently. No AI tool
produces flawless output; the value is in the detection and correction discipline.

| # | What I Prompted / Did | What Copilot Produced (Specific) | How I Detected the Problem | How I Fixed It | What I'd Do Differently Next Time |
|---|---|---|---|---|---|
| 1 | "Generate a Project model and a Project service with create, **update status**, get by team, and **delete** functions. Use a database." | `UpdateProjectStatusAsync(int id, …)` and `DeleteProjectAsync(int id)` that looked the row up by primary key only — `FirstOrDefaultAsync(p => p.Id == id)` — with **no tenant/owner scoping**. It compiled, matched the existing `NotificationService` pattern, and passed happy-path tests. | **Manual review** through a multi-tenant lens during the `REVIEW.md` step — a cross-tenant IDOR (any tenant could mutate/delete another's project via a guessable integer id). Every automated signal was green. | Scoped the lookups to the tenant: first by `teamId`, then the production rewrite added `OrganizationId` resolved from the authenticated principal, enforced in an org-scoped repository; added cross-tenant tests asserting `404`. | Put multi-tenant isolation into `copilot-instructions.md` and state it as an explicit constraint in the *first* feature prompt, so the tenant boundary is designed in, not retrofitted after a review catches it. |
| 2 | Reorganized the repo and ran the first build against the Copilot-shaped `Program.cs`. | `var builder = WebApplicationBuilder.CreateBuilder(args);` — a **hallucinated API**; `CreateBuilder` lives on `WebApplication`, not `WebApplicationBuilder`. Hard compile error (`CS0117`). | `dotnet build` failed immediately. | Manual edit → `WebApplication.CreateBuilder(args)`. | Build right after any generation instead of assuming it compiles, and reach for `/fix` on the compiler error — the fastest one-step correction for this class of mistake. |
| 3 | "Generate a Project service with … **get by team** …" (and the Transaction/Expense analogues). | `GetProjectsByTeamAsync` returned `.Where(...).OrderByDescending(...).ToListAsync()` — **all rows, no `skip`/`take` cap**. Correct for the literal ask, but an unbounded read on a shared multi-tenant DB. | **Manual review** (scalability/noisy-neighbor reasoning), reinforced by comparing to the repo's own Audit example that caps with `.Take(1000)`. | Added `skip`/`take` with clamping in the production rewrite; documented the page-size limit. | Name the non-functionals explicitly (pagination, `CancellationToken`, limits) in the prompt — Copilot optimizes to exactly what's asked and silently omits unstated concerns. |

## Pattern

The failures were largely invisible to automated signals (#2 was the exception — a compiler catch);
the security and scalability issues (#1, #3) surfaced only through human review reasoning about trust
boundaries and shared-resource impact. The durable fix is front-loading those constraints into the
standing instructions so they stop recurring, rather than catching them one review at a time. See
[`REVIEW.md`](REVIEW.md) and [`SPEC.md`](SPEC.md) (Post-Generation Corrections) for the full record.
