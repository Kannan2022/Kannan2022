# TOOL_STRATEGY — AI-Assisted Development Strategy

How AI tooling (GitHub Copilot) was applied to build and evolve the Notification & Audit Service,
which feature fit which task, when AI output was trusted vs. overridden, and the guardrails that kept
the assistance safe. It complements [`SPEC.md`](SPEC.md) (the prompt chain) and
[`PROMPTS.md`](PROMPTS.md) (the prompt log); the concrete corrections it references live in
[`REVIEW.md`](REVIEW.md) and [`SPEC.md`](SPEC.md).

## 1. Purpose & scope

The goal was to use AI to move fast on the *mechanical* majority of the work (scaffolding, CRUD,
tests, docs) while reserving human effort for the *judgment-heavy* minority (architecture, security,
trust boundaries). This document defines the rules that made that split deliberate rather than
accidental. Scope: the repository restructure, the multi-tenant Project feature, its tests, and the
assessment documentation.

## 2. Tool / feature selection matrix

| Task type | Feature chosen | Why it fits | When *not* to use it |
|-----------|----------------|-------------|----------------------|
| Explore / understand the repo, plan a change | **Copilot Chat + `@workspace`** | Whole-repo context; good for "where does X live / what will this touch" | For precise multi-file edits — switch to Edits |
| Generate a feature across several files | **Copilot Edits** | Coordinated model/service/DTO/controller changes in one pass | For risky refactors you can't quickly verify |
| Multi-step build/run/verify loops | **Copilot Agent mode** | Can run `dotnet build/test/run` and iterate | For decisions with legal/security stakes — keep a human in the loop |
| Test scaffolding | **`/tests` slash command** | Produces AAA-style tests seeded by the instructions' examples | For asserting *security invariants* — design those cases yourself |
| Enforce house style on every request | **Custom instructions** (`.github/copilot-instructions.md`) | Standing context so short prompts still yield on-spec code | N/A — always on |
| Prose deliverables (review, impact, docs) | **Copilot Chat / Ask** | Fast first drafts to shape and fact-check | When facts are unknown — verify or ask, don't let it invent |

**Principle:** match the feature to the *blast radius and verifiability* of the task, not just to
convenience. High-verifiability tasks (does it compile? does the test pass?) tolerate more AI
autonomy; low-verifiability tasks (is this tenant-safe?) demand human framing.

## 3. Decision heuristics — accept vs. hand-write vs. override

**Accept AI output when** it is boilerplate against a clear spec, the house style is well-defined in
the instructions, and correctness is cheap to verify (build/test). *Examples: repository CRUD, DTOs,
EF configuration, DI registration, XML docs, test scaffolding.*

**Lead by hand (AI as assistant, not author) when** the decision defines a boundary:
- **Security & trust boundaries** — tenant scoping, where identity comes from, authorization.
- **Architecture** — introducing the repository layer, contracts, error-handling strategy.
- **Data model semantics** — recognising `OrganizationId` as a partition key, not a column.

**Override AI output when** it is confidently wrong. In this project that meant: the cross-tenant
IDOR (id-only lookups), missing architectural layers, absent pagination, and two compile bugs. See
[`SPEC.md` → Post-Generation Corrections](SPEC.md).

**Rule of thumb:** *the more a mistake would cost and the less a test would catch it, the more the
human owns it.*

## 4. Guardrails

1. **Standing instructions first.** `.github/copilot-instructions.md` encodes stack, layering,
   naming, logging, validation, security, and testing so every prompt inherits them.
2. **Verify by execution, always.** Treat green *generation* as unverified until `dotnet build`,
   `dotnet test`, and a real run confirm behavior. (This caught the `WebApplication.CreateBuilder`
   bug and confirmed tenant isolation via captured SQL.)
3. **Never trust AI on tenant/security boundaries.** Assume the model will copy single-tenant
   patterns into multi-tenant code; review every data-access path for a tenant filter.
4. **Ask when under-specified.** When a prompt arrived without its details (the impact analysis),
   the correct response was to ask rather than fabricate contents.
5. **Keep provenance honest.** Distinguish AI-generated from hand-written work and representative
   from actual prompts (as done in `PROMPTS.md`/`SPEC.md`).
6. **No secrets, no PII in logs.** Enforced by the instructions and re-checked in review.

## 5. Effectiveness & cost/benefit

**Where AI paid off (high benefit, low review cost):**
- Feature scaffolding and CRUD — minutes instead of an hour, on-spec on the first pass.
- Test harness and integration tests — the `WebApplicationFactory` + isolation cases.
- First drafts of every document, freeing time for accuracy and framing.

**Where review overhead dominated (benefit net of cost was small or negative):**
- Anything touching the tenant boundary — the generated code *looked* correct and compiled, so the
  cost was entirely in the human review that found the IDOR. AI provided little safety here; it
  provided speed that had to be paid back in scrutiny.

**Net:** AI was strongly positive for breadth and boilerplate, roughly neutral for
security-critical logic (fast to write, expensive to trust), and the combination was still a clear
win *because* the guardrails forced the expensive review to happen.

## 6. Risks of AI-assisted development & mitigations

| Risk | How it showed up (or could) | Mitigation |
|------|-----------------------------|------------|
| **Plausible-but-insecure patterns** | Cross-tenant IDOR from copying a single-tenant lookup | Mandatory security review of every data path; tenant tests |
| **Hallucinated / wrong APIs** | `WebApplicationBuilder.CreateBuilder` (doesn't exist) | Build on every change; don't merge red |
| **Silent scope gaps** | No pagination, no audit trail, hard delete — none were in the prompt | Name non-functionals explicitly; adversarial review |
| **False sense of safety** | Compiles + passes happy-path tests ≠ correct | Execution-based verification; negative/isolation tests |
| **Stale model knowledge** | Older framework idioms / versions | Pin versions in instructions; verify against the SDK |
| **Over-trust of generated docs** | Invented facts or fake prompt history | Provenance labels; verify claims against the code |

## 7. Related documents

- [`SPEC.md`](SPEC.md) — the exact prompt chain, features, techniques, and corrections.
- [`PROMPTS.md`](PROMPTS.md) — narrative prompt log and human-judgment boundary.
- [`REVIEW.md`](REVIEW.md) — the review that caught the multi-tenant defects.
- [`IMPACT_ANALYSIS.md`](IMPACT_ANALYSIS.md) — downstream impact of a proposed change.
- [`PR_DESCRIPTION.md`](PR_DESCRIPTION.md) — AI disclosure and integration summary.
