# ============================================================================
# run-e2e-local.ps1 — Orchestrates the E2E test cycle locally (Windows/PS 5.1+)
#
# Steps:
#   1) Builds the API container image via .NET Container Support
#   2) Starts the full E2E Docker Compose stack (postgres + ia + api + web)
#   3) Waits for the web service to become healthy
#   4) Runs the Playwright E2E tests
#   5) Tears down the stack (guaranteed via try/finally block)
#
# Usage (from repo root):
#   .\scripts\run-e2e-local.ps1
#
# Exit code: 0 if all tests passed, non-zero otherwise (bubbled from dotnet test)
# ============================================================================

$ErrorActionPreference = 'Stop'

$composeFile      = 'docker-compose.e2e.yml'
$apiCsprojDir     = 'memoRecipeAppProject/memorecipe-api/src/MemoRecipe.Api'
$e2eTestProject   = 'tests/MemoRecipe.Web.E2E.Tests'
$imageTag         = 'e2e-local'
$maxWaitSeconds   = 120
$webHealthUrl     = 'http://localhost:8080/'
$testExitCode     = 1  # Default to failure; overwritten on test success

try {
    # ----- Step 1: Build the API container image -----
    Write-Host "==> [1/5] Building API container image (memorecipe-api:$imageTag)..." -ForegroundColor Cyan
    dotnet publish $apiCsprojDir `
        --os linux `
        --arch x64 `
        /t:PublishContainer `
        /p:ContainerImageTag=$imageTag `
        /p:ContainerRegistry=
    if ($LASTEXITCODE -ne 0) { throw "API image build failed (dotnet publish exit code $LASTEXITCODE)" }

    # ----- Step 2: Start the E2E stack -----
    Write-Host "==> [2/5] Starting E2E stack (postgres + ia + api + web)..." -ForegroundColor Cyan
    docker compose -f $composeFile up --build -d
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed (exit code $LASTEXITCODE)" }

    # ----- Step 3: Wait for the web service to become healthy -----
    Write-Host "==> [3/5] Waiting for stack to become healthy (max ${maxWaitSeconds}s)..." -ForegroundColor Cyan
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $healthy = $false
    while ($stopwatch.Elapsed.TotalSeconds -lt $maxWaitSeconds) {
        try {
            $response = Invoke-WebRequest -Uri $webHealthUrl -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                $healthy = $true
                break
            }
        } catch {
            # Web not ready yet — keep polling
        }
        Start-Sleep -Seconds 3
        Write-Host "    Still waiting... ($([int]$stopwatch.Elapsed.TotalSeconds)s elapsed)"
    }
    if (-not $healthy) { throw "Stack did not become healthy within ${maxWaitSeconds}s — check 'docker compose logs'" }
    Write-Host "    Stack healthy in $([int]$stopwatch.Elapsed.TotalSeconds)s" -ForegroundColor Green

    # ----- Step 4: Run Playwright E2E tests -----
    Write-Host "==> [4/5] Running Playwright E2E tests..." -ForegroundColor Cyan
    dotnet test $e2eTestProject
    $testExitCode = $LASTEXITCODE
}
finally {
    # ----- Step 5: Teardown (guaranteed even on failure) -----
    Write-Host "==> [5/5] Tearing down E2E stack..." -ForegroundColor Cyan
    docker compose -f $composeFile down -v
}

if ($testExitCode -ne 0) {
    Write-Host "E2E tests FAILED (exit code $testExitCode)" -ForegroundColor Red
    exit $testExitCode
}
Write-Host "E2E tests PASSED" -ForegroundColor Green
exit 0