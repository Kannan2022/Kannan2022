# PR: Restructure repo + add multi-tenant Project, Transaction & Expense modules

**Branch:** `notificationAuditserviceCl` → `main`
**Status:** build clean · **29/29 tests passing**

## Summary — what & why

This PR restructures the service into a feature-based layout and adds three production-grade,
multi-tenant domain modules — **Project**, **Transaction** (fintech), and **Expense-splitting** —
on top of the existing Notifications/Audit foundation. The driver: the initial AI-generated feature
code shipped a cross-tenant IDOR and lacked layering, validation, and tests. In a multi-tenant B2B
SaaS, tenant isolation of business and financial data is a hard requirement, so each module is built
to enforce it structurally and prove it with tests.

## Changes

- **Repository restructure** → feature-based `src/` (`notifications`, `audit`, `projects`,
  `transactions`, `expenses`, `Common`, `Data`); `.csproj`/config/`README` at root; namespaces realigned.
- **Project feature** — model → repository → service → controller; `OrganizationId` tenant key;
  create / update-status / get-by-team / delete; pagination.
- **Transaction module** (fintech) — same layered pattern; ISO-4217 currency + non-zero amount
  validation; **append-only ledger** (removed a destructive `DeleteAllTransactionsAsync`).
- **Expense-splitting** — `SharedExpense`/`ExpenseParticipant`; Equal/Exact split in integer cents;
  `BalanceCalculationService` (net balances + greedy settlements; rejects mixed currencies); endpoints.
- **Multi-tenant plumbing** — `ITenantContext` resolves the `org_id` claim; controllers fail-closed `401`.
- **Docs** — full `docs/*` set + `CONTRIBUTING`, `CHANGELOG`, PR template, `CODEOWNERS`.

## AI tool disclosure

Detail in [`SPEC.md`](SPEC.md) and [`PROMPT_ENGINEERING.md`](PROMPT_ENGINEERING.md).

- **Modes:** Ask (diagnosis/review/docs), Edit (multi-file generation), Agent (edit→build→migrate→test loop).
- **Features:** `@workspace`, `#file`, `/tests`, custom instructions (`copilot-instructions.md`).
- **Accepted vs. overridden:** boilerplate/DTOs/EF config/tests accepted as-generated; **overridden**
  the cross-tenant IDOR, missing layers, the tenant key, pagination, and compile bugs — see
  [`SPEC.md` → Post-Generation Corrections](SPEC.md).
- **Estimate (approx., not tool-measured):** code ~85% AI-generated / ~15% hand-authored (the 15% is
  where the value sat — security, architecture, money-safety); docs ~90% AI-drafted with human framing.

## Service integration & inter-service contracts

Modular monolith: all features run in one ASP.NET Core process over one EF Core `AuditDbContext`
(SQLite), under one `/api` surface. Contracts:

- **In-process interfaces:** `I{Project,Transaction,SharedExpense}Repository/Service`,
  `IBalanceCalculationService`, `IAuditService`, `INotificationService`, `ITenantContext`.
- **HTTP contracts:** typed DTOs + RFC-7807 `ProblemDetails`; enums serialised as strings.
- **Tenancy contract (shared):** `org_id` claim → `ITenantContext.OrganizationId` → org-scoped
  persistence — the contract that makes every feature tenant-safe.
- **Planned (designed, not wired):** Project/Transaction lifecycle → Audit via
  `IAuditService.LogChangeAsync(...)`; see [`REVIEW.md`](REVIEW.md) M1 and [`IMPACT_ANALYSIS.md`](IMPACT_ANALYSIS.md).

## Testing & known gaps

**29 integration tests passing** (`WebApplicationFactory` with a header-injected tenant):
- Project (11), Transaction (9), Expense (9) — cross-tenant isolation (writes → `404`, reads empty),
  fail-closed `401`, split/balance correctness (cents remainder; netting drops settled users;
  mixed-currency `400`), and validation.

Gaps (tracked): no unit tests with mocked repositories; coverage not tool-measured; auth is a test
stub (no real JWT); Project/Transaction→Audit wiring, retention, and idempotency not yet built.

## Risks & trade-offs

- **Shared database / modular monolith** — simplicity + transactional consistency now, at the cost of
  independent scale/deploy; a future split would be a real migration.
- **Application-enforced tenant isolation** (repository filters), not DB row-level security — centralised
  and test-covered, but a query bypassing the repository would leak; a global query filter is
  recommended as defense-in-depth (`REVIEW.md`).

## How to review / verify

```bash
dotnet build NotificationAuditService.csproj
dotnet test tests/NotificationAuditService.Tests/NotificationAuditService.Tests.csproj
```
Start with `docs/ARCHITECTURE.md`, then the `src/expenses` module (newest), then the tests.

## Self-review checklist

- [x] Build clean (0/0); tests 29/29.
- [x] Every feature data path scoped by `OrganizationId`; fail-closed `401`.
- [x] Migrations generated & applied; `InitialCreate.sql` updated (note: `Migrations/` git-ignored).
- [x] No secrets/PII logged; validation at the boundary with `ProblemDetails`.
- [x] Docs + README index + `CHANGELOG` updated.
- [ ] Not done (called out): unit tests, coverage %, real JWT, audit wiring, CORS tightening, idempotency.
