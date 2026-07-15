# Tests

Automated tests for the Notification & Audit Service.

## Projects

### `NotificationAuditService.Tests`

Integration tests for the Projects feature, run against the real API bootstrapped in-memory with
[`WebApplicationFactory<Program>`](https://learn.microsoft.com/aspnet/core/test/integration-tests).

- **`TestInfrastructure/ProjectApiFactory.cs`** — hosts the app on an isolated, per-run SQLite
  database and injects a tenant identity from an `X-Test-Org` header (standing in for the
  authentication middleware that populates the `org_id` claim in production).
- **`Projects/ProjectsIntegrationTests.cs`** — proves **multi-tenant isolation** (one organisation
  cannot read, update, or delete another's projects — cross-tenant writes return `404`), plus the
  happy paths, `401` fail-closed behaviour when no organisation is present, and request validation.

Tests follow the repo convention `MethodName_Condition_ExpectedResult` and the Arrange-Act-Assert
pattern from [`.github/copilot-instructions.md`](../.github/copilot-instructions.md).

## Running

```bash
# from the repository root
dotnet test tests/NotificationAuditService.Tests/NotificationAuditService.Tests.csproj
```

The main application project (`NotificationAuditService.csproj`) excludes `tests/**` from its own
build, so the test project is compiled and run independently.

## Adding unit tests

For fast, isolated unit tests (e.g. of `ProjectService` with a mocked `IProjectRepository`), add
xUnit + Moq test classes alongside the integration tests, mirroring the `src/` feature folders.
