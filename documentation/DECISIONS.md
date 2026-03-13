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

### DEC-007 : Repository Pattern pour tous les agregats (Recipe + User)
- **Date** : Mars 2026
- **Choix** : `IRecipeRepository` et `IUserRepository` dans Application, implementations dans Infrastructure.
- **Pourquoi** : DIP (Dependency Inversion Principle) — Application definit le contrat, Infrastructure l'implemente. Permet les tests unitaires avec FakeRepository sans base de donnees. Corrige la reference circulaire Application ↔ Infrastructure.
- **Consequence** : Architecture propre : Api → Application ← Infrastructure → Domain.

### DEC-008 : Verification IsPublic dans GetByIdAsync
- **Date** : Mars 2026
- **Choix** : Un user ne peut voir la recette d'un autre que si elle est publique (`IsPublic = true`).
- **Pourquoi** : Securite par defaut. `[Authorize]` verifie seulement l'authentification ("qui es-tu ?"), pas l'autorisation ("as-tu le droit ?"). La logique metier vit dans le service (SRP).
- **Consequence** : `GetByIdAsync` prend un `userId` en parametre pour evaluer les droits d'acces.

### DEC-009 : Tests unitaires avec FakeRepository
- **Date** : Mars 2026
- **Choix** : Implémenter `IRecipeRepository` avec une `List<Recipe>` en memoire pour les tests.
- **Pourquoi** : Tests deterministes, rapides (< 1s), sans base de donnees, sans Docker. Meme pattern que `FakeRecipeAiService` dans les tests IA.
- **Consequence** : Les tests unitaires ne testent pas la persistance (c'est voulu). Les tests d'integration avec vraie DB sont une dette a traiter.

### DEC-010 : MudBlazor comme librairie UI pour Blazor
- **Date** : Mars 2026
- **Choix** : MudBlazor plutot que Bootstrap ou Tailwind
- **Pourquoi** : Composants natifs Blazor (C#, pas du HTML+classes CSS). Theme centralise, responsive integre, zero JS a ecrire. Lib la plus utilisee dans l'ecosysteme Blazor.
- **Risque** : Dependance a une lib tierce. Mitige par Clean Architecture — seule la couche Web utilise MudBlazor, Domain/Application restent independants.

### DEC-011 : FluentValidation plutot que Data Annotations
- **Date** : Mars 2026
- **Choix** : FluentValidation pour valider les DTOs (RecipeCreate, RecipeUpdate, Register, Login)
- **Pourquoi** : Regles dans une classe separee (SRP — le DTO reste un DTO). Testable unitairement avec `TestValidate`. Messages personnalisables. Validations conditionnelles avec `.When(...)` pour le partial update.
- **Consequence** : Validation dans les controllers avant appel aux services. 71 tests unitaires couvrent tous les validators.

### DEC-012 : Global Exception Middleware
- **Date** : Mars 2026
- **Choix** : Middleware custom (`ExceptionMiddleware`) plutot que le handler par defaut d'ASP.NET
- **Pourquoi** : Controle total sur la reponse d'erreur. Le client recoit toujours un message generique (`An unexpected error occurred.`), jamais de stack trace. Les logs serveur recoivent l'exception complete via `ILogger`.
- **Consequence** : Enregistre en premier dans le pipeline (`app.UseMiddleware<ExceptionMiddleware>()`). Principe "fail safely".

### DEC-013 : FakeAuthService pour le developpement frontend
- **Date** : Mars 2026
- **Choix** : Implementer `IAuthService` avec une version fake (`FakeAuthService`) pour le developpement frontend sans API.
- **Pourquoi** : Permet de developper et tester toute l'UX sans avoir besoin de l'API, de la base de donnees ou de Docker. Une seule ligne a changer dans `Program.cs` pour switcher. Meme pattern que `FakeRecipeAiService` cote IA.
- **Consequence** : `FakeAuthService` n'est jamais deploye en production. Il est remplace par `AuthService` (HTTP) des que l'API est disponible.

### DEC-014 : Migration localStorage → cookies HttpOnly pour les tokens JWT
- **Date** : Mars 2026
- **Choix** : Abandonner `localStorage` pour stocker les tokens JWT, migrer vers des cookies `HttpOnly + Secure + SameSite=Strict`.
- **Pourquoi** : `localStorage` est accessible en clair via les DevTools du navigateur et lisible par JavaScript → vulnerable aux attaques XSS. Un cookie `HttpOnly` ne peut pas etre lu par JavaScript — le navigateur l'envoie uniquement directement au serveur.
- **Impact** : Backend — `Login` et `Register` posent un cookie au lieu de retourner `{ token }`. Frontend — `AuthService` n'a plus besoin de `ILocalStorageService`, plus de gestion manuelle du token.
- **Etat** : DONE — branche `feature/auth-frontend`. Backend pose le cookie, frontend utilise `CookieHandler` + `IHttpClientFactory`. DEBT-002 et DEBT-003 resolus.

### DEC-015 : Routes protegees avec CookieAuthStateProvider
- **Date** : Mars 2026
- **Choix** : `AuthenticationStateProvider` custom qui appelle `api/auth/me` pour verifier l'auth, avec cache en memoire.
- **Pourquoi** : Avec les cookies HttpOnly, le frontend ne peut pas lire le token. Le seul moyen de savoir si l'utilisateur est connecte est de demander au serveur. Le cache evite de refaire l'appel API a chaque navigation entre pages.
- **Impact** : `App.razor` utilise `CascadingAuthenticationState` + `AuthorizeRouteView`. Les pages protegees utilisent `@attribute [Authorize]`. Les pages publiques (`/login`, `/register`) restent accessibles sans auth. `RedirectToLogin` redirige vers `/login` si non authentifie.
- **Etat** : DONE — branche `feature/protected-routes`.

### DEC-016 : Layout responsive — sidebar desktop + bottom bar mobile
- **Date** : Mars 2026
- **Choix** : Layout adaptatif selon la taille d'ecran. Desktop : top bar (logo, user, logout) + sidebar gauche (navigation). Mobile : top bar + bottom bar (navigation). Memes liens, affichage conditionnel.
- **Pourquoi** : UX mobile-first. Sur mobile, le pouce atteint facilement le bas de l'ecran (pattern standard : Instagram, Spotify). Sur desktop, la sidebar offre plus d'espace pour les labels + icones. La top bar reste presente dans les deux cas pour le branding et les actions utilisateur.
- **Composants MudBlazor** : `MudAppBar` (top bar), `MudDrawer` (sidebar desktop), bottom bar custom (mobile). Affichage conditionnel via CSS media queries ou `MudHidden`.
- **Pages** : `/` (dashboard), `/recipes` (mon livre), `/recipes/{id}` (detail + edition inline), `/recipes/new` (import scan/photo), `/login`, `/register`.

---

## A investiguer

### INV-001 : Appel api/auth/me retourne 401 sur les pages publiques
- **Constat** : Le CookieAuthStateProvider appelle systematiquement api/auth/me au chargement de l'app, meme sur /login et /register. Retourne 401 si pas de cookie → visible en console (erreur rouge).
- **Impact** : Aucun impact fonctionnel. Cosmétique (erreur visible en console DevTools).
- **Options a evaluer** :
  1. Ignorer — pattern standard des SPAs, pas visible par l'utilisateur
  2. Flag localStorage non-sensible (isLoggedIn true/false) pour eviter l'appel quand pas connecte
- **Etat** : A EVALUER

---

## Inconsistances identifiees (a corriger)

### INC-001 : ~~AuthService depend directement de MemoRecipeDbContext~~ [RESOLUE]
- **Resolution** : `IUserRepository` cree dans Application, `UserRepository` implemente dans Infrastructure. `AuthService` utilise desormais `IUserRepository`. La reference circulaire Application → Infrastructure a ete supprimee. Architecture propre : Api → Application ← Infrastructure. Commit `refactor(arch): fix circular dependency between Application and Infrastructure`.

### INC-002 : ~~Classe LoginRequest morte dans AuthController~~ [RESOLUE]
- **Resolution** : Classe supprimee. Commit `fix: remove dead code and unused UserController endpoint`.

### INC-003 : ~~RecipeDto incomplet par rapport a l'entite~~ [RESOLUE]
- **Resolution** : `RecipeDto` complete avec toutes les proprietes manquantes : `PrepTimeMinutes`, `CookTimeMinutes`, `Difficulty`, `IsPublic`, `CreatedAt`, `UpdatedAt`, `UserId`.

### INC-004 : ~~RecipeCreateDto manque le champ Difficulty~~ [RESOLUE]
- **Resolution** : `DifficultyLevel? Difficulty` ajoute dans `RecipeCreateDto` et `RecipeUpdateDto`. Nullable par choix delibere : ne pas renseigner la difficulte ne signifie pas "Facile".

### INC-005 : ~~UserController sans [Authorize]~~ [RESOLUE]
- **Resolution** : Endpoint supprime (YAGNI + surface d'attaque minimale). `GET /auth/me` couvre le besoin. Un endpoint public avec `PublicUserDto` sera cree si necessaire (ex: afficher l'auteur d'une recette). Commit `fix: remove dead code and unused UserController endpoint`.

### INC-006 : ~~RecipeProfile mapping Categories potentiellement incomplet~~ [RESOLUE]
- **Resolution** : `RecipeRepository` utilise `.Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)` sur toutes les requetes. Les categories sont toujours chargees en Eager Loading.

---

## Dette technique

### DEBT-001 : Structure de dossiers redondante (voir DEC-006)
- **Impact** : Faible (cosmetique)
- **Priorite** : Basse

### DEBT-002 : ~~AuthService utilise localStorage pour les tokens JWT~~ [RESOLUE]
- **Resolution** : Migration vers cookies HttpOnly (DEC-014). `LocalStorageService` supprime. `AuthService` utilise desormais `IHttpClientFactory` + `CookieHandler`. Le token n'est plus jamais accessible en JavaScript.

### DEBT-003 : ~~Register controller retourne Ok(user) au lieu de Ok(new { token })~~ [RESOLUE]
- **Resolution** : `Register` pose un cookie `authCookie` et retourne `Ok()`. Plus de token expose dans la reponse. Corrige en meme temps que DEBT-002.

### DEBT-002 : ~~Pas de validation d'entree sur les endpoints~~ [RESOLUE]
- **Resolution** : FluentValidation integre pour RecipeCreateDto, RecipeUpdateDto, RegisterDto, LoginDto. 4 validators, 71 tests unitaires. Validation dans les controllers avant appel aux services.

### DEBT-003 : ~~Pas de gestion d'erreur globale~~ [RESOLUE]
- **Resolution** : `ExceptionMiddleware` ajouté. Client recoit un message generique, logs serveur recoivent la stack trace complete.

### DEBT-004 : ~~Secrets en clair dans appsettings.json~~ [RESOLUE PARTIELLEMENT]
- **Resolution** : `appsettings.Development.json` cree pour les secrets locaux, ajoute au `.gitignore`. `appsettings.json` ne contient plus que des placeholders explicites (`CHANGE_ME_USE_APPSETTINGS_DEVELOPMENT_JSON`).
- **Restant** : En production, utiliser Azure Key Vault ou variables d'environnement. A traiter en feature 2.4 (Secrets Management).

### DEBT-005 : ~~Pas de tests cote API~~ [RESOLUE PARTIELLEMENT]
- **Resolution** : Projet `MemoRecipe.Application.Tests` cree avec 13 tests unitaires couvrant `RecipeService` (GetById, GetAll, Create, Update, Delete). Pattern FakeRepository utilise pour des tests deterministes sans base de donnees.
- **Restant** : Tests d'integration (avec vraie DB) et tests des autres services (Auth) a ajouter.

### DEBT-006 : ~~Pattern d'acces aux donnees non uniforme~~ [RESOLUE]
- **Resolution** : Repository Pattern adopte uniformement. `IRecipeRepository` + `IUserRepository` dans Application, implementations dans Infrastructure. Plus aucun service n'accede directement a `MemoRecipeDbContext`.
