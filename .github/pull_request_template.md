<!-- Fill in each section. Delete guidance comments before submitting. -->

## Summary
<!-- What does this PR do and why? 1–3 sentences. -->

## Type of change
- [ ] Feature
- [ ] Bug fix
- [ ] Refactor / restructure
- [ ] Docs
- [ ] Tests
- [ ] Chore / tooling

## Changes
<!-- Bullet the notable changes, grouped by area (feature, data, docs). -->

## Multi-tenant & security
<!-- Required for any data-access change. -->
- [ ] All new queries are scoped by `OrganizationId` (tenant), resolved from the authenticated principal — never from client input.
- [ ] No secrets or PII are logged.
- [ ] Input is validated at the controller boundary; errors return `ProblemDetails`.
- [ ] Destructive/irreversible operations are justified in the description (or avoided).

## Testing
<!-- How was this verified? Paste the result. -->
```
dotnet build
dotnet test tests/NotificationAuditService.Tests/NotificationAuditService.Tests.csproj
```
- [ ] `dotnet build` clean (0 warnings / 0 errors)
- [ ] `dotnet test` passing (state the count)
- [ ] New behaviour covered by tests (incl. cross-tenant isolation where relevant)

## Database
- [ ] EF migration added if the schema changed (note: `Migrations/` may be git-ignored — see CONTRIBUTING).
- [ ] Reference schema `src/Database/InitialCreate.sql` updated to match.

## AI tool disclosure
<!-- This project tracks AI-assisted development (see docs/). -->
- Copilot mode(s) used:
- Where AI output was accepted vs. overridden:
- Approx. AI-generated vs. hand-written:

## Checklist
- [ ] Follows `.github/copilot-instructions.md` (layering, naming, async, XML docs).
- [ ] Public methods/interfaces documented.
- [ ] Docs updated (README index + relevant `docs/*`).
- [ ] Self-reviewed the diff.

## Screenshots / evidence
<!-- Optional: Swagger, test output, etc. -->
