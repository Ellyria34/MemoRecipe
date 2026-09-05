# MemoRecipe.Web.E2E.Tests

End-to-end browser tests for the MemoRecipe Blazor WASM application, using
[Playwright for .NET](https://playwright.dev/dotnet/) + xUnit.

## What is covered

Six scenarios exercising the full stack (browser → Web → API → PostgreSQL, with
the IA Function in Fake mode):

| # | Test | Description |
|---|---|---|
| 1 | `SmokeTest` | Homepage loads and shows the expected title |
| 2 | `AuthTests.Auth_RegisterLoginLogoutRelogin_AllStepsSucceed` | Register → login → logout → re-login |
| 3 | `AuthTests.Auth_LoginWithWrongPassword_ShowsErrorAlert` | Wrong password triggers alert |
| 4 | `RecipeCreateTests.Recipe_CreateEditVerify_WorksEndToEnd` | Manual create + edit + verify |
| 5 | `RecipeScanTests.Recipe_ScanUploadAndSave_UsesFakeIaAndPersists` | Upload JPEG → Fake IA pipeline → save (skipped in CI, see below) |
| 6 | `RecipeDeleteTests.Recipe_DeleteWithConfirmation_RemovesFromList` | Detail → delete + confirm → verify absence |

## Prerequisites

- **Docker Desktop** (or Docker Engine on Linux) running
- **.NET SDK 10** installed
- **PowerShell 7+** (or Windows PowerShell 5.1) to run the orchestration script

## Quick start

From the repository root:

```powershell
.\scripts\run-e2e-local.ps1
```

The script:
1. Builds the API container image (`dotnet publish /t:PublishContainer`)
2. Starts the E2E Docker stack (postgres + ia + api + web) via `docker-compose.e2e.yml`
3. Waits for the stack to become healthy
4. Runs `dotnet test` on this project
5. Tears down the stack (`down -v`) — guaranteed via `try/finally`

Expected: **6/6 tests green in around 40 seconds** on a warm Docker cache.

## Architecture

### Page Object Pattern

UI interactions are encapsulated in classes under `Pages/` (`LoginPage`,
`RegisterPage`, `HomePage`, `CreateRecipePage`, `RecipeListPage`,
`RecipeDetailPage`, `EditRecipePage`, `ScanRecipePage`). Each page exposes:
- **Locators** using semantic Playwright methods (`GetByRole`, `GetByLabel`)
  rather than fragile CSS selectors
- **Business actions** (`FillAndSubmitAsync`, `CreateWithMinimalDataAsync`, ...)

### TestUserHelper

`Helpers/TestUserHelper.CreateUserViaHttpAsync(email, userName, password)`
provisions test users via `POST /api/auth/register` to avoid duplicating the UI
register flow across every scenario.

### Fake IA pipeline

The E2E stack runs the IA Function with `AI_PROVIDER=Fake`. This uses
`FakeRecipePipeline` in `memoRecipe-ia/Application/Pipeline/` which returns a
hardcoded `RecipeDto` (Cheesecake) and skips OCR + LLM entirely — avoiding the
native `libleptonica-1.82.0.so` dependency required by Tesseract.

### Sequential test execution

`xunit.runner.json` disables both `parallelizeAssembly` and
`parallelizeTestCollections`. E2E tests share the same database, rate limiter,
and Function instance — running them in parallel causes non-deterministic
failures.

## CI

Tests run on GitHub Actions via the `e2e-tests` job in `.github/workflows/ci.yml`
on every push to `main` and every pull request. The scan test is skipped in CI
via `[Trait("Category", "SkipCI")]` and the `--filter "Category!=SkipCI"`
argument on `dotnet test`.

## Why is the scan test skipped in CI?

The scan test (`Recipe_ScanUploadAndSave_UsesFakeIaAndPersists`) passes reliably
in local Docker Desktop but fails deterministically on `ubuntu-latest` runners
with the API receiving `Connection refused (ia:7071)` when calling the Function.
Seven mitigation attempts (extended healthchecks, warmup loops, `dotnet run` vs
`func start`, dummy `AzureWebJobsStorage`, prebuilt Docker image, IPv4 binding
force) did not resolve the difference between Docker Desktop and Docker native
on Linux.

Root cause is not identified. The test is skipped in CI to unblock the rest of
the suite; **the local run remains authoritative** (`.\scripts\run-e2e-local.ps1`
runs all 6/6). Since the production IA Function runs on Azure Container Apps
(not `func start` in a Docker container), this CI failure does not indicate a
production issue.

Follow-up work is tracked internally.

## Adding a new scenario

1. Add a Page Object under `Pages/` if the scenario touches a new page
2. Add a test class or test method in an existing class
3. Use unique per-test data with `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`
   to avoid collisions between scenarios
4. Run locally with `.\scripts\run-e2e-local.ps1` before pushing

## Troubleshooting

**"Docker daemon not running"**: start Docker Desktop and wait for it to
become fully ready.

**Tests fail on first run after clone**: run `dotnet build` first to trigger
Playwright browser install via `playwright.ps1 install --with-deps chromium`.

**Local run flaky**: check container health with `docker compose -f docker-compose.e2e.yml -p memorecipe_e2e ps` and inspect logs with `docker compose -f docker-compose.e2e.yml -p memorecipe_e2e logs <service>`.

**Playwright browser missing**: run `pwsh bin/Debug/net10.0/playwright.ps1 install chromium --with-deps`.
