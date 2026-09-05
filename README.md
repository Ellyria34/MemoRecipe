# MemoRecipe
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE)
[![Latest tag](https://img.shields.io/github/v/tag/Ellyria34/MemoRecipe?label=latest%20tag)](https://github.com/Ellyria34/MemoRecipe/tags)
[![CI](https://github.com/Ellyria34/MemoRecipe/actions/workflows/ci.yml/badge.svg)](https://github.com/Ellyria34/MemoRecipe/actions/workflows/ci.yml)
[![CodeQL](https://github.com/Ellyria34/MemoRecipe/actions/workflows/codeql.yml/badge.svg)](https://github.com/Ellyria34/MemoRecipe/actions/workflows/codeql.yml)
[![E2E Tests](https://github.com/Ellyria34/MemoRecipe/actions/workflows/ci.yml/badge.svg?job=e2e-tests)](https://github.com/Ellyria34/MemoRecipe/actions/workflows/ci.yml)


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
    ├── DECISIONS.md                # Thematic technical decisions (per topic, reader-friendly)
    ├── ADR.md                      # Architectural Decision Records (chronological, detailed)
    └── DEPLOYMENT.md               # Build, push, deploy, rollback runbook
```

## Technology Foundation

ASP.NET Core .NET 10 · PostgreSQL 16 · EF Core 10 · JWT Bearer in HttpOnly cookies · FluentValidation · MudBlazor · Blazor WASM .NET 10 · Azure Functions .NET 8 · Multi-provider LLM factory with multimodal Vision (Mistral / Google Gemini / Groq — text and Vision variants selectable via env var) · Tesseract (local OCR fallback) · xUnit + TestContainers.

## Running Locally

**Prerequisites**: .NET 10 SDK, .NET 8 SDK, Docker Desktop, Azure Functions Core Tools v4, Tesseract installed locally, and an API key for the configured LLM provider.

```bash
# 1. Database (Docker)
cp .env.example .env   # then replace CHANGE_ME placeholders (from repo root)
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

> **Latest release** : `v1.0.0-alpha.2` (August 21, 2026) — first tagged release, container images published to GHCR, pre-beta feature-complete.

| Area | Status |
|---|---|
| **AI pipeline** | Two interchangeable paths behind a Strategy-pattern factory: direct multimodal Vision LLM (Mistral Vision — EU-hosted, GDPR-native — by default) or OCR Tesseract + text-only LLM (Groq / Mistral / Gemini) as fallback; swappable via a single env var; deterministic post-processing on top; structured ingredient extraction (name + quantity + unit propagated end-to-end); per-call token usage tracked cross-project for cost observability |
| **Backend** | Clean Architecture, recipe CRUD with ownership rules, FluentValidation, global exception middleware, healthcheck endpoint |
| **Frontend** | Auth (Login / Register), recipe workflow (scan, manual create, list, detail, edit), adaptive nav (sidebar desktop + bottom bar mobile), shared `RecipeForm` component |
| **Security** | PBKDF2 password hashing, custom security headers middleware (CSP, HSTS, etc.), per-IP + per-account rate limiting, strict CORS, defense-in-depth upload validation, fail-fast config validation at startup, LLM defense-in-depth (OWASP LLM01 prompt injection sanitizer with regex catalog + sealed delimiters + 4-tier LLM-level rate limiter with 429 + Retry-After + structured audit trail without PII) |
| **RGPD / EU AI Act** | Privacy policy + legal mentions pages, consent on registration, AI transparency notice on scan page, hosting in Switzerland (adequacy decision), Right to erasure (Art. 17) with 30-day grace period + cascade purge |
| **Tests** | Unit tests on validators / services / AI pipeline (deterministic fakes); integration tests via `WebApplicationFactory<Program>` with TestContainers (real PostgreSQL) |
| **CI/CD** | GitHub Actions: build + tests on push and PR (API + IA + Web), vulnerable-package scan (fail-fast on High/Critical), CodeQL SAST (C# + workflows), Lighthouse a11y/perf audit; container images pushed to GHCR on version tags |
| **Observability** | Structured logging via Serilog (no PII); structured LLM audit trail (userId + provider + tokens usage + duration + input hash — GDPR Art. 5.1.c compliant); Telegram alerting channel on critical operations (backup failures, unhandled exceptions, cost thresholds, login fail storm detection) |
| **Containerization** | API image built via .NET SDK Container Support (no Dockerfile, ~194 MB Alpine); Frontend image with custom nginx Dockerfile (~40 MB); orchestration via `docker-compose.prod.yml`; images published on GitHub Container Registry — see [`DEPLOYMENT.md`](documentation/DEPLOYMENT.md) |
| **Backup & DR** | Daily encrypted PostgreSQL backup (`pg_dump` + GPG asymmetric encryption; public key in the container, private key kept off-server); local retention 30 days; full restore procedure documented and end-to-end tested. Off-site copy (S3-compatible or SFTP storage service, per 3-2-1 rule) is planned in part 2 before public production launch. See [DEC-038](documentation/ADR.md#dec-038) and [`DEPLOYMENT.md`](documentation/DEPLOYMENT.md) |

For a thematic overview of the technical choices, see [`documentation/DECISIONS.md`](documentation/DECISIONS.md). For the full chronological log with alternatives considered, trade-offs and sources, see [`documentation/ADR.md`](documentation/ADR.md).

## Roadmap

- HTTPS forced in production (reverse proxy + Let's Encrypt)
- VPS deployment (reverse proxy + Docker Compose orchestration)
- Off-site encrypted backup copy (S3-compatible or SFTP storage, part 2 of the backup pipeline)
- GDPR self-service flows (data export, profile management)
- Bring-Your-Own-Key for AI providers (multi-provider, encrypted at rest)
- MAUI mobile client consuming the same API

## License

This project is licensed under the **GNU Affero General Public License v3.0** — see [`LICENSE`](LICENSE) for full text.

The AGPL was chosen to keep the code open-source for everyone while keeping the door open for future commercial dual-licensing if the project becomes a paid product.