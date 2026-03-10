# MemoRecipe

MemoRecipe is a personal project focused on building a clean, testable, and controlled pipeline for extracting structured recipes from images, combining local OCR and AI-based parsing.

The project deliberately emphasizes:

- clear separation of responsibilities (OCR, AI, domain logic),
- deterministic and testable behavior, even when using a Large Language Model.
- long-term maintainability rather than quick AI prototyping

AI is treated as a data source, never as the source of truth.


## Project Vision

MemoRecipe allows users to:
- manage a personal cookbook,
- create and edit structured recipes,
- import recipes from photos or scans (magazines, handwritten notes),
- correct and improve AI-extracted recipes,
- adapt recipes over time (portions, corrections, variants),
- access their data across devices (web & mobile).

The system is designed so that human validation and domain rules always prevail over AI output.

## Global Architecture

The project is organized as a monorepo, composed of three clearly separated layers:

```
MemoRecipe/
├─ memoRecipe-ia/           # Azure Functions – OCR & AI processing
├─ memorecipe-api/          # ASP.NET API – domain, security, persistence
├─ App/                     # Frontends
│  ├─ MemoRecipe.Web        # Blazor Web
│  └─ MemoRecipe.Mobile     # .NET MAUI
└─ README.md
```

## Responsibilities

```
| Layer               | Responsibility                                       |
| ------------------- | ---------------------------------------------------- |
| **Frontend**        | UX, forms, navigation, user interactions             |
| **API**             | Domain logic, authentication, persistence, decisions |
| **Azure Functions** | OCR, AI parsing, deterministic preprocessing         |
```
Frontends never communicate directly with Azure Functions.


## AI & OCR Subsystem (memoRecipe-ia)

### Processing Pipeline

The OCR and AI pipeline is explicit and linear:

```
Image
  ↓
Local OCR (Tesseract)
  ↓
Raw noisy text
  ↓
Contract-based AI parsing (LLM)
  ↓
Structured DTO
  ↓
Deterministic domain post-processing
  ↓
Final RecipeDto
```

### Key principle

The LLM is never the source of truth.
It proposes a structure, but 
- all domain corrections are applied afterwards, in code.
- all transformations are deterministic
- every step is testable


### OCR

- Local OCR using Tesseract
- No cloud dependency
- Output is intentionally kept noisy
- Integration tests included


### AI Parsing
- Accessed via the IChatCompletionClient abstraction
- Current real implementation: Mistral
- Fake implementation used for deterministic tests

Strict rules enforced in the prompt:
- No ingredient invention
- No quantity invention
- No implicit normalization
- Raw JSON output only

The prompt is built as a strict contract, not a suggestion.


### Deterministic Post-Processing

Some errors are domain-specific, not AI hallucinations.

Examples:
- [15g → 115g
- l00g → 100g
- 480ml must remain 480ml

These corrections are handled by: Application/OcrQuantityNormalizer.cs

Characteristics:
- deterministic
- unit-tested
- conservative (if uncertain → no correction)

No domain logic is delegated to the LLM.


## Backend API (memorecipe-api)

The API is built with ASP.NET (.NET 10) and follows Clean Architecture:
- Domain → entities and core business concepts
- Application → services, DTOs, mappings
- Infrastructure → EF Core, PostgreSQL, migrations
- Api → controllers, authentication, configuration

Key features:
- JWT authentication
- PostgreSQL persistence (code-first)
- Swagger documentation
- Secure API-first design (Web & Mobile clients)

The API remains the single source of truth.


## Frontend (Planned)

Two frontends will consume the same API:
- Blazor Web for browser-based usage
- .NET MAUI for native mobile applications

Both share the same DTOs and contracts, ensuring consistency across platforms.

## Testing Strategy

The project is strongly test-oriented:
- unit tests for AI parsing (fake LLMs)
- unit tests for deterministic post-processing
- OCR integration tests
- no test depends on a real network call

Goal: allow changing the prompt, model, or AI provider without breaking domain behavior.


### Running Locally

Prerequisites :
- .NET 10 (API & Frontend)
- .NET 8 (Azure Functions)
- Azure Functions Core Tools
- Local Tesseract installation

Environment variable: MISTRAL_API_KEY=your_api_key

Start the Function :

```
cd memoRecipe-ia
func start
```

Example request :

```
curl.exe -X POST http://localhost:7071/api/ExtractOcrFunction -F "file=@C:\Sarah\Projects\MemoRecipe\tests\Assets\test-cheesecake.jpg"   
```

## Project Philosophy

This project is not about:
- “using AI quickly”,
- hiding domain logic inside prompts,
- blindly trusting LLM output.

It demonstrates that:
- AI can be treated as a component, not a brain,
- applications must keep control,
- AI-assisted systems can remain robust, testable, and explainable.


## Current Status

### Implemented

- Local OCR pipeline
- Contract-based AI parsing
- Deterministic post-processing
- Azure Functions integration
- Backend API with JWT auth & persistence
- Database schema & migrations
- Recipe CRUD endpoints (GET, POST, PUT, DELETE) with authorization
- Repository Pattern (IRecipeRepository, IUserRepository)
- Unit tests for RecipeService (13 tests, FakeRepository pattern)
- Input validation with FluentValidation (4 validators, 71 unit tests)
- Global error handling middleware (ExceptionMiddleware)
- Blazor Web frontend setup (Blazor WASM .NET 10 + MudBlazor)

### In Progress

- OCR data stabilization
- Linking OCR extractions to recipes
- Frontend UI development

### Planned

- MAUI mobile application
- Recipe versioning
- Advanced user feedback loops
- CI/CD pipeline
- RGPD compliance
- Advanced security (rate limiting, CORS)