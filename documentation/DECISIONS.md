# Decisions & Technical Debt

Ce fichier trace les decisions architecturales, les choix techniques et la dette technique identifiee.

---

## Decisions architecturales

### DEC-001 : Monorepo avec separation claire des responsabilites
- **Date** : Nov 2025
- **Choix** : Un seul repo Git contenant 3 briques (IA, API, Front)
- **Pourquoi** : Simplifie le versionning et les PRs cross-projets pour un projet solo. Chaque brique reste independante (solutions .sln separees, frameworks differents).
- **Consequence** : Le front ne communique jamais directement avec les Azure Functions, tout passe par l'API.

### DEC-002 : Clean Architecture pour l'API (4 couches)
- **Date** : Nov 2025
- **Choix** : Api > Application > Domain > Infrastructure
- **Pourquoi** : Separation des responsabilites (SRP), testabilite, independance du framework. Le Domain ne depend de rien, l'Application contient la logique metier, l'Infrastructure gere la persistance.
- **Consequence** : Les services metier vivent dans Application, pas dans Api.

### DEC-003 : L'IA comme source de donnees, pas source de verite
- **Date** : Nov 2025
- **Choix** : Le LLM propose, le code decide. Toutes les corrections sont deterministes et testables.
- **Pourquoi** : Fiabilite, reproductibilite, testabilite. Un changement de modele IA ne doit pas casser le comportement metier.

### DEC-004 : PostgreSQL avec colonnes JSONB
- **Date** : Nov 2025
- **Choix** : Donnees structurees en tables relationnelles + JSONB pour les donnees flexibles (OCR brut, nutrition, metadata).
- **Pourquoi** : PostgreSQL gere nativement le JSON avec indexation. Evite de creer des tables pour des donnees semi-structurees qui varient beaucoup.

### DEC-005 : JWT pour l'authentification API-first
- **Date** : Nov 2025
- **Choix** : JWT Bearer stateless, pas de cookies de session.
- **Pourquoi** : L'API sera consommee par un client web (Blazor) ET une app mobile (MAUI). JWT fonctionne sur les deux sans gestion de session serveur.

### DEC-006 : Ne pas restructurer les dossiers du monorepo maintenant
- **Date** : Mars 2026
- **Choix** : Garder la structure actuelle `memoRecipeAppProject/memorecipe-api/src/...` meme si `memorecipe-api` est un niveau de dossier redondant.
- **Pourquoi** : Le gain est cosmetique. Restructurer casserait les chemins dans .sln, .csproj, migrations, docker-compose. On applique YAGNI : on restructure quand c'est bloquant, pas pour du cosmetique.

---

## Inconsistances identifiees (a corriger)

### INC-001 : AuthService depend directement de MemoRecipeDbContext [SEVERITE: MOYENNE]
- **Fichier** : `MemoRecipe.Application/Services/Auth/AuthService.cs`
- **Probleme** : La couche Application reference directement la couche Infrastructure (DbContext). En Clean Architecture stricte, Application ne devrait pas connaitre Infrastructure.
- **Impact** : Couplage fort. Si on change d'ORM ou de base de donnees, il faut modifier les services metier.
- **Solution possible** : Introduire des interfaces Repository dans Application (ex: `IUserRepository`) implementees dans Infrastructure. Le service ne connaitrait que l'interface.
- **Quand corriger** : Lors de la feature 1.1 (Recipe CRUD), decider du pattern (Repository vs DbContext direct) et l'appliquer de maniere coherente.

### INC-002 : Classe LoginRequest morte dans AuthController [SEVERITE: FAIBLE]
- **Fichier** : `MemoRecipe.Api/Controllers/AuthController.cs` (lignes 59-63)
- **Probleme** : Une classe `LoginRequest` est declaree en bas du fichier mais n'est jamais utilisee (c'est `LoginDto` qui est utilise). Code mort = confusion.
- **Solution** : Supprimer la classe `LoginRequest`.

### INC-003 : RecipeDto incomplet par rapport a l'entite [SEVERITE: MOYENNE]
- **Fichier** : `MemoRecipe.Application/DTOs/Recipes/RecipeDto.cs`
- **Probleme** : Il manque des proprietes qui existent sur l'entite Recipe :
  - `PrepTimeMinutes` (seul `TotalTimeMinutes` est expose)
  - `CookTimeMinutes`
  - `Difficulty` (enum)
  - `IsPublic`
  - `CreatedAt` / `UpdatedAt`
  - `UserId`
- **Impact** : Le front ne pourra pas afficher ces informations. L'update DTO a ces champs mais pas le read DTO.
- **Solution** : Ajouter les proprietes manquantes dans `RecipeDto`.

### INC-004 : RecipeCreateDto manque le champ Difficulty [SEVERITE: FAIBLE]
- **Fichier** : `MemoRecipe.Application/DTOs/Recipes/RecipeCreateDto.cs`
- **Probleme** : L'entite Recipe a un champ `Difficulty` (enum Easy/Medium/Hard) mais le DTO de creation ne le propose pas. La valeur sera toujours `Easy` par defaut.
- **Solution** : Ajouter `DifficultyLevel Difficulty` au DTO (ou decider que Easy est toujours le defaut a la creation).

### INC-005 : UserController sans [Authorize] [SEVERITE: HAUTE]
- **Fichier** : `MemoRecipe.Api/Controllers/UserController.cs`
- **Probleme** : L'endpoint `GET /api/users/{id}` est accessible sans authentification. N'importe qui peut consulter les infos d'un utilisateur par son ID.
- **Impact** : Faille de securite. Exposition de donnees personnelles.
- **Solution** : Ajouter `[Authorize]` sur le controller ou l'action. Evaluer si cet endpoint est meme necessaire (le endpoint `GET /auth/me` existe deja).

### INC-006 : RecipeProfile mapping Categories potentiellement incomplet [SEVERITE: FAIBLE]
- **Fichier** : `MemoRecipe.Application/Mappings/Profiles/RecipeProfile.cs` (ligne 29)
- **Probleme** : Le mapping `src.RecipeCategories` vers `List<CategoryDto>` fonctionne grace au mapping dans CategoryProfile, mais necessite que `RecipeCategory.Category` soit charge en Eager Loading (.Include). Si oublie dans le service, les noms de categories seront vides.
- **Impact** : Pas un bug aujourd'hui (pas de service Recipe), mais piege a retenir lors de l'implementation.
- **Action** : S'assurer d'utiliser `.Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)` dans le RecipeService.

---

## Dette technique

### DEBT-001 : Structure de dossiers redondante (voir DEC-006)
- **Impact** : Faible (cosmetique)
- **Priorite** : Basse

### DEBT-002 : Pas de validation d'entree sur les endpoints
- **Impact** : Haute (securite + UX)
- **Priorite** : A traiter en feature 1.3 (FluentValidation)

### DEBT-003 : Pas de gestion d'erreur globale
- **Impact** : Moyenne (stack traces exposees en dev, erreurs 500 non formattees)
- **Priorite** : A traiter en feature 1.4 (Error Middleware)

### DEBT-004 : Secrets en clair dans appsettings.json
- **Impact** : Haute (securite)
- **Detail** : Cle JWT et mot de passe DB en dur dans `appsettings.json`
- **Priorite** : A traiter en feature 2.4 (Secrets Management)

### DEBT-005 : Pas de tests cote API
- **Impact** : Haute (qualite, regression)
- **Priorite** : A traiter en Phase 5

### DEBT-006 : Pattern d'acces aux donnees non uniforme
- **Impact** : Moyenne (coherence)
- **Detail** : Les services utilisent directement DbContext. A decider si on introduit le pattern Repository ou si on garde DbContext comme "Repository suffisant" (voir INC-001).
- **Priorite** : A trancher lors de la feature 1.1
