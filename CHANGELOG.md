# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Project feature** — layered model → repository → service → controller with multi-tenant
  isolation (`OrganizationId` resolved from the authenticated principal), typed request/response
  contracts, validation, `ProblemDetails` error handling, structured logging, and pagination.
- **Transaction module** (fintech) — rebuilt to the same layered, tenant-scoped pattern; ISO-4217
  currency validation and non-zero amount enforcement; append-only ledger.
- **Expense-splitting feature** — `SharedExpense` + `ExpenseParticipant` model; `SharedExpenseService`
  (Equal/Exact split computed in integer minor units); `BalanceCalculationService` (net balances +
  greedy settlements); `ExpensesController` endpoints.
- **Multi-tenant plumbing** — `ITenantContext`/`TenantContext` resolving the `org_id` claim; tenant
  fail-closed (`401`) on every feature controller.
- **Tests** — 29 xUnit integration tests (Project, Transaction, Expense) via
  `WebApplicationFactory`, proving cross-tenant isolation, split/balance correctness, and validation.
- **Docs** — `ARCHITECTURE`, `PR_DESCRIPTION`, `SPEC`, `PROMPTS`, `PROMPT_ENGINEERING`,
  `TOOL_STRATEGY`, `REVIEW`, `IMPACT_ANALYSIS` under `docs/`, indexed from the README; plus
  `CONTRIBUTING.md`, `CHANGELOG.md`, a PR template, and `CODEOWNERS`.

### Changed
- **Repository restructure** — flat `NotificationAuditService/` project moved to a feature-based
  `src/` layout (`notifications`, `audit`, `projects`, `transactions`, `expenses`, `Data`), with the
  `.csproj`, config, and `README.md` at the repo root; namespaces realigned.
- Enum values (`ProjectStatus`, `SplitType`) persisted as strings for readable, reorder-safe rows.

### Removed
- **`DeleteAllTransactionsAsync`** — an unscoped operation that deleted every tenant's transactions;
  removed in favour of an append-only ledger (corrections are compensating transactions).

### Fixed
- Entry-point bug `WebApplicationBuilder.CreateBuilder` → `WebApplication.CreateBuilder`.

### Security
- Closed a cross-tenant IDOR in the Project service (id-only lookups) by scoping every data-access
  path to the tenant `OrganizationId`.
- Added `OrganizationId` tenant isolation to transactions and expenses (previously scoped only by
  `UserId`).

[Unreleased]: https://github.com/Kannan2022/Kannan2022/compare/main...notificationAuditserviceCl
