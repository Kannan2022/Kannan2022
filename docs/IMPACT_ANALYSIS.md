# Impact Analysis — Capturing Caller IP Addresses in the Audit Trail

**Date:** 2026-07-14
**Status:** Proposed — pending governance sign-off (see §3, Phase 0)

## Change under analysis

> Capture the **caller's IP address and user agent server-side** and persist them to the audit
> trail for **project lifecycle events** (create / update-status / delete), by integrating
> `ProjectService` with the existing `IAuditService`. This also replaces today's **client-supplied**
> IP/user-agent in the audit path ([`AuditController`](../src/audit/AuditController.cs)) with values
> taken from the trusted request pipeline.

> **Scope note:** The `AuditLog` entity already declares `IpAddress` and `UserAgent`
> ([`src/audit/AuditLog.cs`](../src/audit/AuditLog.cs)), so this change primarily *populates* existing
> fields from a trustworthy source and *starts recording* project events.

---

## 1. Files, modules, and data models affected

**Legend:** 🟢 Additive (backward compatible) · 🔴 Breaking · 🗄️ Migration required · ⚪ No change

### Data models

| Model | Change | Nature |
|-------|--------|--------|
| [`AuditLog`](../src/audit/AuditLog.cs) | `IpAddress` / `UserAgent` columns already exist — now populated from server context. No column change needed **for IP itself**. | 🟢 Additive |
| [`AuditLog`](../src/audit/AuditLog.cs) | **Recommended:** add `OrganizationId` so audit records (which will now hold PII) are tenant-isolated like `Project`. | 🔴 Breaking · 🗄️ Migration required |
| [`Project`](../src/projects/Project.cs) | None — no new fields required on the project. | ⚪ No change |

### Modules / files

| File / module | Change | Nature |
|---------------|--------|--------|
| `src/Common/IRequestContext.cs` + `RequestContext.cs` **(new)** | New abstraction exposing `IpAddress`/`UserAgent` from `IHttpContextAccessor`, mirroring [`ITenantContext`](../src/Common/ITenantContext.cs). | 🟢 Additive |
| [`ProjectService`](../src/projects/ProjectService.cs) / [`IProjectService`](../src/projects/IProjectService.cs) | Inject `IAuditService` + request context; write an audit record on create/update/delete. Public signatures unchanged. | 🟢 Additive |
| [`ProjectsController`](../src/projects/ProjectsController.cs) | None if IP captured via context. | ⚪ No change |
| [`AuditController`](../src/audit/AuditController.cs) + `LogChangeRequest` | Stop trusting body-supplied `IpAddress`/`UserAgent`; populate from the pipeline. | 🔴 Breaking (API behavior) |
| [`AuditService`](../src/audit/AuditService.cs) / [`IAuditService`](../src/audit/IAuditService.cs) | `LogChangeAsync` already accepts `ipAddress`/`userAgent` — reused. Add `organizationId` only if tenant-scoping. | ⚪ No change (🔴 if extended) |
| [`AuditDbContext`](../src/Data/AuditDbContext.cs) | Only if `AuditLog.OrganizationId` is added: new property config + index. | 🗄️ Migration required (else ⚪) |
| [`Program.cs`](../src/Program.cs) | Register request context; add `UseForwardedHeaders` + known proxies for correct client IP behind a LB; register a retention background service. | 🟢 Additive |
| [`Database/InitialCreate.sql`](../src/Database/InitialCreate.sql) | Update reference schema if `AuditLog` gains `OrganizationId`. | 🗄️ (doc) |
| [`appsettings.json`](../appsettings.json) | Config: trusted proxy networks, audit **retention days**, log-scrubbing toggle. | 🟢 Additive |
| `Migrations/` (generated) | New migration if `AuditLog` schema changes (auto-applied at startup). | 🗄️ Migration required |
| [`tests/`](../tests/README.md) | New tests: IP captured & stored, audit written per op, audit tenant-isolated, retention purge, **logs contain no IP**. | 🟢 Additive |
| Retention worker **(new hosted service)** | `BackgroundService` invoking `DeleteOldAuditLogsAsync` on a schedule. | 🟢 Additive |

**Migration summary:** Capturing IP itself needs **no** migration (columns exist). A migration is
required **only** for the recommended `AuditLog.OrganizationId` tenant-isolation column — additive,
nullable-then-backfill, so low-risk and reversible.

---

## 2. Security & compliance risks introduced by capturing IP addresses

An IP address is **personal data** under GDPR (Recital 30) and personal information under CCPA/CPRA
and LGPD. Recording it turns the audit store into a **PII store**, raising:

### Privacy & legal basis
- **Lawful basis & transparency:** documented basis (legitimate-interest assessment or consent),
  updated privacy notice + Records of Processing; a **DPIA** may be required.
- **Data minimisation:** consider **truncation** (mask last octet / IPv6 suffix) or **keyed hashing**;
  store full IP only where a documented security/fraud need exists.
- **Data subject rights:** IP linked to `UserId` must be discoverable and erasable — in tension with
  audit immutability; reconcile via retention window + controlled purge.
- **Cross-border/residency:** storing region-specific IPs can trigger residency obligations.

### Data retention
- **Indefinite retention risk:** `DeleteOldAuditLogsAsync` exists but is **not scheduled** — PII would
  accumulate forever. Define and **enforce** a retention period (Phase 5).
- **Tamper-evidence vs deletability:** audit trails often must be append-only (SOC 2 / ISO 27001) yet
  retention requires deletion — resolve the policy explicitly.

### Logging exposure (highest operational risk)
- IP flowing into **application logs / APM / SIEM** creates uncontrolled secondary PII copies with
  broader access, different retention, and possible cross-border transfer. The repo's own guidance
  ([`.github/copilot-instructions.md`](../.github/copilot-instructions.md) §4) says *never log PII*.
- **Controls:** never put IP in log message templates; keep it in the DB record only; scrub and review sinks.

### Access control, integrity & tenancy
- **Unauthenticated audit read:** [`AuditController`](../src/audit/AuditController.cs) has **no
  `[Authorize]`** and no tenant scoping — returning IPs would expose one tenant's users' IPs to any
  caller. Add auth + RBAC + tenant scoping **before** storing IP.
- **Spoofing/integrity:** today's IP comes from the client body (forgeable); even server-side, a naive
  `X-Forwarded-For` read is spoofable unless the proxy chain is trusted.
- **Encryption & blast radius:** an audit table of IP + user + actions is a high-value target;
  encrypt at rest and tighten DB access.

### Frameworks touched
GDPR / CCPA-CPRA / LGPD, **SOC 2** (audit integrity, retention, access control), **ISO 27001**.

---

## 3. Recommended implementation approach & sequencing

Phased so the **legal gate** and **isolation/access controls** land *before* any PII is persisted,
with everything behind a flag for clean rollback.

**Phase 0 — Governance (blocking):** LIA/DPIA; decide full vs. masked/hashed IP; set retention;
update privacy notice + Records of Processing; sign-off. *No merges until closed.*

**Phase 1 — Trustworthy IP capture (🟢):** `UseForwardedHeaders` + known proxies in
[`Program.cs`](../src/Program.cs); add `IRequestContext`; unit-test XFF handling. *No persistence yet.*

**Phase 2 — Isolate & lock down the audit store (🔴 · 🗄️):** add `AuditLog.OrganizationId` (+ migration);
tenant-scope audit queries; add `[Authorize]` + RBAC to [`AuditController`](../src/audit/AuditController.cs).

**Phase 3 — Integrate audit into `ProjectService` (🟢, behind a flag):** record create/update/delete
with server-captured IP/UA + org + user.

**Phase 4 — Logging hygiene (🟢):** confirm IP never enters app logs; add scrubbing; classify field sensitive.

**Phase 5 — Enforce retention (🟢):** `BackgroundService` running `DeleteOldAuditLogsAsync`; alert on failures.

**Phase 6 — Deprecate client-supplied IP (🔴):** ignore body `IpAddress`/`UserAgent`; version/communicate.

**Phase 7 — Test & roll out:** integration tests (IP stored, audit tenant-isolated, retention purge,
logs contain no IP); validate real client IP behind the LB in staging; enable gradually.

**Rollback:** flag off disables capture (additive, low blast radius); `OrganizationId` is additive/nullable —
rollback leaves it unused; no destructive migration.

### Sequencing rationale
Legal gate → capture correctly before storing → isolation + access control before persisting PII →
integrate behind a flag → hygiene and retention before GA → remove the legacy forgeable path last.
