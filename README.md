# MemoRecipe

MemoRecipe is a personal project focused on building a clean, testable, and controlled pipeline for extracting structured recipes from images, combining local OCR and AI-based parsing.

The project deliberately emphasizes:
- application-controlled decision making (the app decides, not the LLM),
- clear separation of responsibilities (OCR, AI, domain logic),
- deterministic and testable behavior, even when using a Large Language Model.


## Project Goal

Starting from a recipe image (photo, magazine scan, handwritten note):

1. Extract raw text using OCR
2. Convert noisy OCR output into a structured recipe
3. Ensure that:
    - no data is invented
    - all corrections are deterministic and traceable
    - the system remains testable without relying on a real LLM


## Global Architecture

The project is organized as a monorepo, with the current focus on the AI backend:`

```
MemoRecipe/
├─ memoRecipe-ia/          # Azure Functions (.NET 8, isolated)
│  ├─ Application/
│  │  ├─ Dtos/
│  │  ├─ Interfaces/
│  │  ├─ Pipeline/
│  │  └─ OcrQuantityNormalizer.cs
│  ├─ Infrastructure/
│  │  ├─ OCR/
│  │  └─ AI/
│  ├─ Functions/
│  └─ Program.cs
│
├─ tests/
│  ├─ Assets/              # Test images
│  └─ MemoRecipe.IA.Tests/ # Unit tests
│
└─ README.md
```


### Processing Pipeline

The pipeline is explicit and linear:

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


## Key principle

The LLM is never the source of truth.
It proposes a structure, but all domain corrections are applied afterwards, in code.


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


### Domain Post-Processing (Core Concept)

Some errors are neither OCR issues nor LLM hallucinations, but domain-specific problems.

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


### Testing Strategy

The project is strongly test-oriented:
- Unit tests for AI parsing (using fake LLMs)
- Unit tests for quantity normalization
- OCR-related tests
- No test depends on a real network call

Goal: being able to change the prompt, the model, or the AI provider without breaking domain behavior.


### Running Locally

Prerequisites :
- .NET 8 (LTS)
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
- “using AI quickly”
- hiding domain logic inside prompts
- blindly trusting LLM output

It aims to show that:
- an LLM can be treated as a component, not a brain
- application code must keep control
- AI-assisted systems can remain robust, testable, and explainable

## Current Status

- Local OCR implemented and tested
- Contract-based AI parsing in place
- End-to-end pipeline wired
- Deterministic domain post-processing implemented
- Tests passing

Planned (not yet implemented):
- richer domain model
- persistence layer
- improved user feedback