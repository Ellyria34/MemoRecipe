<!-- Chargé à chaque session Claude Code. Max ~40 lignes. Une règle = une ligne vérifiable. Rien de personnel ici (fichier tracké). -->

# MemoRecipe

## Stack
- Monorepo .NET : `memoRecipeAppProject/memorecipe-api/` (ASP.NET, .NET 10), `memoRecipe-ia/` (Azure Functions, .NET 8), `App/MemoRecipe.Web/` (Blazor WASM).
- API en Clean Architecture : Api -> Application -> Domain -> Infrastructure.
- PostgreSQL 16 via Docker, EF Core 10. Tests : xUnit, TestContainers (integration), Playwright (E2E).

## Commandes
- Build API : `dotnet build memoRecipeAppProject/memorecipe-api/memorecipe-api.sln`
- Build App : `dotnet build App/MemoRecipe.Web/MemoRecipe.Web.csproj`
- Build IA : `dotnet build memoRecipe-ia/memorecipe-ia.sln`
- Tests API : `dotnet test memoRecipeAppProject/memorecipe-api/memorecipe-api.sln`
- Tests IA (sans Tesseract) : `dotnet test tests/MemoRecipe.IA.Tests/MemoRecipe.IA.Tests.csproj --filter "Category!=Integration"`
- Lancer les tests concernes avant de proposer un commit.

## Code
- Commentaires de code (`//`, XML doc) en anglais. Textes affiches a l'utilisateur en francais.
- ASCII pur dans le code, les commentaires et les commits : pas d'emoji ni d'unicode decoratif.
- Mapperly : garder `[MapperIgnoreSource]` / `[MapperIgnoreTarget]` explicites. Ne pas proposer `RequiredMappingStrategy.None`.
- Dockerfile : `COPY <fichier> <dossier>/` avec slash final obligatoire pour cibler un dossier.

## Git
- Commit = une seule ligne, style `type(scope): description`. Pas de body, pas de trailer ni attribution (`Co-Authored-By`, footer genere, ou autre), ni dans les commits ni dans les PR bodies. Le detail va dans la PR.
- Ordre : branche -> push -> lier la branche a l'issue (GitHub, section Development) -> ouvrir la PR. Aucun push direct sur `main`.

## Fichiers sensibles
- Ne jamais lire ni afficher `.env*` (sauf `.env.example`), `appsettings.Development.json`, `local.settings.json`, ni aucun fichier de credentials.
- Ne jamais inclure ces fichiers dans un `git add`, meme modifies. Seul `appsettings.json` est tracke.

## Documentation
- `journal/`, `documentation/BACKLOG.md`, `documentation/Backlog_*.md`, `documentation/FEATURES.md`, `documentation/fiches/` sont personnels et gitignores : ne jamais les ajouter au depot.
- Avant de modifier le journal ou le backlog, lire `documentation/CONVENTIONS/journal-sprint.md` et `documentation/CONVENTIONS/backlog-us.md`.
- Docs publiques trackees (README, DECISIONS, DEPLOYMENT) : justifications techniques uniquement. Aucune information personnelle, aucun secret, aucun prestataire actif nomme, aucun chiffre personnel.
