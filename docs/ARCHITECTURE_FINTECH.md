# Fintech Architecture — Transaction & Expense-Splitting

**How they relate.** The Transaction and Expense-splitting modules are two money-handling feature
slices in the same modular monolith, sharing one `AuditDbContext`/SQLite and the same tenancy
contract (`org_id` claim → `ITenantContext` → `OrganizationId`). They're complementary but
independent: **Transaction** is an append-only per-user ledger of individual money movements, while
**Expense-splitting** records shared expenses and *computes* who-owes-whom (net balances +
settlements). They don't call each other today; a natural future link is posting a settlement as
concrete transactions.

**Layered architecture & data flow.** `Route (Controller)` → `Service` → `Repository` →
`Model`/DbContext → SQLite. The controller validates the typed DTO, resolves the tenant from the
authenticated principal, and returns `ProblemDetails` on error; the service holds business rules
(money math in integer cents, validation, structured logging); the repository is the single EF Core
data-access point, **org-scoped on every query**; the model/DbContext persist with tenant-first indexes.

**Why it fits fintech.** Tenant isolation is enforced at one choke point (the repository), so
financial data can't leak across organisations; money is `decimal` and split/balanced in integer
minor units to avoid rounding drift; the append-only transaction ledger preserves auditability;
boundary validation blocks malformed money operations; and a single DB gives transactional
consistency across a create + its participant rows.

**Key design decisions.** `OrganizationId` always from the authenticated principal, never client
input; removed the unscoped `DeleteAllTransactionsAsync` (immutable ledger — corrections are
compensating entries); Equal/Exact splits computed so shares sum exactly to the total; the balance
service **rejects mixed-currency groups** rather than silently netting across currencies; enums
persisted as text for readable, reorder-safe rows.
