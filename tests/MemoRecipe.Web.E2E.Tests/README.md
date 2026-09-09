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
on every push to `main` and every pull request.

## IA image: official Functions runtime

The E2E stack builds the IA Function from `infra/ia-e2e/Dockerfile`, which uses
the official `mcr.microsoft.com/azure-functions/dotnet-isolated` runtime image
rather than the Azure Functions Core Tools (`func start`) used by
`infra/ia-dev/Dockerfile`.

Core Tools binds the Functions host to `127.0.0.1` only, which is unreachable
from the `api` container on the Docker network. This is invisible on Docker
Desktop but deterministic on Linux runners.

Beyond fixing that, the official runtime is what actually runs the Function in
production, while `func start` is a development tool that is never deployed. The
E2E stack therefore exercises the same runtime as production.

Function keys are provided through the file-based secret store
(`AzureWebJobsSecretStorageType=files`), seeded from `infra/ia-e2e/` with
throwaway values. Unlike Core Tools, the official runtime enforces
`AuthorizationLevel.Function`, so the E2E run now covers the authentication path
as well.

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
