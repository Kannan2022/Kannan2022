# Feature Usage Log

How GitHub Copilot was used across this assessment — the feature chosen at each point, *why that
feature rather than another*, and the outcome. Complements [`SPEC.md`](SPEC.md),
[`PROMPT_ENGINEERING.md`](PROMPT_ENGINEERING.md), and [`TOOL_STRATEGY.md`](TOOL_STRATEGY.md).

> **Provenance:** entries map the working session onto the Copilot surface each step corresponds to
> in a Copilot-based workflow. Seven entries covering six distinct features.

| # | Where in the Case Study | Copilot Feature Used | Why This Feature (Not Another) | What Happened |
|---|---|---|---|---|
| 1 | Project setup — establishing standards before any feature was generated | **Custom instructions** (`.github/copilot-instructions.md`) | It's *ambient and persistent* — configured once, every later prompt inherits it. Per-prompt Chat would mean re-stating stack/layering/security each time. | Short prompts ("generate a Project service") produced on-spec layered code — async, constructor DI, XML docs, EF patterns — without restating conventions. Compounding payoff across the whole session. |
| 2 | "Understand the code and arrange the folder…" — planning the restructure | **Copilot Chat + `@workspace`** | The task needs *whole-repo* awareness to plan `git mv`s and namespace changes; `#file` is too narrow and Edits would be premature before a plan exists. | Produced the move plan and the files/namespaces to touch; the follow-up build surfaced a latent `WebApplicationBuilder.CreateBuilder` bug. |
| 3 | Generating the Project feature (model + service + DbContext + DI) | **Copilot Edits** (multi-file) | I needed coordinated *changes applied across several files at once*; Ask only advises, it doesn't edit. | A complete, house-style slice generated in one pass — which later review then exposed as carrying a cross-tenant IDOR. |
| 4 | Reviewing the Project service with a structured, multi-tenant rubric | **Copilot Chat / Ask** (review, constraint-framed) | Review is a *judgment/analysis* task, not a code mutation — Ask is the reasoning surface; Edit/Agent would jump to changing code before understanding the risk. | The multi-tenant lens surfaced the cross-tenant IDOR (`id`-only lookups) that build + happy-path tests all passed over. |
| 5 | Re-targeting the Transaction rewrite at the proven pattern | **`#file`** context (point at `ProjectService`/repository) | To *transfer a specific known-good pattern*, focusing the model on the exact sibling file beats `@workspace`, which dilutes with unrelated context. | The Transaction module built `0/0` on the first try by reapplying the Project feature's layered, tenant-scoped shape. |
| 6 | Adding the Project/Transaction/Expense test suites | **`/tests`** slash command | Purpose-built for *test scaffolding*, seeded by the AAA/naming examples already in the instructions; describing tests in prose via Ask is slower and less consistent. | Generated xUnit integration tests in the house style (incl. cross-tenant isolation); needed one manual fix — a missing `using Microsoft.AspNetCore.Http;`. |
| 7 | The Transaction remediation + Expense feature, end-to-end | **Copilot Agent mode** | Remediation/build-out is an *edit → build → migrate → test → run* loop; only Agent can execute the toolchain (EF CLI, `dotnet test`, the app) and observe results. Ask/Edit can't close that loop. | Applied multi-file changes, ran the build (caught a compile error), generated/applied migrations, ran tests to *prove* tenant isolation (29/29), and ran the app to verify live `201`/`401` + Swagger. |

## Takeaway on feature selection

The split follows *what the task produces* — Ask for judgment (2, 4), Edits/`#file` for coordinated
code generation (3, 5), `/tests` for test scaffolding (6), and Agent for anything that must be
executed and observed (7), all resting on the standing custom instructions (1). Agent was the most
valuable for remediation because security fixes have to be *proven*, not just written.
