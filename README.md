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
├── memoRecipe-ia/              # Azure Functions — OCR & AI processing
├── memoRecipeAppProject/
│   └── memorecipe-api/         # ASP.NET API — domain, auth, persistence
│       ├── src/
│       │   ├── MemoRecipe.Api
│       │   ├── MemoRecipe.Application
│       │   ├── MemoRecipe.Domain
│       │   └── MemoRecipe.Infrastructure
│       └── tests/
├── App/
│   └── MemoRecipe.Web          # Blazor WASM frontend
├── documentation/
│   └── DECISIONS.md            # Architectural decisions and technical debt log
└── journal/                    # Development log
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

# Tests
cd memoRecipeAppProject/memorecipe-api
dotnet test
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
- Login / Register pages with inline validation
- Protected routes via custom `CookieAuthStateProvider`
- Responsive layout (sidebar on desktop, bottom bar on mobile)
- Full scan-to-save workflow: upload → AI extraction → preview → edit → database save
- Reusable `RecipeForm` component shared between scan, edit, and future manual creation
- Form models (`RecipeFormModel`, `IngredientFormModel`, `StepFormModel`) decoupled from API DTOs
- Recipe list page (`/recipes`) sorted by creation date (most recent first), enriched cards (title, total time, French difficulty labels, servings with singular/plural)
- Reusable `RecipeListCard` component shared between recipe list and dashboard
- Recipe detail page with delete confirmation dialog
- Recipe edit page (`/recipes/{id}/edit`) reusing the shared form
- Dashboard (`/`) with recipe count and 5 most recent recipes
- Code-behind pattern everywhere (`.razor` / `.razor.cs` separation)
- Save button automatically disabled while the form is invalid (title length, at least one ingredient, at least one step)
- API base URL is config-driven via `wwwroot/appsettings.json` (with Development override) — same bundle works in `dotnet watch` mode (cross-origin) and in Docker compose prod (same-origin via nginx reverse proxy)

### Security
- Password hashing migrated from HMAC-SHA512 to PBKDF2 (`PasswordHasher<T>`, 100k iterations), with rolling migration for existing users
- Azure Function authorization level set to `Function` (not `Anonymous`)
- Custom `SecurityHeadersMiddleware` adds 6 security headers: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy` (tuned for Blazor WASM + MudBlazor), `Strict-Transport-Security` (production only)
- Kestrel configured to hide the `Server` header (no underlying web server exposed)
- Rate limiting (built-in `AddRateLimiter`): per-IP fixed window on auth (10/min), scan (5/min), and global (100/min); per-account lockout after 5 consecutive failed logins (15-minute window via `IMemoryCache`); 429 responses include `Retry-After`
- CORS strictly configured: allowed origins loaded from `appsettings.json` with fail-fast startup validation; explicit whitelist of headers (`Content-Type`) and methods (`GET`, `POST`, `PUT`, `DELETE`)
- CSRF protection via cookie `SameSite=Strict` combined with strict CORS (no dedicated CSRF token needed in this configuration)
- File upload validation on the scan endpoint with **defense in depth across four layers**: global Kestrel request body limit, per-endpoint size attributes, server-side checks (size, extension whitelist `.jpg`/`.jpeg`/`.png`, MIME type whitelist), and binary signature verification (magic bytes for JPEG, PNG)
- Fail-fast configuration validation at startup: the API refuses to boot if required env vars (`JwtSettings:Secret`, `ConnectionStrings:DefaultConnection`, `OcrScan:BaseUrl`) are missing or still hold `CHANGE_ME` placeholders — prevents accidental production deployments with insecure dev defaults

### Tests
- Unit tests on validators, services, and the AI pipeline (deterministic fakes for the LLM and the repository layer)
- Integration tests using `WebApplicationFactory<Program>` with SQLite in-memory replacing PostgreSQL (no Docker required for CI)
- Targeted integration tests on the scan endpoint covering each defense layer (extension, MIME, magic bytes, golden path), with mutation testing applied to verify each test actually fails when its target layer is removed
- A `FakeOcrScanService` swapped in via DI override allows testing the golden path without calling the real Azure Function

### Tooling and maintenance
- NuGet package versions aligned across projects (EF Core, Blazor WASM); legacy `Microsoft.AspNetCore.WebUtilities 2.2.0` upgraded to a current .NET 8 release in the AI project
- Object mapping handled by [Mapperly](https://github.com/riok/mapperly) (MIT-licensed source generator) — mappings produced at compile time, zero runtime reflection, errors caught at build time

### Containerization
- Multi-stage Docker images:
  - **API** (~150 MB): `dotnet/sdk:10.0-alpine` for build, `dotnet/aspnet:10.0-alpine` for runtime (SDK stripped from the final image)
  - **Frontend** (~40 MB): `dotnet/sdk:10.0-alpine` for build, `nginx:alpine` to serve the published Blazor WASM bundle as static files (no .NET runtime required server-side)
- Layer caching optimized: csproj files copied before sources so `dotnet restore` stays cached when only code changes
- `nginx.conf` configured with SPA routing fallback (`try_files $uri $uri/ /index.html =404`) so client-side routes work correctly on full reload (F5)
- `.dockerignore` excludes build artifacts, IDE state, secrets, and personal docs to keep the build context lean and avoid shipping sensitive files
- `.env.example` documents every required env var for production deployment with `CHANGE_ME` placeholders

## Next Steps

- Production orchestration via `docker-compose.prod.yml` wiring the API, frontend, and PostgreSQL services together (API + frontend Docker images already in place)
- HTTPS forced in production (verified behavior behind a reverse proxy)
- CI/CD pipeline (automated build, tests, vulnerable-package scan via `dotnet list package --vulnerable`)
- AGPL §13 footer linking to source (compliance for public-facing AGPL deployment)
- GDPR compliance: account deletion with grace period, data export, legal pages, AI transparency notice
- Manual recipe creation (without scan), pagination, search and filters on the recipe list
- MAUI mobile client (consumes the same API contracts as the Blazor web client)


## License

This project is licensed under the **GNU Affero General Public License v3.0** — see the [LICENSE](LICENSE) file for full text.
It allows the code to remain open-source for everyone while keeping the door open for future commercial dual-licensing if the project becomes a paid product.
