# Prompt Engineering Documentation

Per-prompt breakdown of the engineering chain: the request, the Copilot **mode** (Ask / Edit / Agent),
other Copilot features used, the prompting **technique**, and the rationale. Complements
[`SPEC.md`](SPEC.md) (chain + post-generation corrections) and [`TOOL_STRATEGY.md`](TOOL_STRATEGY.md).

> **Provenance:** prompt text is the verbatim/summarised request from the working session. "Mode" and
> "features" map each request onto the Copilot surface a Copilot-based workflow would use.

| Prompt # | Prompt Text (summarised) | Copilot Mode | Other Features | Prompting Technique | Why This Approach? |
|---|---|---|---|---|---|
| 1 | "Understand the code and arrange the folder in [target `src/` tree]" | **Agent** | `@workspace` | Specificity (exact target tree) | Reorg needs whole-repo awareness + a precise destination so the model plans `git mv`s/namespaces instead of guessing; Agent executes the moves and builds. |
| 2 | "Generate a Project model and a Project service with create, update-status, get-by-team, delete. Use a database." | **Edit** | `#file` (sibling services), custom instructions | Decomposition (model + 4 named ops) | Enumerating operations yields a complete, on-spec slice in one multi-file pass; the instructions file supplies unstated conventions. |
| 3 | "Create a REVIEW.md file for project service" | **Ask** | `#file` | Role-based (act as reviewer) | An open first pass to surface what the model notices before constraining it. |
| 4 | "Review… every issue: severity, multi-tenant impact, detection, fix, + 'Issues Copilot Introduced' section" | **Ask** | `@workspace`, `#file` | Iterative refinement + Constraint (fixed rubric) | The rubric + multi-tenant lens forced deeper analysis — this is what uncovered the cross-tenant IDOR. |
| 5 | "Rewrite the Project Service to production standards: layering, ORM, validation, typed contracts, error handling, logging, multi-tenant isolation, docs" | **Agent** | `@workspace` | Constraint (explicit NFR checklist) | Naming each non-functional requirement is far more reliable than "make it production-ready"; each item becomes a verifiable artifact, and Agent builds/tests it. |
| 6 | "add it under tests/" | **Agent** | `/tests`, `#file` | Few-shot (test examples in `copilot-instructions.md`) | Worked exemplars steer test shape (AAA, naming); a terse follow-up suffices, and Agent runs the suite to prove isolation. |
| 7 | "Run the app" | **Agent** | terminal / preview browser | Iterative refinement (verify loop) | Static generation isn't proof; running confirmed startup, migrations, live 201/401, and Swagger. |
| 8 | "Create IMPACT_ANALYSIS.md: files affected + change nature; IP-capture risks; sequencing" | **Ask** | `@workspace` | Constraint + Specificity (3 mandated sections) | Arrived without details first → asked rather than invented; then fixed sections produced a grounded, file-referenced analysis. |
| 9 | "Create PROMPTS.md documenting how Copilot built the service" | **Ask** | `@workspace` | Role-based (author) | Captures the workflow and the human-judgment boundary honestly. |
| 10 | "Create SPEC.md: prompt chain, feature+technique per prompt, ≥2 features/≥3 techniques, corrections" | **Ask** | `@workspace` | Constraint (mandated contents + minimums) | Meta-prompt whose own constraints are satisfied by a coverage matrix + corrections log. |
| 11 | "Create TOOL_STRATEGY.md" | **Ask** | `@workspace` | Role-based + Constraint | Produces the accept-vs-override heuristics and guardrails as a reusable strategy doc. |
| 12 | "Rewrite the Transaction module to production standards (layering, ORM, validation, error handling, logging, authorisation)" | **Agent** | `@workspace`, `#file` | Analogical (pattern transfer) + Constraint | Reuse the *proven* Project feature pattern for a fintech module; Agent's edit→build→migrate→test loop caught a compile error and proved tenant isolation. |
| 13 | "Build the Expense Splitting feature: Shared Expense Model, Balance Calculation Service, API Endpoints, Tests (≥6)" | **Agent** | `@workspace`, `/tests` | Decomposition (4 named deliverables) + Constraint | Split the vague domain into concrete artifacts slotted into the layered pattern; constraint-driven correctness (cents math, single-currency balances). |
| 14 | "Create a test case and execute it for expense module" | **Agent** | `/tests` | Boundary-case selection + Iterative refinement | Added an *uncovered* multi-expense netting case (a user nets to zero and drops from settlements), then executed it to confirm. |

## Techniques used across the chain

| Technique | Prompts | Essence |
|---|---|---|
| Specificity | 1, 8 | Give exact targets/sections to remove ambiguity. |
| Decomposition | 2, 13 | Break a feature/domain into named parts → complete coverage. |
| Role-based | 3, 9, 11 | Frame the model as reviewer/author. |
| Iterative refinement | 4, 7, 14 | Reshape prior output; verify by execution. |
| Constraint | 4, 5, 8, 10, 12, 13 | Enumerate non-functionals/required contents explicitly. |
| Few-shot | 6 | Lean on worked examples in the instructions file. |
| Analogical (pattern transfer) | 12 | Reapply a proven feature pattern to a new module. |

## Mode usage summary

- **Ask** — diagnosis, review, and prose docs (prompts 3, 4, 8–11). Best where the output is judgment/analysis.
- **Edit** — coordinated multi-file code generation (prompt 2). Best for a clean, spec-driven slice.
- **Agent** — anything needing an edit→build→migrate→test→run loop (prompts 1, 5–7, 12–14). **Most useful for
  remediation**, because fixes to security boundaries must be *executed and observed*, not just written.
