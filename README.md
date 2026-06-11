# MemoRecipe
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE)


MemoRecipe is a personal project that started from a concrete need: being able to import recipes from photos or scans (magazines, handwritten notes), then correct, improve, and reuse them over time.

Beyond that personal need, the goal was also to stay current with the .NET ecosystem while working on a realistic, scalable project — and to explore two topics I was particularly interested in: integrating AI into a real application, and building proper security into it from the start.

## What It Does

The system lets users manage a personal cookbook, import recipes from images via OCR and AI parsing, correct and refine AI-extracted content, and access everything across web and mobile. The key design principle is that human validation and domain rules always take precedence over AI output — the AI is a tool, not a decision-maker.

## Architecture

The project is structured as a real full-stack application organized as a monorepo. The core is an ASP.NET Core .NET 10 API following Clean Architecture (Domain, Application, Infrastructure), with PostgreSQL for persistence, JWT authentication, and a rich domain model around recipes, users, and history.

The AI layer is intentionally separated: OCR and AI parsing run as Azure Functions. Local Tesseract handles OCR, then a contract-based LLM call structures the recipe. All business logic and decisions stay in the API — the AI never becomes the source of truth. Sensitive corrections like quantities and units are handled deterministically in code, with tests.

On the frontend side, the plan is a Blazor WASM web client and a MAUI mobile app, both consuming the same API contracts.

```
MemoRecipe/
├── memoRecipe-ia/                  # Azure Functions — OCR & AI processing
├── memoRecipeAppProject/
│   └── memorecipe-api/             # ASP.NET API — domain, auth, persistence
│       └── src/
│           ├── MemoRecipe.Api
│           ├── MemoRecipe.Application
│           ├── MemoRecipe.Domain
│           └── MemoRecipe.Infrastructure
├── App/
│   └── MemoRecipe.Web              # Blazor WASM frontend
├── tests/                          # xUnit test projects (Api, Application, IA)
│   ├── MemoRecipe.Api.Tests
│   ├── MemoRecipe.Application.Tests
│   └── MemoRecipe.IA.Tests
└── documentation/
    └── DECISIONS.md                # Architectural decisions and technical debt log
```

## Technology Foundation

The API runs on ASP.NET Core .NET 10 with PostgreSQL 16 (Docker) and Entity Framework Core 10. Authentication uses JWT Bearer. Input validation relies on FluentValidation. The AI pipeline uses Azure Functions .NET 8, Tesseract for local OCR, and Mistral as the LLM provider behind an abstraction layer. The frontend is built with Blazor WASM .NET 10 and MudBlazor. Tests use xUnit with fake implementations for both the LLM and the repository layer, keeping every test deterministic and free of external dependencies.

## Running Locally

**Prerequisites:** .NET 10 SDK, .NET 8 SDK, Docker Desktop, Azure Functions Core Tools, Tesseract, and `MISTRAL_API_KEY` set as an environment variable.

```bash
# Database — first time setup
cd memoRecipeAppProject/memorecipe-api
cp .env.example .env
# Then edit .env and replace CHANGE_ME_USE_A_STRONG_PASSWORD with your own strong passwords
docker-compose up -d

# API → http://localhost:5131
cd memoRecipeAppProject/memorecipe-api
dotnet run --project src/MemoRecipe.Api

# Frontend → http://localhost:5110
cd App/MemoRecipe.Web
# First time only: copy the dev appsettings template (gitignored real file)
cp wwwroot/appsettings.Development.json.example wwwroot/appsettings.Development.json
dotnet watch

# Azure Functions → http://localhost:7071
cd memoRecipe-ia
func start

# Tests (run from the repo root)
dotnet test tests/MemoRecipe.Application.Tests/MemoRecipe.Application.Tests.csproj
dotnet test tests/MemoRecipe.Api.Tests/MemoRecipe.Api.Tests.csproj
dotnet test tests/MemoRecipe.IA.Tests/MemoRecipe.IA.Tests.csproj
```

> **Local credentials:** `.env` is gitignored (never commit real credentials). `.env.example` is a template tracked in git with `CHANGE_ME` placeholders — each contributor sets their own local values.

> **API local dev config:** the API reads from `appsettings.Development.json` (gitignored). Use the keys listed in [`.env.example`](memoRecipeAppProject/memorecipe-api/.env.example), converted from env-var format to nested JSON — e.g. `JwtSettings__Secret=...` becomes `{"JwtSettings": {"Secret": "..."}}`.

> **Frontend without API:** swap `AuthService` for `FakeAuthService` in `Program.cs` to develop the UI without running the API or Docker.

> **Auth cookies:** JWT tokens are stored in `HttpOnly` cookies (never in `localStorage`). The `CookieHandler` ensures `credentials: include` is sent on every cross-origin request.

## Current Status

### AI pipeline
- OCR (Tesseract) + LLM parsing (Mistral) + deterministic post-processing
- Clean abstraction (`IChatCompletionClient`) decouples the API from the LLM provider
- Main remaining challenge: Tesseract OCR quality on real-world images

### Backend (ASP.NET Core .NET 10)
- Clean Architecture (Api / Application / Domain / Infrastructure)
- JWT authentication stored in `HttpOnly` cookies (no `localStorage`)
- Recipe CRUD with ownership and `IsPublic` authorization rules
- FluentValidation with dedicated unit tests on each validator
- Global exception middleware (generic client response, full stack in server logs)
- Query parameters object for sorting, limiting and future pagination
- Dedicated `GET /api/recipe/count` endpoint

### Frontend (Blazor WASM .NET 10 + MudBlazor)
- Auth: Login / Register with inline validation, protected routes via custom `CookieAuthStateProvider`, JWT in HttpOnly cookies (never `localStorage`)
- Recipe workflow: list (`/recipes`, enriched cards), detail (with delete confirmation), edit (`/recipes/{id}/edit`), dashboard (`/`, count + 5 most recent)
- Scan-to-save: upload → AI extraction → preview → edit → database save
- Shared components and patterns: `RecipeForm` (scan/edit/future manual creation), `RecipeListCard` (list/dashboard), code-behind `.razor/.razor.cs`, form models decoupled from API DTOs
- Responsive: sidebar on desktop, bottom bar on mobile; save button disabled while form invalid (title length, ≥1 ingredient, ≥1 step)
- Config-driven API base URL via `wwwroot/appsettings.json` — same bundle for `dotnet watch` dev (cross-origin) and Docker compose prod (same-origin via nginx reverse proxy)

### Security
- Passwords: PBKDF2 (`PasswordHasher<T>`, 100k iterations) with rolling migration from legacy HMAC-SHA512
- HTTP headers: custom `SecurityHeadersMiddleware` adds 6 headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy` tuned for Blazor WASM, `Strict-Transport-Security` in production); Kestrel `Server` header hidden
- Rate limiting (`AddRateLimiter`): per-IP fixed window (auth 10/min, scan 5/min, global 100/min) + per-account lockout after 5 failed logins (15-min window via `IMemoryCache`); 429 responses include `Retry-After`
- CORS strict: allowed origins / headers (`Content-Type`) / methods (`GET`, `POST`, `PUT`, `DELETE`) loaded from `appsettings.json` with fail-fast startup validation
- CSRF: cookie `SameSite=Strict` + strict CORS (no dedicated CSRF token needed)
- Upload validation (defense in depth, 4 layers): Kestrel body limit, per-endpoint size attribute, server-side checks (size + extension/MIME whitelist for `.jpg`/`.jpeg`/`.png`), binary magic-bytes signature verification
- Config fail-fast at startup: API refuses to boot if required env vars (`JwtSettings:Secret`, `ConnectionStrings:DefaultConnection`, `OcrScan:BaseUrl`) are missing or still hold `CHANGE_ME` placeholders — prevents accidental prod deployments with insecure defaults
- Azure Function authorization level: `Function` (not `Anonymous`)

### Tests
- Unit tests on validators, services, and the AI pipeline (deterministic fakes for the LLM and the repository layer)
- Integration tests via `WebApplicationFactory<Program>` with SQLite in-memory as the test DB; migration to TestContainers (real PostgreSQL in a container) planned for full prod-like fidelity (cf. DECISIONS.md DEC-033)
- Targeted integration tests on the scan endpoint covering each defense layer (extension, MIME, magic bytes, golden path), with mutation testing applied to verify each test actually fails when its target layer is removed
- A `FakeOcrScanService` swapped in via DI override allows testing the golden path without calling the real Azure Function

### Tooling and maintenance
- NuGet package versions aligned across projects (EF Core, Blazor WASM); legacy `Microsoft.AspNetCore.WebUtilities 2.2.0` upgraded to a current .NET 8 release in the AI project
- Object mapping handled by [Mapperly](https://github.com/riok/mapperly) (MIT-licensed source generator) — mappings produced at compile time, zero runtime reflection, errors caught at build time

### Containerization
- **API image (~194 MB)**: built via **.NET SDK native Container Support** (no Dockerfile) — properties declared in `MemoRecipe.Api.csproj` (`<ContainerBaseImage>`, `<ContainerRepository>`, `<ContainerUser>`, `<ContainerPort>`, `<ContainerEnvironmentVariable>`). Generated with:
  ```
  dotnet publish memoRecipeAppProject/memorecipe-api/src/MemoRecipe.Api/MemoRecipe.Api.csproj --os linux --arch x64 /t:PublishContainer
  ```
  Base: `dotnet/aspnet:10.0-alpine` (non-root user `app` UID 1654 inherited). The SDK handles multi-stage build, layer caching, and image generation in one MSBuild target.
- **Frontend image (~40 MB)**: still uses a **custom Dockerfile** with `dotnet/sdk:10.0-alpine` for build and `nginx:alpine` for runtime to serve the published Blazor WASM bundle as static files (no .NET runtime required server-side). Container Support SDK does not apply because the runtime is nginx, not .NET.
- Layer caching optimized automatically by the SDK for the API; manual layer optimization in the Frontend Dockerfile (csproj copied before sources so `dotnet restore` stays cached when only code changes).
- `nginx.conf` configured with SPA routing fallback (`try_files $uri $uri/ /index.html =404`) so client-side routes work correctly on full reload (F5).
- `.env.example` documents every required env var for production deployment with `CHANGE_ME` placeholders.
- **`docker-compose.prod.yml`** orchestrates the 3 services (API + Frontend + PostgreSQL) with a custom internal bridge network, healthchecks chain (`postgres healthy → api healthy → web healthy`), resource limits, `security_opt: no-new-privileges`, and a same-origin reverse proxy pattern (Frontend nginx proxifies `/api/*` to the API container) — no CORS needed in production.
- API has a `/health` endpoint (`AddHealthChecks()`) used by the Docker compose healthcheck, and applies EF Core migrations automatically on startup (single-instance pattern; multi-instance scaling would use an init container).

## Next Steps

- HTTPS forced in production (reverse proxy + Let's Encrypt at the host edge, TLS terminating upstream and forwarding HTTP to the Docker compose Frontend on the local loopback)
- CI/CD pipeline (automated build, tests, vulnerable-package scan via `dotnet list package --vulnerable`, plus CodeQL on GitHub-hosted runners)
- Container registry distribution via GitHub Container Registry (GHCR): images built locally with the .NET SDK + pushed to GHCR + pulled from the deployment host — replacing build-on-host workflows (cf. DECISIONS.md DEC-031)
- Integration tests migration to TestContainers (real PostgreSQL in a container instead of SQLite in-memory) for full prod-like fidelity on JSONB columns and TIMESTAMPTZ precision (cf. DEC-033)
- Optional: .NET Aspire AppHost as a single-source-of-truth stack orchestrator (dev local in one click + generated docker-compose for prod, cf. DEC-032)
- AGPL §13 footer linking to source (compliance for public-facing AGPL deployment)
- GDPR compliance: account deletion with grace period, data export, legal pages, AI transparency notice
- Manual recipe creation (without scan), pagination, search and filters on the recipe list
- MAUI mobile client (consumes the same API contracts as the Blazor web client)
- Compose hardening v2: `read_only` filesystems, `cap_drop: ALL` with minimal `cap_add`, explicit non-root `user:` directive (currently deferred to avoid breaking Postgres startup with too-strict capabilities — incremental hardening)
- Automated Postgres backup to S3-compatible object storage (`pg_dump` daily, encrypted at rest, documented restore procedure)
- Observability: log rotation + centralized aggregation (Loki/Grafana), metrics scraping (Prometheus + Grafana + exporters)


## License

This project is licensed under the **GNU Affero General Public License v3.0** — see the [LICENSE](LICENSE) file for full text.
It allows the code to remain open-source for everyone while keeping the door open for future commercial dual-licensing if the project becomes a paid product.
