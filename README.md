# MemoRecipe
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE)

MemoRecipe is a personal project that started from a concrete need: being able to import recipes from photos or scans (magazines, handwritten notes), then correct, improve, and reuse them over time.

Beyond that personal need, the goal was also to stay current with the .NET ecosystem on a realistic, scalable project — and to explore two topics I was particularly interested in: integrating AI into a real application, and building proper security into it from the start.

## What It Does

The system lets users manage a personal cookbook, import recipes from images via OCR and AI parsing, correct AI-extracted content before saving, and access everything across web and mobile.

**Key design principle**: human validation and domain rules always take precedence over AI output — the AI is a tool, not a decision-maker.

## Architecture

Full-stack monorepo. ASP.NET Core .NET 10 API with Clean Architecture (Domain / Application / Infrastructure), PostgreSQL persistence, JWT authentication via HttpOnly cookies. The AI layer is intentionally separated as an Azure Functions project so the API never depends on a specific LLM provider. Frontend in Blazor WASM (a MAUI mobile client is planned).

```
MemoRecipe/
├── memoRecipe-ia/                  # Azure Functions — OCR + AI provider factory
├── memoRecipeAppProject/
│   └── memorecipe-api/             # ASP.NET API — Clean Architecture
│       └── src/
│           ├── MemoRecipe.Api
│           ├── MemoRecipe.Application
│           ├── MemoRecipe.Domain
│           └── MemoRecipe.Infrastructure
├── App/
│   └── MemoRecipe.Web              # Blazor WASM frontend
├── tests/                          # xUnit (Api, Application, IA)
└── documentation/
    ├── DECISIONS.md                # Architectural decisions log
    └── DEPLOYMENT.md               # Build, push, deploy, rollback runbook
```

## Technology Foundation

ASP.NET Core .NET 10 · PostgreSQL 16 · EF Core 10 · JWT Bearer in HttpOnly cookies · FluentValidation · MudBlazor · Blazor WASM .NET 10 · Azure Functions .NET 8 · Tesseract (local OCR) · Multi-provider LLM factory (Mistral, Google Gemini, Groq) · xUnit + TestContainers.

## Running Locally

**Prerequisites**: .NET 10 SDK, .NET 8 SDK, Docker Desktop, Azure Functions Core Tools v4, Tesseract installed locally, and an API key for the configured LLM provider.

```bash
# 1. Database (Docker)
cd memoRecipeAppProject/memorecipe-api
cp .env.example .env   # then replace CHANGE_ME placeholders
docker compose up -d

# 2. Azure Functions (AI pipeline)
cd memoRecipe-ia
func start              # listens on http://localhost:7071

# 3. API
cd memoRecipeAppProject/memorecipe-api
dotnet run --project src/MemoRecipe.Api   # listens on http://localhost:5131

# 4. Frontend
cd App/MemoRecipe.Web
cp wwwroot/appsettings.Development.json.example wwwroot/appsettings.Development.json
dotnet watch            # listens on https://localhost:5XXX

# Tests
dotnet test tests/MemoRecipe.Application.Tests
dotnet test tests/MemoRecipe.Api.Tests
dotnet test tests/MemoRecipe.IA.Tests
```

> `.env` and `appsettings.Development.json` are gitignored — local credentials never reach the repo. Each contributor sets their own values from the `.example` templates.

For production deployment (build, push, rollback procedures) see [`documentation/DEPLOYMENT.md`](documentation/DEPLOYMENT.md).

## Current Status

| Area | Status |
|---|---|
| **AI pipeline** | OCR Tesseract + multi-provider LLM factory (Mistral / Gemini / Groq, swappable via env var) + deterministic post-processing |
| **Backend** | Clean Architecture, recipe CRUD with ownership rules, FluentValidation, global exception middleware, healthcheck endpoint |
| **Frontend** | Auth (Login / Register), recipe workflow (scan, manual create, list, detail, edit), adaptive nav (sidebar desktop + bottom bar mobile), shared `RecipeForm` component |
| **Security** | PBKDF2 password hashing, custom security headers middleware (CSP, HSTS, etc.), per-IP + per-account rate limiting, strict CORS, defense-in-depth upload validation, fail-fast config validation at startup |
| **RGPD / EU AI Act** | Privacy policy + legal mentions pages, consent on registration, AI transparency notice on scan page, hosting in Switzerland (adequacy decision) |
| **Tests** | Unit tests on validators / services / AI pipeline (deterministic fakes); integration tests via `WebApplicationFactory<Program>` with TestContainers (real PostgreSQL) |
| **Containerization** | API image built via .NET SDK Container Support (no Dockerfile, ~194 MB Alpine); Frontend image with custom nginx Dockerfile (~40 MB); orchestration via `docker-compose.prod.yml`; images published on GitHub Container Registry — see [`DEPLOYMENT.md`](documentation/DEPLOYMENT.md) |

For the rationale behind these choices (alternatives considered, trade-offs), see [`documentation/DECISIONS.md`](documentation/DECISIONS.md).

## Roadmap

- HTTPS forced in production (reverse proxy + Let's Encrypt)
- CI/CD pipeline (GitHub Actions: build, test, vulnerable-package scan, CodeQL)
- VPS deployment (Infomaniak Cloud, Apache reverse proxy)
- GDPR self-service flows (account deletion, data export, profile management)
- Bring-Your-Own-Key for AI providers (multi-provider, encrypted at rest)
- MAUI mobile client consuming the same API

## License

This project is licensed under the **GNU Affero General Public License v3.0** — see [`LICENSE`](LICENSE) for full text.

The AGPL was chosen to keep the code open-source for everyone while keeping the door open for future commercial dual-licensing if the project becomes a paid product.
