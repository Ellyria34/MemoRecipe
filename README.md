# MemoRecipe

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
# Database
docker-compose up -d

# API → http://localhost:5131
cd memoRecipeAppProject/memorecipe-api
dotnet run --project src/MemoRecipe.Api

# Frontend → http://localhost:5110
cd App/MemoRecipe.Web
dotnet watch

# Azure Functions → http://localhost:7071
cd memoRecipe-ia
func start

# Tests
cd memoRecipeAppProject/memorecipe-api
dotnet test
```

> **Frontend without API:** swap `AuthService` for `FakeAuthService` in `Program.cs` to develop the UI without running the API or Docker.

> **Auth cookies:** JWT tokens are stored in `HttpOnly` cookies (never in `localStorage`). The `CookieHandler` ensures `credentials: include` is sent on every cross-origin request.

## Current Status

The pipeline architecture is in place — prompt engineering and AI parsing layer are functional. The main remaining challenge is Tesseract OCR output quality on real-world images. The full backend is done: JWT authentication via HttpOnly cookies, recipe CRUD with authorization, FluentValidation (4 validators, 71 unit tests), and global exception handling.

The Blazor frontend now covers Login/Register pages with inline validation, protected routes via CookieAuthStateProvider, responsive layout (sidebar desktop + bottom bar mobile), and a scan recipe page that uploads an image, calls the IA pipeline via the API, and displays the parsed recipe in an editable form. The user can review and modify the AI output before saving.

Next steps: saving the scanned recipe to the database, improving the form UX, stabilizing OCR quality, and eventually the MAUI mobile client and CI/CD pipeline.
