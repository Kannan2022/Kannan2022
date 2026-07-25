# PR: Restructure repo + add multi-tenant Project, Transaction & Expense modules

**Branch:** `notificationAuditserviceCl` → `main`
**Open:** https://github.com/Kannan2022/Kannan2022/compare/main...notificationAuditserviceCl?expand=1
**Status:** build clean · **29/29 tests passing**

## Summary
Restructures the service into a feature-based layout and adds three production-grade, **multi-tenant**
domain modules — **Project**, **Transaction** (fintech), and **Expense-splitting** — on top of the
existing Notifications/Audit foundation. The initial AI-generated feature code shipped a cross-tenant
IDOR and lacked layering, validation, and tests; in a multi-tenant B2B SaaS, tenant isolation of
business and financial data is a hard requirement, so each module now enforces it structurally and
proves it with tests.

## Type of change
- [x] Feature · [x] Refactor/restructure · [x] Tests · [x] Docs · [x] Security fix

## Changes
- **Repository restructure** → feature-based `src/` (`notifications`, `audit`, `projects`,
  `transactions`, `expenses`, `Common`, `Data`); `.csproj`/config/`README` at root; namespaces realigned.
- **Project feature** — model → repository → service → controller; `OrganizationId` tenant key;
  create / update-status / get-by-team / delete; pagination.
- **Transaction module** (fintech) — same layered, tenant-scoped pattern; ISO-4217 currency +
  non-zero amount validation; **append-only ledger** (removed the unscoped `DeleteAllTransactionsAsync`).
- **Expense-splitting** — `SharedExpense` + `ExpenseParticipant`; Equal/Exact split computed in
  integer minor units (cents); `BalanceCalculationService` (net balances + greedy settlements;
  rejects mixed currencies); `ExpensesController` endpoints.
- **Multi-tenant plumbing** — `ITenantContext` resolves the `org_id` claim; every feature controller
  fail-closes with `401` when no tenant is present.
- **Data/infra** — DbContext config with tenant-first indexes; EF migrations now tracked (schema
  builds on a fresh clone); `src/Database/InitialCreate.sql` updated.
- **Collaboration docs** — `docs/` set (`ARCHITECTURE`, `SPEC`, `PROMPTS`, `PROMPT_ENGINEERING`,
  `TOOL_STRATEGY`, `REVIEW`, `IMPACT_ANALYSIS`, `PR_DESCRIPTION`) + `CONTRIBUTING.md`, `CHANGELOG.md`,
  PR template, `CODEOWNERS`.

## Multi-tenant & security
- [x] All data-access paths scoped by `OrganizationId`, resolved from the authenticated principal — never from client input.
- [x] Closed a cross-tenant IDOR in the Project service (id-only lookups); added tenant isolation to transactions & expenses (previously scoped only by `UserId`).
- [x] Input validated at the controller boundary; errors returned as RFC-7807 `ProblemDetails`.
- [x] No secrets/PII logged. Removed an unscoped mass-delete operation.

## AI tool disclosure
- **Modes:** Ask (diagnosis/review/docs), Edit (multi-file generation), Agent (edit→build→migrate→test loop).
- **Features:** `@workspace`, `#file`, `/tests`, custom instructions (`.github/copilot-instructions.md`).
- **Accepted vs. overridden:** boilerplate/DTOs/EF config/tests accepted; **overridden** the IDOR,
  missing layers, tenant key, pagination, and compile bugs — full list in `docs/SPEC.md`.
- **Estimate (approx.):** code ~85% AI-generated / ~15% hand-authored (security, architecture,
  money-safety); docs ~90% AI-drafted with human framing.

## Testing
```
dotnet build NotificationAuditService.csproj
dotnet test tests/NotificationAuditService.Tests/NotificationAuditService.Tests.csproj
```
- [x] Build clean (0 warnings / 0 errors)
- [x] **29/29 passing** — Project (11), Transaction (9), Expense (9)
- [x] Covers cross-tenant isolation (writes → `404`, reads empty), fail-closed `401`, split/balance
  correctness (cent remainder; netting drops settled users; mixed-currency `400`), and validation.

## Database
- [x] EF migrations added and now tracked in `Migrations/`; `Program.cs` applies them at startup.
- [x] `src/Database/InitialCreate.sql` updated to match.

## Risks & trade-offs
- **Shared database / modular monolith** — simplicity + transactional consistency now, at the cost of
  independent scale/deploy (a future split would be a real migration).
- **Application-enforced tenant isolation** (repository filters) rather than DB row-level security —
  centralised and test-covered, but a query bypassing the repository would leak; a global query
  filter is recommended as defense-in-depth (see `docs/REVIEW.md`).

## Known gaps (tracked, out of scope)
No unit tests with mocked repositories; coverage not tool-measured; auth is a test stub (no real JWT);
Project/Transaction→Audit wiring, retention, and transaction idempotency not yet built; CORS still `AllowAll`.

## How to review
Start with `docs/ARCHITECTURE.md`, then the `src/expenses` module (newest), then the tests.

## Checklist
- [x] Follows `.github/copilot-instructions.md` (layering, naming, async, XML docs)
- [x] Public methods/interfaces documented
- [x] README index + `CHANGELOG.md` updated
- [x] Self-reviewed the diff
