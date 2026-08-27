# Decisions & Technical Debt

Ce fichier trace les decisions architecturales, les choix techniques et la dette technique identifiee.

---

## Decisions architecturales

### DEC-001 : Monorepo avec separation claire des responsabilites
- **Statut** : ✅ ACTIVE
- **Date** : Nov 2025
- **Choix** : Un seul repo Git contenant 3 briques (IA, API, Front)
- **Pourquoi** : Simplifie le versionning et les PRs cross-projets pour un projet solo. Chaque brique reste independante (solutions .sln separees, frameworks differents).
- **Consequence** : Le front ne communique jamais directement avec les Azure Functions, tout passe par l'API.

### DEC-002 : Clean Architecture pour l'API (4 couches)
- **Statut** : ✅ ACTIVE
- **Date** : Nov 2025
- **Choix** : Api > Application > Domain > Infrastructure
- **Pourquoi** : Separation des responsabilites (SRP), testabilite, independance du framework. Le Domain ne depend de rien, l'Application contient la logique metier, l'Infrastructure gere la persistance.
- **Consequence** : Les services metier vivent dans Application, pas dans Api.

### DEC-003 : L'IA comme source de donnees, pas source de verite
- **Statut** : ✅ ACTIVE
- **Date** : Nov 2025
- **Choix** : Le LLM propose, le code decide. Toutes les corrections sont deterministes et testables.
- **Pourquoi** : Fiabilite, reproductibilite, testabilite. Un changement de modele IA ne doit pas casser le comportement metier.

### DEC-004 : PostgreSQL avec colonnes JSONB
- **Statut** : ✅ ACTIVE
- **Date** : Nov 2025
- **Choix** : Données structurées en tables relationnelles + JSONB pour les données flexibles (OCR brut, nutrition, metadata).
- **Pourquoi** : PostgreSQL gère nativement le JSON avec indexation. Évite de créer des tables pour des données semi-structurées qui varient beaucoup.
- **Colonnes concrètes** :
  - `IngredientNutrition.AllergensJson` : liste d'allergènes
  - `OCRExtraction.JsonData` : sortie brute OCR + IA structurée
  - `RecipeSource.MetadataJson` : metadata source variable (URL, livre, etc.)
- **Conséquence sur les tests (identifiée 02/06/2026)** :
  Les tests d'intégration actuels utilisent SQLite in-memory via `WebApplicationFactory`. **Deux divergences silencieuses** vs Postgres prod :
  **1. JSONB** : SQLite ne supporte pas le type `jsonb` — traduit en `TEXT`. Aujourd'hui sans risque (aucune query JSONB-specific dans le code), mais dès que des queries `@>`, `?`, `->` seront ajoutées (ex: recherche par allergène), il faudra TestContainers.
  **2. Dates et timestamps** : SQLite n'a pas de type date natif (stockage en TEXT/ISO string), donc :
    - Pas de support `TIMESTAMP WITH TIME ZONE` (les colonnes `CreatedAt`/`UpdatedAt` perdent la sémantique TIMESTAMPTZ)
    - Précision microseconde Postgres → précision variable SQLite
    - `DateTime.Kind` perdu au round-trip (revient `Unspecified` en SQLite vs `Utc` en Postgres + Npgsql)
  Aujourd'hui le code utilise systématiquement `DateTime.UtcNow` et aucune logique métier ne dépend de `.Kind` après lecture DB → risque dates faible. Mais le risque latent grandira avec les features futures (search par période, filtre temporel).
  **A Faire** : migration vers TestContainers tracée dans **BACK-062**.
- **État** : DÉCIDÉ et appliqué — Postgres avec JSONB en place depuis InitialCreate migration.


### DEC-005 : JWT pour l'authentification API-first
- **Statut** : 🟡 SUPERSEDED par [DEC-014](#dec-014-migration-localstorage--cookies-httponly-pour-les-tokens-jwt), qui décrit le mode d'authentification en vigueur aujourd'hui (JWT stateless transporté dans un cookie HttpOnly plutôt que dans un header Bearer).
- **Date** : Nov 2025
- **Choix** : JWT Bearer stateless, pas de cookies de session.
- **Pourquoi** : L'API sera consommee par un client web (Blazor) ET une app mobile (MAUI). JWT fonctionne sur les deux sans gestion de session serveur.

### DEC-006 : Ne pas restructurer les dossiers du monorepo maintenant
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : Garder la structure actuelle `memoRecipeAppProject/memorecipe-api/src/...` meme si `memorecipe-api` est un niveau de dossier redondant.
- **Pourquoi** : Le gain est cosmetique. Restructurer casserait les chemins dans .sln, .csproj, migrations, docker-compose. On applique YAGNI : on restructure quand c'est bloquant, pas pour du cosmetique.

### DEC-007 : Repository Pattern pour tous les agregats (Recipe + User)
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : `IRecipeRepository` et `IUserRepository` dans Application, implementations dans Infrastructure.
- **Pourquoi** : DIP (Dependency Inversion Principle) — Application definit le contrat, Infrastructure l'implemente. Permet les tests unitaires avec FakeRepository sans base de donnees. Corrige la reference circulaire Application ↔ Infrastructure.
- **Consequence** : Architecture propre : Api → Application ← Infrastructure → Domain.

### DEC-008 : Verification IsPublic dans GetByIdAsync
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : Un user ne peut voir la recette d'un autre que si elle est publique (`IsPublic = true`).
- **Pourquoi** : Securite par defaut. `[Authorize]` verifie seulement l'authentification ("qui es-tu ?"), pas l'autorisation ("as-tu le droit ?"). La logique metier vit dans le service (SRP).
- **Consequence** : `GetByIdAsync` prend un `userId` en parametre pour evaluer les droits d'acces.

### DEC-009 : Tests unitaires avec FakeRepository
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : Implémenter `IRecipeRepository` avec une `List<Recipe>` en memoire pour les tests.
- **Pourquoi** : Tests deterministes, rapides (< 1s), sans base de donnees, sans Docker. Meme pattern que `FakeRecipeAiService` dans les tests IA.
- **Consequence** : Les tests unitaires ne testent pas la persistance (c'est voulu). Les tests d'integration avec vraie DB sont une dette a traiter (résolue depuis via [DEC-033](#dec-033) TestContainers Postgres).
- **Framework de test** : xUnit (choix implicite depuis le début du projet, cohérent avec l'écosystème .NET moderne et retenu pour son intégration native `dotnet test`). L'organisation des fakes transverses réutilisés entre plusieurs projets de tests est formalisée dans [DEC-059](#dec-059) (`MemoRecipe.Tests.Shared`).

### DEC-010 : MudBlazor comme librairie UI pour Blazor
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : MudBlazor plutot que Bootstrap ou Tailwind
- **Pourquoi** : Composants natifs Blazor (C#, pas du HTML+classes CSS). Theme centralise, responsive integre, zero JS a ecrire. Lib la plus utilisee dans l'ecosysteme Blazor.
- **Risque** : Dependance a une lib tierce. Mitige par Clean Architecture — seule la couche Web utilise MudBlazor, Domain/Application restent independants.

### DEC-011 : FluentValidation plutot que Data Annotations
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : FluentValidation pour valider les DTOs (RecipeCreate, RecipeUpdate, Register, Login)
- **Pourquoi** : Regles dans une classe separee (SRP — le DTO reste un DTO). Testable unitairement avec `TestValidate`. Messages personnalisables. Validations conditionnelles avec `.When(...)` pour le partial update.
- **Consequence** : Validation dans les controllers avant appel aux services. 71 tests unitaires couvrent tous les validators.

### DEC-012 : Global Exception Middleware
- **Statut** : ✅ ACTIVE, avec évolutions internes. Depuis Mars 2026, l'`ExceptionMiddleware` a été enrichi de 3 catches spécifiques pour exceptions métier typées : `AccountMarkedForDeletionException` renvoie 403, `AiRateLimitExceededException` renvoie 429 avec header `Retry-After`, `RecipeLimitReachedException` renvoie 403 avec discriminant JSON `error: "recipe_limit_reached"`. Le catch général `Exception` renvoie 500 avec message générique et déclenche en plus une alerte Telegram via `IAlertingService.NotifyServerErrorAsync()` (skip pour `/health` qui surface via son propre check). L'ordre pipeline a également évolué, le middleware n'est plus enregistré en premier comme initialement décrit, il vient après CORS, RateLimiter, SecurityHeaders, SerilogRequestLogging et HttpsRedirection (dev). Ce nouvel ordre est volontaire pour laisser les middlewares transversaux (CORS, rate limiting, headers de sécurité) s'appliquer avant la capture d'exception métier.
- **Date** : Mars 2026
- **Choix** : Middleware custom (`ExceptionMiddleware`) plutot que le handler par defaut d'ASP.NET
- **Pourquoi** : Controle total sur la reponse d'erreur. Le client recoit toujours un message generique (`An unexpected error occurred.`), jamais de stack trace. Les logs serveur recoivent l'exception complete via `ILogger`.
- **Consequence** : Enregistre en premier dans le pipeline (`app.UseMiddleware<ExceptionMiddleware>()`). Principe "fail safely".

### DEC-013 : FakeAuthService pour le developpement frontend
- **Statut** : ✅ ACTIVE. Le wire par défaut dans `App/MemoRecipe.Web/Program.cs` est `AuthService` (implémentation HTTP réelle), branchée depuis la mise en place de l'API auth (voir [DEC-014](#dec-014) et [DEC-015](#dec-015)). Le `FakeAuthService` est conservé opérationnel dans le codebase comme option dev offline, permettant de développer et tester l'UX frontend sans dépendre de l'API backend (utile en déplacement sans connexion ou pour tester des scénarios sans backend démarré). Le swap se fait en changeant une seule ligne dans `Program.cs` (`AddScoped<IAuthService, FakeAuthService>()`). Le fake est maintenu au contrat `IAuthService` au fil de ses évolutions (dernière méthode ajoutée : `RequestAccountDeletionAsync` post BACK-005 juin 2026).
- **Date** : Mars 2026
- **Choix** : Implementer `IAuthService` avec une version fake (`FakeAuthService`) pour le developpement frontend sans API.
- **Pourquoi** : Permet de developper et tester toute l'UX sans avoir besoin de l'API, de la base de donnees ou de Docker. Une seule ligne a changer dans `Program.cs` pour switcher. Meme pattern que `FakeRecipeAiService` cote IA.
- **Consequence** : `FakeAuthService` n'est jamais deploye en production. Il est remplace par `AuthService` (HTTP) des que l'API est disponible.

### DEC-014 : Migration localStorage → cookies HttpOnly pour les tokens JWT
- **Statut** : ✅ ACTIVE. Cette décision remplace [DEC-005](#dec-005--jwt-pour-lauthentification-api-first) et devient la source de vérité complète pour l'authentification de l'API MemoRecipe.
- **Date** : Mars 2026 (le principe JWT stateless est hérité de DEC-005, Nov 2025)
- **Choix** : Utiliser un token JWT stateless (aucune session côté serveur, cible client web Blazor et mobile MAUI), transporté dans un cookie `HttpOnly + Secure + SameSite=Strict` plutôt que dans un header `Authorization: Bearer` ou dans `localStorage`.
- **Pourquoi** :
  - **JWT stateless** (aspect maintenu depuis DEC-005) : pas de gestion de session côté serveur, le token porte les claims (userId, expiration). Compatible avec plusieurs clients (Blazor web, MAUI mobile) sans state partagé.
  - **Abandon de `localStorage`** : accessible en clair via les DevTools du navigateur et lisible par JavaScript, donc vulnérable aux attaques XSS. Un cookie `HttpOnly` ne peut pas être lu par JavaScript, le navigateur l'envoie uniquement au serveur.
  - **`SameSite=Strict`** : le cookie n'est envoyé que pour les requêtes originaires du même site, protection anti-CSRF sans token dédié (voir [DEC-024](#dec-024--pas-de-token-anti-csrf-protection-par-samesitestrict--cors)).
- **Impact** :
  - Backend : `Login` et `Register` posent un cookie au lieu de retourner `{ token }`. `JwtBearerEvents.OnMessageReceived` lit le token depuis `Request.Cookies["authCookie"]` au lieu du header `Authorization`.
  - Frontend : `AuthService` n'a plus besoin de `ILocalStorageService`, plus de gestion manuelle du token. Le navigateur attache le cookie automatiquement à chaque requête vers l'API (via `CookieHandler` + `IHttpClientFactory` avec `credentials: include`).
- **Etat** : DONE. Backend pose le cookie, frontend utilise `CookieHandler` + `IHttpClientFactory`. DEBT-002 et DEBT-003 resolus.

### DEC-015 : Routes protegees avec CookieAuthStateProvider
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : `AuthenticationStateProvider` custom qui appelle `api/auth/me` pour verifier l'auth, avec cache en memoire.
- **Pourquoi** : Avec les cookies HttpOnly, le frontend ne peut pas lire le token. Le seul moyen de savoir si l'utilisateur est connecte est de demander au serveur. Le cache evite de refaire l'appel API a chaque navigation entre pages.
- **Impact** : `App.razor` utilise `CascadingAuthenticationState` + `AuthorizeRouteView`. Les pages protegees utilisent `@attribute [Authorize]`. Les pages publiques (`/login`, `/register`) restent accessibles sans auth. `RedirectToLogin` redirige vers `/login` si non authentifie.
- **Etat** : DONE — branche `feature/protected-routes`.

### DEC-016 : Layout responsive — sidebar desktop + bottom bar mobile
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : Layout adaptatif selon la taille d'ecran. Desktop : top bar (logo, user, logout) + sidebar gauche (navigation). Mobile : top bar + bottom bar (navigation). Memes liens, affichage conditionnel.
- **Pourquoi** : UX mobile-first. Sur mobile, le pouce atteint facilement le bas de l'ecran (pattern standard : Instagram, Spotify). Sur desktop, la sidebar offre plus d'espace pour les labels + icones. La top bar reste presente dans les deux cas pour le branding et les actions utilisateur.
- **Composants MudBlazor** : `MudAppBar` (top bar), `MudDrawer` (sidebar desktop), bottom bar custom (mobile). Affichage conditionnel via CSS media queries ou `MudHidden`.
- **Pages** : `/` (dashboard), `/recipes` (mon livre), `/recipes/{id}` (detail + edition inline), `/recipes/new` (import scan/photo), `/login`, `/register`.

### DEC-017 : Frontend → API → Azure Function IA (pas d'appel direct)
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : Le frontend envoie l'image à l'API, qui appelle l'Azure Function IA. Le frontend ne communique jamais directement avec l'Azure Function.
- **Pourquoi** : Un seul point d'entrée sécurisé (cookies HttpOnly déjà en place). L'Azure Function peut rester privée/interne. Meilleur contrôle RGPD (traçabilité, audit, suppression des images). Compatible MAUI (même endpoint API). L'utilisateur n'a pas besoin de connaître l'existence du service IA.
- **Conséquence** : Nouveau service `IOcrScanService` (Application) / `OcrScanService` (Infrastructure) pour l'appel HTTP. Endpoint `POST api/recipe/scan` dans `RecipeController`. URL Azure Function configurable dans `appsettings.json`.
- **Etat** : DONE — scan IA fonctionnellement complet (endpoint `POST /api/recipes/scan` + frontend Blazor + Azure Function IA + parsing LLM + preview éditable + validation + sauvegarde BDD). Gated par le feature flag `Features:ScanRecipeEnabled` en V1 (cf. [DEC-040](#dec-040) + BACK-092) → sera réactivé sans changement de code en V1.1/V2.

### DEC-018 : RecipeFormModel séparé des DTOs API + composant RecipeForm réutilisable
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : Le formulaire de recette utilise un `RecipeFormModel` dédié (pas un DTO API) et vit dans un composant `RecipeForm.razor` réutilisable. Chaque page parente mappe vers le DTO approprié (`RecipeCreateDto` ou `RecipeUpdateDto`) avant d'appeler l'API.
- **Pourquoi** : Single Responsibility — le formulaire ne doit pas dépendre d'un contrat API. `RecipeFormModel` = ce que l'utilisateur voit et édite. Le même composant est réutilisé dans 3 contextes : scan (pré-rempli par l'IA), création manuelle (vide), modification (pré-rempli depuis l'API). Le parent décide du verbe HTTP (POST vs PUT), pas le formulaire.
- **Conséquence** : `RecipeFormModel` dans `Models/`, `RecipeForm.razor` dans `Components/`. Le composant expose un `[Parameter] RecipeFormModel` et un `[Parameter] EventCallback<RecipeFormModel>` pour notifier le parent au clic "Sauvegarder".
- **Etat** : DONE — composant intégré dans Scan, Edit et création manuelle (future).

### DEC-019 : Code-behind pattern + `= default!;` pour les pages Blazor
- **Statut** : ✅ ACTIVE
- **Date** : Mars 2026
- **Choix** : Séparer chaque page en `.razor` (template) + `.razor.cs` (code C#). Utiliser `= default!;` sur les propriétés `[Inject]` pour supprimer les warnings nullable.
- **Pourquoi** : Séparation des responsabilités (SRP) — le template ne contient que du HTML/Razor, le C# est dans une classe `partial`. `= default!;` est le pattern recommandé par Microsoft pour les injections Blazor ([doc officielle](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection)).
- **Conséquence** : Les `@inject` du `.razor` deviennent `[Inject]` dans le `.razor.cs` avec `{ get; set; } = default!;`. Les `using` doivent être ajoutés manuellement dans le `.razor.cs` (pas d'accès aux `@using` de `_Imports.razor`).
- **Etat** : DONE. Pattern initialement appliqué sur RecipeDetail, Recipes, ScanRecipe, EditRecipe, puis généralisé à l'ensemble des pages et composants Frontend (46 occurrences de `= default!;` dans 17 fichiers `.razor.cs` au 20/08/2026). Un layout dédié `AuthLayout.razor` (avec code-behind `AuthLayout.razor.cs`) a également été créé pour les pages non authentifiées (`/login`, `/register`), séparé du `MainLayout` général (pas de NavBar, pas de skip-link car pas de navigation à sauter).

### DEC-020 : Migration du hashing des mots de passe — HMAC-SHA512 → PBKDF2 (PasswordHasher\<T\>)
- **Statut** : ✅ ACTIVE
- **Date** : Avril 2026
- **Choix** : Remplacer le hashing custom `HMACSHA512` par `PasswordHasher<User>` de `Microsoft.AspNetCore.Identity` (PBKDF2, 100 000 itérations, salt intégré).
- **Pourquoi** : `HMACSHA512` est un algorithme rapide (milliards de hash/seconde) — vulnérable au brute force si la BDD est compromise. `PasswordHasher<T>` utilise PBKDF2 avec un work factor élevé, rendant le brute force impraticable. C'est le standard recommandé par Microsoft ([doc officielle](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0)).
- **Migration douce** : Les utilisateurs existants (hashés avec l'ancien algo) sont migrés automatiquement à la prochaine connexion — le login vérifie l'ancien hash, re-hash avec PBKDF2, vide le `PasswordSalt`, et sauvegarde. La méthode `VerifyLegacy()` est conservée temporairement pour la rétrocompatibilité.
- **Conséquence** : `PasswordHasher` n'est plus `static`, injecté via DI. Le champ `PasswordSalt` reste en BDD (pour vérifier les anciens hash) mais est vide pour les nouveaux users. `IUserRepository` a une nouvelle méthode `Update()`. À terme : supprimer `VerifyLegacy()` et le champ `PasswordSalt` quand tous les users auront migré.
- **Etat** : DONE — migration douce en place, testée avec comptes existants.

### DEC-021 : SecurityHeadersMiddleware custom plutot que packages tiers
- **Statut** : ✅ ACTIVE
- **Date** : Avril 2026
- **Choix** : Middleware custom dans `MemoRecipe.Api/Middlewares/SecurityHeadersMiddleware.cs` qui ajoute 6 headers de securite (X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, CSP, HSTS) sur chaque reponse.
- **Pourquoi** : Les headers sont statiques et peu nombreux — un middleware custom de ~20 lignes est plus simple et transparent qu'un package tiers (NWebsec, etc.). On garde le controle total sur les valeurs. CSP adapte a Blazor WASM (`wasm-unsafe-eval`) + MudBlazor (`unsafe-inline` pour style-src) + Google Fonts.
- **HSTS conditionnel** : `Strict-Transport-Security` ajoute uniquement en production (`!IsDevelopment()`), car HSTS casserait le dev local en HTTP/certificats auto-signes.
- **X-XSS-Protection volontairement omis** : Header deprecie (MDN 2025), peut creer des failles XSS. CSP le remplace entierement.
- **Sources** : [OWASP HTTP Headers Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html), [MDN Security Headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers), [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/).
- **Etat** : DONE — BACK-001, 7 tests d'integration.

### DEC-022 : Rate limiting double couche — IP natif + per-account custom
- **Statut** : ✅ ACTIVE
- **Date** : Avril 2026
- **Choix** : Deux niveaux de rate limiting complementaires. Niveau 1 : `AddRateLimiter()` natif ASP.NET Core avec Fixed Window par IP (global 100/min, auth 10/min, scan 5/min). Niveau 2 : compteur custom par email dans `AuthService` avec `IMemoryCache` (5 echecs → blocage 15 min).
- **Pourquoi** : Le rate limiting par IP ne suffit pas contre le credential stuffing (botnets avec milliers d'IP). Le rate limiting par compte via `IMemoryCache` bloque AVANT la verification du mot de passe (evite le timing attack). Le rate limiter natif `AddPolicy()` avec partition par `httpContext.User` ne fonctionne PAS pour le login car `UseRateLimiter` s'execute avant `UseAuthentication`.
- **LoginResult pattern** : `LoginAsync` retourne un objet `LoginResult` (Token + IsLockedOut) au lieu de `string?` pour permettre au controller de distinguer 401 (mauvais identifiants) de 429 (compte bloque).
- **Retry-After** : Header ajoute via `OnRejected` callback (valeur fixe 60s). Le `RejectionStatusCode` par defaut est 503, pas 429 — doit etre configure explicitement ou gere dans `OnRejected`.
- **Sources** : [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit), [OWASP Credential Stuffing Prevention](https://cheatsheetseries.owasp.org/cheatsheets/Credential_Stuffing_Prevention_Cheat_Sheet.html).
- **Etat** : DONE — BACK-002, 3 tests d'integration. Logs des tentatives bloquees reportes a BACK-010 (Serilog).

### DEC-023 : CORS dynamique via appsettings + fail fast au demarrage
- **Statut** : ✅ ACTIVE
- **Date** : Avril 2026
- **Choix** : Externaliser les origines CORS dans `appsettings.json` (`Cors:AllowedOrigins` array) au lieu d'un string hard-code. Resserrer les permissions : `WithHeaders("Content-Type")` au lieu de `AllowAnyHeader()`, `WithMethods("GET", "POST", "PUT", "DELETE")` au lieu de `AllowAnyMethod()`. Validation au demarrage qui leve une exception si la config est manquante.
- **Pourquoi** : En production, le frontend sera sur un autre domaine que `localhost:5110`. Le hard-coding empechait tout deploiement. L'array permet plusieurs origines (ex: `https://<your-domain>` + `https://www.<your-domain>`). Resserrer les methods/headers reduit la surface d'attaque (principe du moindre privilege).
- **`Authorization` non whiteliste** : L'authentification passe par le cookie `authCookie` (envoye automatiquement via `AllowCredentials()`), pas par un header `Authorization: Bearer`. Pas besoin de l'autoriser explicitement.
- **`OPTIONS` non liste dans `WithMethods`** : Les requetes preflight sont gerees automatiquement par le middleware CORS — l'ajouter manuellement est redondant et peut causer des conflits (doc Microsoft).
- **Fail fast** : Si `Cors:AllowedOrigins` est absent ou vide au demarrage → `InvalidOperationException`. Mieux vaut crasher avec un message clair que tourner avec un CORS mal configure.
- **Sources** : [ASP.NET Core CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors), [MDN CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CORS).
- **Etat** : DONE — BACK-003, 3 tests d'integration.

### DEC-024 : Pas de token anti-CSRF (protection par SameSite=Strict + CORS)

- **Statut** : ✅ ACTIVE
- **Date** : Mai 2026
- **Choix** : MemoRecipe ne met PAS en place de token anti-CSRF dédié.
- **Pourquoi** : La combinaison **cookie `SameSite=Strict`** (DEC-014) + **CORS whitelist stricte** (DEC-023) couvre déjà l'attaque CSRF par deux barrières indépendantes :
  - Le navigateur n'envoie pas le cookie `authCookie` si la requête vient d'un autre site (`SameSite=Strict`)
  - Même si le cookie passait, l'API rejette les `Origin` non whitelistées (CORS)
- **Sources** : [OWASP CSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html), [MDN SameSite cookies](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie/SameSite)
- **Conditions qui invalideraient ce choix** :
  - Passer à `SameSite=Lax` (cookie envoyé en GET cross-site) → token anti-CSRF requis
  - Acceptation d'origines partenaires (CORS plus permissif) → token anti-CSRF requis
  - Schéma d'auth sans cookie → réévaluer
- **État** : DONE — choix conscient, à réévaluer si une des conditions ci-dessus devient vraie.

### DEC-025 : Retrait du support WebP (Tesseract Windows sans libwebp)

- **Statut** : ✅ ACTIVE. WebP est refusé quel que soit le provider actif (Vision ou OCR fallback), le blocage est appliqué au niveau du contrôleur API. L'assouplissement pour le path Vision est tracé dans un ticket post V1.
- **Date** : 22 mai 2026
- **Choix** : MemoRecipe **ne supporte pas** le format WebP pour l'upload de recettes scannées. Seuls **JPG/JPEG et PNG** sont acceptés.
- **Pourquoi** :
  - L'installeur Tesseract-OCR Windows par défaut **n'inclut pas le support `libwebp`** dans le composant Leptonica utilisé pour le décodage des images.
  - Conséquence runtime observée : `Error in pixReadMemWebP: function not present` → `System.IO.IOException: Failed to load image from memory.` au moment de `Tesseract.Pix.LoadFromMemory(...)` pour toute image WebP.
  - Trois options ont été considérées (cf. BACK-051 + BACK-039) :
    - **Option A — Recompiler Tesseract avec `libwebp`** : complexifie le déploiement (Docker, CI/CD), crée une dépendance fragile et environnement-spécifique difficile à reproduire entre dev / CI / prod.
    - **Option B — Conversion serveur WebP → PNG avant Tesseract** (via `ImageSharp` ou `SkiaSharp`) : ajoute une dépendance NuGet et un overhead perf (~50-200 ms par image). Solution propre mais ajoute une couche de code à maintenir et tester.
    - **Option C — Retirer WebP du périmètre supporté** *(choix retenu)* : KISS, alignement avec ce que Tesseract sait lire nativement, moins de surface d'attaque, code plus simple à maintenir, pas de dépendance supplémentaire.
  - **Argument pragmatique MVP** : la valeur métier de WebP est marginale face à JPG/PNG (formats majoritaires dans le partage de recettes — appareils photo, exports Photoshop par défaut, WhatsApp, blogs culinaires). Décision **réversible** plus tard sans contrainte forte.
- **Sources** :
  - [Tesseract InputFormats](https://tesseract-ocr.github.io/tessdoc/InputFormats.html) — formats nativement supportés
  - Logs Function : `Error in pixReadMemWebP: function not present` (observé pendant BACK-051, 22/05/2026)
  - [OWASP File Upload Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html) — principe de whitelist stricte des formats supportés
- **Conséquences** :
  - 3 fichiers modifiés : `RecipeController.cs` (extensions + MIME + magic bytes), `ScanRecipe.razor` (attribut `Accept`), `README.md` (section defense in depth)
  - BACK-039 mis à jour pour porter la **future ré-introduction du WebP** (Option B recommandée à terme — conversion serveur, dépendance unique vs build système custom)
  - Aucune régression pour les utilisateurs actuels (le scan n'avait jamais réellement fonctionné avec WebP en l'absence de libwebp)
- **Conditions qui invalideraient ce choix** :
  - Migration vers un build de Tesseract avec `libwebp` (ex : image Docker custom Linux, package alternatif maintenu)
  - Besoin utilisateur fort exprimé après mise en production (feedback récurrent "je n'arrive pas à uploader mon image")
  - Apparition d'une bibliothèque .NET de conversion WebP→PNG mature et low-overhead (changement du calcul coût/bénéfice de l'Option B)
- **État** : DONE — choix conscient, à réévaluer si une des conditions ci-dessus devient vraie.

- **Mise à jour post-[DEC-043](#dec-043) et vérification 20/08/2026 (scope élargi 21/08/2026)** : le pivot vers un provider Vision multimodal ouvre techniquement le support WebP, car Mistral Vision accepte nativement ce format. Cependant, le blocage WebP reste appliqué au niveau du contrôleur API (`RecipeController.CreateScannedRecipe`), qui filtre les uploads via une whitelist stricte JPEG et PNG (extensions, MIME types, magic bytes). Le support WebP end-to-end nécessite deux évolutions coordonnées : l'assouplissement de la whitelist contrôleur, et la mise en place d'une conversion serveur WebP vers PNG pour le path fallback OCR Tesseract (qui ne peut pas décoder WebP sans libwebp). Ces évolutions sont consolidées dans [BACK-105](../documentation/BACKLOG.md#back-105) "Support formats étendus uploads (WebP + PDF + HEIC + refacto utilitaire `IFileUploadValidator`)" post V1, scope élargi le 21/08/2026 pour couvrir en une seule passe WebP + PDF + HEIC + le refacto de la validation vers un utilitaire réutilisable (fusion avec US-A2-08 suspendue et US-B1-04 suspendue pour cohérence). En attendant l'implémentation de BACK-105, la DEC-025 reste strictement active, WebP est refusé quel que soit le provider actif.

### DEC-026 : Migration AutoMapper → Mapperly (source generator, OSS MIT, mappers statiques)

- **Statut** : ✅ ACTIVE
- **Date** : 23 mai 2026
- **Choix** : MemoRecipe abandonne **AutoMapper** au profit de **Mapperly** (`Riok.Mapperly`, OSS MIT), avec une approche **mappers statiques** plutôt que l'instanciation + injection DI traditionnelle.
- **Pourquoi** :
  - **Changement de licence AutoMapper** : depuis fin 2024 / début 2025, AutoMapper (créé par Jimmy Bogard en 2008, OSS depuis 17 ans) est passé sous licence commerciale **Lucky Penny Software**. Warning au build : `You do not have a valid license key for the Lucky Penny software AutoMapper. This is allowed for development and testing scenarios. If you are running in production you are required to have a licensed version.` → bloquant pour la prod sans achat de licence (~$300/an).
  - **Trois options évaluées** (cf. BACK-046) :
    - **Option A — Acheter licence Lucky Penny** : 0 code à toucher, mais ~$300/an + dépendance commerciale + mauvais signal sur un projet perso d'apprentissage.
    - **Option B — Downgrade vers AutoMapper v13 (dernière OSS)** : gratuit mais **dette technique** (version morte, plus de fixes sécurité). À éviter.
    - **Option C — Migrer vers Mapperly** *(choix retenu)* : OSS MIT, **source generator** (mappings générés à la compilation, zéro reflection runtime, 30-50× plus rapide), erreurs détectées à la compilation, apprentissage d'un outil moderne .NET.
  - **Style "mappers statiques" plutôt que DI** : Mapperly est conçu pour être appelé directement via `RecipeMapper.ToDto(recipe)` sans injection. Avantages : pas de DI à configurer, pas d'interfaces à créer, services simplifiés (plus de `private readonly IMapper _mapper;`). Les tests utilisant `FakeRepository` (pas Moq) ne mockent jamais le mapper de toute façon — pas de perte de testabilité.
- **Sources** :
  - [Mapperly GitHub (riok/mapperly)](https://github.com/riok/mapperly) — OSS MIT, maintenu actif
  - [Annonce Lucky Penny / AutoMapper commercial](https://www.jimmybogard.com/automapper-and-mediatr-going-commercial/)
  - [Comparaison perf AutoMapper vs Mapperly (benchmarks)](https://mapperly.riok.app/docs/intro/)
  - [OWASP A03:2025 Software Supply Chain Failures](https://owasp.org/Top10/2025/A03_2025-Software_and_Data_Integrity_Failures/) — vendor lock-in OSS comme risque
- **Conséquences** :
  - **5 profiles** à réécrire (UserProfile, RecipeProfile, CategoryProfile, IngredientProfile, StepProfile) en classes statiques partielles avec `[Mapper]` attribute
  - **2 services** à simplifier (`AuthService`, `RecipeService`) — retrait du paramètre `IMapper mapper` dans le constructeur + appels directs `XxxMapper.ToDto(...)`
  - **`Program.cs`** : retrait de `builder.Services.AddAutoMapper(...)` (Mapperly ne nécessite pas d'enregistrement DI en mode statique)
  - **2 csproj** : retrait du `PackageReference Include="AutoMapper"`, ajout de `PackageReference Include="Riok.Mapperly"`
  - **Gains attendus** : warning licence parti, perf mapping ~30-50× plus rapide, erreurs typo détectées à la compilation (build cassé) au lieu du runtime (`AutoMapperMappingException`)
  - **Bonus pédagogique** : découverte des **source generators** .NET (concept moderne très valorisé en entretien — utilisés aussi par System.Text.Json, Serilog source-gen, etc.)
- **Conditions qui invalideraient ce choix** :
  - **Mapperly devient commercial** lui aussi (peu probable, OSS MIT avec gouvernance communautaire — mais on a un précédent récent avec AutoMapper)
  - **Besoin de mock dynamique du mapper** dans les tests (ex : passage à Moq) → revenir au pattern instance + interface (Style 2). Aujourd'hui non pertinent : tests via `FakeRepository`.
  - **Émergence d'un nouveau standard** dans l'écosystème .NET pour le mapping (ex : feature native EF Core ou primitive de runtime) → réévaluer.
- **État** : APPLIQUÉ via **BACK-046** (mergé). Migration AutoMapper → Mapperly terminée : 5 profiles convertis en static partial classes (`UserMapper`, `RecipeMapper`, `IngredientMapper`, `StepMapper`, `CategoryMapper`), 2 services simplifiés (`AuthService`, `RecipeService` — retrait du paramètre `IMapper` du constructeur), `Program.cs` nettoyé (`AddAutoMapper` retiré, pas de DI à enregistrer pour Mapperly statique), packages NuGet swappés. Convention `[MapperIgnoreSource]` / `[MapperIgnoreTarget]` explicite préférée à `RequiredMappingStrategy.None` (cf. feedback projet : meilleure documentation des exclusions). Warning licence AutoMapper supprimé, perf mapping 30-50× plus rapide, erreurs typo désormais détectées au build. **Aucun rollback nécessaire à ce jour**, l'expérience est positive.


### DEC-027 : nginx:alpine pour servir le Blazor WASM (au lieu d'aspnet runtime)

- **Statut** : ✅ ACTIVE
- **Date** : 28 mai 2026
- **Choix** : Le Dockerfile du Frontend Blazor WASM utilise **`nginx:alpine`** au stage runtime, **pas** `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (qu'on utilise côté API).
- **Pourquoi** :
  - **Blazor WASM = SPA statique** : le résultat de `dotnet publish` produit un dossier `wwwroot/` contenant uniquement des fichiers statiques (`index.html`, `_framework/` avec le bundle WASM, CSS, JS, images). Le navigateur télécharge ces fichiers et **exécute le WebAssembly côté client**. **Aucun runtime .NET n'est nécessaire côté serveur**.
  - Embarquer `aspnet:10.0-alpine` (~150 MB) juste pour servir des fichiers statiques = gâchis : 100% du runtime .NET inutilisé.
  - **`nginx:alpine`** (~40 MB) est conçu pour ça : **4× plus léger**, performances imbattables sur le statique, optimisé pour des dizaines de milliers de connexions concurrentes, configuration simple via fichiers `.conf`.
  - **Trade-off** : on perd la possibilité de servir des assets dynamiques côté serveur (SSR, middleware), mais c'est inapplicable au modèle Blazor WASM (tout est client-side).
- **Sources** :
  - [Blazor WebAssembly hosting & deployment (Microsoft)](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly) — confirme que tout serveur HTTP statique convient
  - [nginx official Docker image](https://hub.docker.com/_/nginx) — image officielle, mainline branch, scans CVE réguliers
  - Fiche [DOCKERFILE-CHEATSHEET.md](fiches/DOCKERFILE-CHEATSHEET.md) section Partie 2 — détail technique
- **Conséquences** :
  - **`nginx.conf` requis** dans le projet Frontend pour gérer le **SPA routing fallback** (`try_files $uri $uri/ /index.html =404`) sans lequel un F5 sur une route interne (`/recipes/abc`) renvoie un 404 nginx
  - **Pas d'`ENV ASPNETCORE_ENVIRONMENT=Production`** côté Frontend : nginx n'est pas un runtime .NET, et le mode Production est figé dans le bundle au moment du publish (`-c Release`)
  - **Port exposé = 80** (convention nginx), pas 8080 comme côté API
  - **Pas d'`ENTRYPOINT` à définir** : l'image officielle nginx lance nginx en foreground par défaut (container-compatible)
  - **Image finale ~40 MB** (vs ~150 MB avec aspnet) — gain net 110 MB par image. À l'échelle d'un CI/CD ou d'un registry, c'est significatif (bandwidth, storage, pull time)
  - Optimisations prod nginx (gzip avancé, cache headers immutables sur assets hashés, security headers) tracées dans **BACK-054** pour application juste avant le déploiement
  - **Frontend non impacté par DEC-030** (Container Support natif SDK .NET, scope API uniquement) : le Frontend Blazor WASM garde son `Dockerfile` nginx custom — le SDK .NET ne sait pas générer une image avec nginx comme runtime
- **Conditions qui invalideraient ce choix** :
  - **Passage à Blazor Server** ou **Blazor United/SSR** : ces modèles nécessitent un runtime .NET côté serveur. Il faudrait revenir à `aspnet:10.0-alpine`.
  - **Besoin de middleware/API routes côté serveur** dans le même container (ex: BFF pattern). Mais c'est mieux d'avoir une API séparée (déjà notre cas).
  - **Migration vers Caddy** (alternative à nginx avec HTTPS auto via Let's Encrypt) : à considérer au moment de BACK-009 si on veut simplifier la chaîne TLS, mais nginx reste la baseline.
- **État** : DÉCIDÉ et appliqué le 28/05/2026 (BACK-007 partie 2, PR #13). Renforcement anti-fingerprinting ajouté le 01/08/2026 (PR #43) : directive `server_tokens off;` ajoutée dans `nginx.conf` pour masquer la version nginx dans les réponses HTTP et pages d'erreur. Cohérent avec le hardening Kestrel côté API (voir [DEC-056](#dec-056)).


### DEC-028 : Frontend ↔ API via reverse proxy nginx (Option B), pas de CORS exposé

- **Statut** : ✅ ACTIVE
- **Date** : 29 mai 2026
- **Choix** : Pour la composition prod (BACK-007 partie 3), le nginx du container Frontend **proxifie `/api/*`** vers le container API en interne au réseau Docker (`proxy_pass http://api:8080/api/`). L'API n'est **pas exposée** publiquement. Le bundle Blazor WASM utilise une **URL relative `/api/...`** (même origine), donc **zéro CORS** en prod.
- **Pourquoi** :
  - **Surface d'attaque réduite** : l'API n'écoute qu'en interne au réseau Docker, jamais joignable depuis Internet. Vis-à-vis OWASP A05:2025 (Security Misconfiguration), c'est la posture la plus restrictive.
  - **Simplicité TLS** : 1 seul certificat HTTPS pour le sous-domaine `app.<your-domain>` (Apache du host + Let's Encrypt via BACK-009), au lieu de 2 certificats pour 2 sous-domaines (`api.` + `app.`).
  - **Same-origin** : `SameSite=Strict` sur les cookies HttpOnly (DEC-024 CSRF) fonctionne parfaitement parce que le Frontend et l'API partagent l'origine. Pas de bidouille `credentials: include` cross-origin.
  - **Bundle WASM universel** : un seul build `dotnet publish -c Release` fonctionne en dev local (avec override `appsettings.Development.json`) ET en prod (URL relative via `HostEnvironment.BaseAddress`). Pas de rebuild par environnement.
  - **Pattern standard prod** : architecture SPA + API derrière un même reverse proxy = pratique recommandée chez la majorité des déploiements modernes (Caddy, Traefik, nginx).
- **Alternative considérée — Option A (Frontend appelle API en cross-origin)** :
  - L'API serait exposée sur `api.<your-domain>` avec son propre certificat
  - CORS à configurer (déjà partiellement fait dans BACK-002 + BACK-023)
  - Cookies HttpOnly cross-origin = trade-off `SameSite=None; Secure` + `credentials: include` partout
  - Rebuild WASM par environnement (URL `api.<your-domain>` dans le bundle compilé)
  - **Rejetée** : plus de complexité, plus de surface d'attaque, pas d'avantage compensatoire.
- **Sources** :
  - [Mozilla — Same-origin policy & CORS](https://developer.mozilla.org/en-US/docs/Web/Security/Same-origin_policy)
  - [OWASP A05:2025 — Security Misconfiguration](https://owasp.org/Top10/2025/A05_2025-Security_Misconfiguration/)
  - [Blazor WASM hosting models (Microsoft)](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly)
  - DEC-024 — CSRF protection via SameSite=Strict + strict CORS (cette décision renforce DEC-024)
- **Conséquences** :
  - **Modifs Frontend** : 3 fichiers — `wwwroot/appsettings.json` (`ApiBaseUrl: ""`), `wwwroot/appsettings.Development.json` (`ApiBaseUrl: "http://localhost:5131/"` pour le dev `dotnet watch`), `Program.cs` (lecture depuis config avec fallback sur `HostEnvironment.BaseAddress`)
  - **Modif `nginx.conf`** : ajout d'un bloc `location /api/ { proxy_pass http://api:8080/api/; ... }` avant le `location /` existant
  - **`docker-compose.prod.yml`** : API et Postgres utilisent `expose:` (interne) au lieu de `ports:` (mappé sur host)
  - **API CORS config** : peut être supprimée en prod (origins vide) puisqu'il n'y a plus de cross-origin. **À garder en dev** pour le mode `dotnet watch`.
  - **Reverse proxy edge du host** (Apache/nginx/Caddy selon le setup) : ProxyPass de l'origine publique HTTPS vers le loopback du container Frontend nginx en interne au host. Permet la cohabitation propre avec d'autres sites éventuels hébergés sur le même host.
- **Conditions qui invalideraient ce choix** :
  - **L'API devient consommée par d'autres clients que le Frontend Blazor** (ex: mobile MAUI futur appelant directement, partenaires externes, microservices) → là `api.<your-domain>` sous-domaine séparé + CORS strict devient pertinent. Mais le Frontend Web pourrait continuer en Option B en parallèle.
  - **Découplage Frontend / API souhaité** pour les déployer séparément (versions différentes, ratios de scaling différents) → 2 containers ≠ même origine.
- **État** : **DÉCIDÉ le 29/05/2026 et APPLIQUÉ le 01/06/2026** (BACK-007 partie 3, PR #14). Validé en E2E local : bundle Blazor WASM appelle `/api/*` en same-origin via nginx reverse proxy, **zéro CORS error** dans la console DevTools.


### DEC-029 : Compose security baseline — phasage volontaire du hardening avancé

- **Statut** : 🟡 PARTIELLEMENT SUPERSEDED sur un trade-off, ACTIVE sur le reste. La baseline compose (isolation réseau, `no-new-privileges`, `mem_limit`, `cpus`, healthchecks) reste intégralement en vigueur. En revanche, le trade-off "pas de Docker secrets natifs, env vars suffisent" listé dans les alternatives deferred a été superseded par BACK-004 (30/07/2026), qui a introduit le mécanisme `secrets:` top-level dans `docker-compose.prod.yml` + `AddKeyPerFile("/run/secrets")` côté API. Les secrets sensibles (POSTGRES_PASSWORD, JWT Secret, ConnectionString, OcrScan BaseUrl, Telegram BotToken et ChatId) sont désormais montés en fichier plutôt qu'en variable d'environnement. BACK-057 (backup Postgres) mentionné ici comme "à faire" est également résolu, tracé par [DEC-038](#dec-038).
- **Date** : 31 mai 2026
- **Choix** : Le `docker-compose.prod.yml` (BACK-007 partie 3) implémente une **posture sécu baseline solide** (network isolation, `security_opt: no-new-privileges`, `mem_limit` + `cpus`, healthchecks + `depends_on: service_healthy`, secrets via `env_file`) **mais diffère volontairement** 3 mesures de hardening avancées (`read_only: true` filesystems, `cap_drop: ALL` + `cap_add` minimal, `user:` non-root explicit) tracées dans **BACK-056**.
- **Pourquoi** :
  - **Postgres en particulier** nécessite plusieurs Linux capabilities (`CHOWN`, `SETUID`, `SETGID`, `DAC_OVERRIDE`, `FOWNER`, `FSETID`) et l'accès en écriture à `/var/run/postgresql` + `/tmp`. Configurer `cap_drop: ALL` + `cap_add: [...]` + `read_only: true` + `tmpfs: [...]` proprement demande du **tuning fin par image** qui peut casser au moindre upgrade Postgres.
  - **Phasage > perfection** : avoir une baseline solide validée et fonctionnelle MAINTENANT vaut mieux que chercher la perfection trop tôt et risquer de casser le service en production. **Hardening incrémental** = méthode pro standard.
  - **Trade-off conscient** : la baseline actuelle ferme déjà 80% de la surface d'attaque (isolation réseau, anti-escalade, anti-DoS). Les 20% restants nécessitent du temps qui n'est pas critique au stade portfolio.
- **Autres trade-offs deferred dans cette même PR** (mentionnés pour traçabilité) :
  - **Pas de TLS intra-network** (HTTP entre `web` et `api` dans le réseau Docker `backend`) : acceptable car même host, attaquer le bus interne demanderait déjà d'avoir compromis le host. mTLS = overkill pour notre cas.
  - **Pas de Docker secrets** natifs (`secrets:` mechanism qui monte les secrets en fichier `/run/secrets/x` au lieu d'env vars) : env vars suffisent pour un compose simple. Le mécanisme `secrets:` brille en Docker Swarm / Kubernetes où il est intégré au scheduler. Pas pertinent ici.
  - **Pas d'images distroless** (au lieu d'Alpine) : Alpine ~50 MB déjà très léger. Distroless ~20 MB mais zéro shell = très complexe à debugger en cas de pb prod. Marginal gain vs cost.
- **Sources** :
  - [Docker Compose hardening guide (OWASP)](https://cheatsheetseries.owasp.org/cheatsheets/Docker_Security_Cheat_Sheet.html)
  - [Postgres official Docker image security recommendations](https://hub.docker.com/_/postgres)
  - [no-new-privileges security_opt (Docker docs)](https://docs.docker.com/reference/compose-file/services/#security_opt)
- **Conséquences** :
  - **Sécu actuelle** : ~7/10 pour un projet portfolio learning, ~6/10 pour une app SaaS B2B moyenne, ~4/10 pour fintech/santé (où il faudrait BACK-056 + BACK-057 + BACK-058 + BACK-059 + compliance).
  - **Pitch entretien clair et défendable** : "j'ai construit le compose en couches sécu — baseline d'abord, hardening avancé tracé pour itération suivante. Phasage évite de casser le service en cherchant la perfection trop tôt."
  - **Tickets dédiés créés** : BACK-056 (advanced hardening), BACK-057 (backup auto Postgres), BACK-058 (logs centralisés), BACK-059 (monitoring Prometheus+Grafana) — pour rendre explicite ce qui manque et le tracker comme dette technique consciente.
- **Conditions qui invalideraient ce choix** :
  - **Passage à un domaine régulé** (santé, finance, gov) où le hardening avancé devient obligation légale → faire BACK-056 immédiatement.
  - **Incident de sécurité** sur un projet similaire qui aurait été évité par read_only / cap_drop → revoir la priorité.
  - **Disponibilité d'un orchestrateur** (Docker Swarm, Kubernetes) qui intègre natement Docker secrets / Pod security policies → migrer vers ces mécanismes.
- **État** : DÉCIDÉ et appliqué le 31/05/2026 (BACK-007 partie 3, PR #14 mergée le 01/06/2026).


### DEC-030 : Container Support natif SDK .NET pour la generation de l'image API

- **Statut** : ✅ ACTIVE. Container Support SDK .NET reste le mode de génération de l'image API en prod (pas de Dockerfile). Précision sur l'évolution du dev workflow : la conséquence initialement listée "docker-compose.yml (dev) : le service `api` passe à `image: memorecipe-api:dev`" n'est plus d'actualité. Aujourd'hui le dev compose (`docker-compose.yml` à la racine du repo) contient Postgres, pgAdmin et le worker IA en container Linux ([DEC-061](#dec-061), résout le gap dev/prod sur les libs natives Tesseract libwebp). L'API en dev tourne directement via `dotnet run` (ou `dotnet watch`) sur la machine hôte, sans passer par Docker, ce qui simplifie le cycle de dev (hot reload natif, debug direct). Le pattern Container Support SDK reste utilisé pour la génération de l'image API prod poussée sur GHCR (voir [DEC-031](#dec-031)).
- **Date** : 04 juin 2026
- **Choix** : Pour le projet `MemoRecipe.Api`, abandon du `Dockerfile` manuel au profit du **Container Support natif intégré au SDK .NET 7+** (cible MSBuild `PublishContainer`). L'image API est désormais générée via `dotnet publish --os linux --arch x64 /t:PublishContainer`, avec la configuration en properties MSBuild dans le `.csproj` (`<ContainerBaseImage>`, `<ContainerRepository>`, `<ContainerImageTag>`, `<ContainerUser>`, `<ContainerPort>`).
- **Pourquoi** :
  - **Suggestion du mentor (retour LinkedIn 02/06/2026, cf. fiche MENTORING-RETOURS.md)** : ".Net 10, tu peux te passer des Dockerfile, c'est directement intégré dans les csproj maintenant et dans le SDK .net."
  - **Cohérence automatique avec le SDK** : la base image (`mcr.microsoft.com/dotnet/aspnet:10.0-alpine`) suit la version du SDK installée. Plus de risque de désynchronisation Dockerfile / SDK lors des upgrades.
  - **Sécurité baked-in** : Container Support SDK applique les bonnes pratiques par défaut (utilisateur non-root via `<ContainerUser>`, layers optimisées, minimal attack surface).
  - **Maintenabilité** : ~5 lignes XML dans le `.csproj` remplacent ~30 lignes de Dockerfile multi-stage. Moins de code = moins de bugs potentiels.
  - **Layer caching automatique** : le SDK gère le découpage en layers (OS / runtime / NuGet deps / code app) sans configuration manuelle.
  - **Validation en visio mentor 04/06/2026** : le mentor confirme que le résultat reste une image Docker standard, donc l'orchestration Compose (et le déploiement en prod) est inchangée — c'est uniquement la "recette" qui passe du Dockerfile vers le `.csproj`.
- **Scope** :
  - **S'applique à** : `MemoRecipe.Api` uniquement (projet .NET 10).
  - **Ne s'applique PAS au Frontend Blazor WASM** : le Frontend utilise `nginx:alpine` comme runtime (cf. DEC-027), pas un runtime .NET. Container Support SDK ne sait pas générer une image avec nginx comme entrypoint. Le Dockerfile custom Frontend est conservé.
- **Alternative considérée — Garder le Dockerfile multi-stage existant** :
  - Avantage : aucune migration, code stable connu.
  - Inconvénient : ~30 lignes à maintenir manuellement, version base image hardcodée (drift vs SDK installé), pas de bénéfice à l'effort de maintenance.
  - **Rejetée** : la migration est mécanique et apporte une simplification durable.
- **Sources** :
  - [.NET SDK Container Building (docs Microsoft)](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container)
  - [SDK Containers — properties MSBuild de customisation](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container#customizing-the-container-image)
  - Retour mentor 02/06/2026 + visio 04/06/2026 (cf. fiches/MENTORING-RETOURS.md)
- **Conséquences** :
  - **`memorecipe-api.csproj`** enrichi des properties `<ContainerBaseImage>`, `<ContainerRepository>`, `<ContainerImageTag>`, `<ContainerUser>`, `<ContainerPort>`, etc. (Note : `<ContainerImageName>` est **obsolète** depuis le SDK .NET 10.0 — remplacé par `<ContainerRepository>`, warning CONTAINER003 à l'utilisation.)
  - **`Dockerfile` de l'API supprimé** du repo.
  - **`docker-compose.yml` (dev)** : le service `api` passe de `build: ./...` à `image: memorecipe-api:dev`. Workflow dev : `dotnet publish /t:PublishContainer` avant `docker compose up -d`.
  - **`docker-compose.prod.yml`** : le service `api` passe de `build:` à `image: ghcr.io/<user>/memorecipe-api:<tag>` (cf. DEC-031 pour le workflow registry).
  - **Frontend non impacté** : DEC-027 reste valide (Dockerfile nginx custom conservé).
  - **Pré-requis pour DEC-032 (Aspire)** : Aspire utilise Container Support SDK en interne pour les projets .NET. Cette décision doit être appliquée avant l'étape Aspire.
- **Conditions qui invalideraient ce choix** :
  - **Customisation OS poussée** non supportable par les properties MSBuild (installation de paquets système custom, configuration noyau, dépendances natives complexes) → repasser à un Dockerfile.
  - **Build multi-architecture complexe** non couvert par `<ContainerRuntimeIdentifiers>` → repasser à un Dockerfile + buildx.
  - **Retrait du Container Support du SDK** (improbable, fonctionnalité officielle Microsoft) → repasser à un Dockerfile.
- **État** : DÉCIDÉ le 04/06/2026 (visio mentor). À implémenter dans **BACK-063** (étape 1A).


### DEC-031 : Distribution des images via GitHub Container Registry (GHCR) en prod

- **Statut** : ✅ ACTIVE
- **Date** : 04 juin 2026 (visio mentor) + analyse comparative post-visio
- **Choix** : Les images Docker du projet (API et Frontend) sont **buildées en local sur le poste dev**, **pushées vers GHCR (GitHub Container Registry)** taguées avec une version sémantique, puis **pullées depuis le VPS Cloud** au moment du déploiement. Le `docker-compose.prod.yml` utilise `image: ghcr.io/<user>/memorecipe-api:<tag>` au lieu de `build:`. (Option A retenue contre Option B "installer SDK .NET sur le VPS".)
- **Pourquoi** :
  - **VPS partagé** : le VPS Cloud héberge aussi d'autres sites en parallèle. Installer le SDK .NET dessus (alternative Option B) serait invasif (paquets système ~600 MB + maintenance des versions SDK) et augmenterait la surface d'attaque. Option A préserve la coexistence.
  - **Build sur dev = principe pro standard** : on ne build pas sur le serveur de prod. Le serveur de prod doit juste **exécuter** des artefacts pré-construits et validés. Build CPU-intensive en dev → pas de risque de ralentir les autres services du VPS pendant un déploiement.
  - **Rollback rapide** : `docker compose pull memorecipe-api:v1.0.4 && docker compose up -d` permet de revenir à une version précédente en ~30 secondes, atomiquement. Alternative Option B demanderait `git checkout + rebuild + restart` (~5-10 min, plus risqué).
  - **Reproductibilité parfaite** : un tag d'image (`v1.0.5`) est **immuable**. La même image tourne en dev, en pré-prod (futur), et en prod. Plus de "ça marche chez moi" lié à la version du SDK installée localement.
  - **Versionning natif** : les tags sémantiques (`v1.0.5`, `latest`, `staging`) offrent une gestion de versions explicite sans tooling additionnel.
  - **CI/CD future facilitée (BACK-008)** : GitHub Actions peut push directement à GHCR via `GITHUB_TOKEN` (5 lignes de config). Alternative Option B demanderait SSH depuis CI vers le VPS = clé privée à sécuriser = friction.
  - **Sécurité** : code source jamais déposé sur le VPS. GHCR scanne automatiquement les images pour vulnérabilités (Dependabot intégré).
  - **Cohérence avec DEC-032** : Aspire (étape 2) réutilisera GHCR comme registry cible via `aspire publish --registry ghcr.io`. Décision compatible avec roadmap.
- **Pourquoi GHCR plutôt que Docker Hub** :
  - **Repos publics illimités** + **pulls illimités** (Docker Hub limite à 100 pulls/6h en anonyme, 200 en compte gratuit).
  - **Authentification native GitHub** via `GITHUB_TOKEN` — pas de compte séparé à créer/maintenir.
  - **Intégration GitHub** : packages visibles sur la page Packages du repo, lien direct au code source, releases.
  - **Docker Hub free** : limité à 1 seul repo privé, friction si on veut faire évoluer le projet.
- **Alternative considérée — Option B : installer SDK .NET sur le VPS** :
  - Workflow : `git pull` sur VPS + `dotnet publish /t:PublishContainer` sur VPS + `docker compose up -d`.
  - Avantage : pas besoin de registry.
  - Inconvénients : SDK à installer/maintenir sur VPS partagé, code source exposé sur VPS, build CPU sur prod (risque de ralentir les autres services hébergés), rollback lent (rebuild), pas de versionning natif, anti-pattern (build sur prod).
  - **Rejetée** : invasive sur le VPS partagé + plusieurs anti-patterns prod.
- **Sources** :
  - [GitHub Container Registry — docs officielles](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
  - [.NET SDK Containers — push vers un registry](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container#publish-the-container-image-to-a-container-registry)
  - Analyse comparative Option A vs B documentée dans fiche MENTORING-RETOURS.md (section visio 04/06/2026)
- **Conséquences** :
  - **Création d'un compte GHCR** (depuis le compte GitHub existant) + génération d'un PAT (Personal Access Token) avec scope `write:packages` pour push depuis le poste dev.
  - **Authentification Docker locale** : `docker login ghcr.io -u <user> -p $GHCR_TOKEN` (token stocké dans le password manager, jamais en clair dans le repo).
  - **Workflow déploiement** : `dotnet publish /t:PublishContainer /p:ContainerRegistry=ghcr.io /p:ContainerImageTag=<version>` génère + push en une commande.
  - **`docker-compose.prod.yml`** : services utilisent `image: ghcr.io/<user>/memorecipe-api:<tag>` (plus de `build:`).
  - **VPS** : doit pouvoir s'authentifier à GHCR pour pull (token read-only via `read:packages`). Pour les repos publics, pas d'auth nécessaire.
  - **Premier push plus lent** (image complète ~150-300 MB, ~2-3 min en fibre), **pushs incrémentaux rapides** (~5-20 MB delta, ~10-30 sec) grâce au layer caching Docker.
  - **Tag = version sémantique** (`v1.0.5`) pour rollback explicite + `latest` mis à jour à chaque release stable.
  - **Décision finale prise en solo après la visio** (le mentoring s'étant arrêté à 1 session). Traçabilité de l'analyse comparative conservée dans MENTORING-RETOURS.md pour relecture future.
- **Conditions qui invalideraient ce choix** :
  - **Volonté de quitter GitHub** comme plateforme principale du projet → migrer vers Docker Hub, Azure Container Registry, ou self-hosted (Harbor).
  - **Besoin d'un registry privé en self-hosted** (compliance, on-premise, isolation réseau) → migrer vers un registry custom.
  - **Évolution des quotas GHCR** (improbable au volume actuel — repos publics gratuits illimités) → réévaluer.
- **État** : APPLIQUÉ le 17/06/2026 via **BACK-064** (PR #18 mergée). Workflow opérationnel : `dotnet publish /t:PublishContainer` pour l'API (push direct via Container SDK) + `docker build && docker push` pour le Frontend. Procédure complète documentée dans [`documentation/DEPLOYMENT.md`](DEPLOYMENT.md). Test E2E local validé (pull GHCR + compose up + auth fonctionnelle). Application sur VPS Cloud prévue dans **BACK-007 partie 3**.


### DEC-032 : .NET Aspire (Option B) pour orchestration du stack dev + prod

- **Statut** : 🔵 ENVISAGÉ (spike V2 tracé BACK-065). La décision d'adopter Aspire a été prise le 04/06/2026 mais l'implémentation (spike BACK-065) n'a jamais été lancée sur le sprint Alpha.1 ni Alpha.2 pour des raisons de priorisation produit (les tickets P0 CI/CD, RGPD, backup, sécurité IA ont consommé le temps disponible). Le stack actuel utilise toujours `docker-compose.yml` classique pour le dev (Postgres + pgAdmin) et `docker-compose.prod.yml` pour la prod, sans AppHost Aspire. Aucun projet `MemoRecipe.AppHost` ni `MemoRecipe.ServiceDefaults` n'existe dans la solution à ce jour. La décision reste conceptuellement pertinente et sera réévaluée en V2 selon la charge produit et la valeur ajoutée observée après stabilisation V1.
- **Date** : 04 juin 2026 (visio mentor)
- **Choix** : Adoption de **.NET Aspire** en **étape 2** (après que Container Support SDK DEC-030 soit en place) pour décrire et orchestrer le stack MemoRecipe (Postgres + API + Frontend + reverse proxy nginx). **Option B retenue** : décrire le **maximum** dans l'AppHost C# (services + reverse proxy nginx via `WithContainer()` + healthchecks), pour que le `docker-compose.yml` généré par `aspire publish --publisher docker-compose` soit le plus complet possible et directement réutilisable en prod.
- **Pourquoi** :
  - **Suggestion du mentor (retour LinkedIn 02/06/2026, cf. fiche MENTORING-RETOURS.md)** : ".net aspire pour t'éviter les docker compose et faire tourner le tout en local en un clic et sa sera d'autant plus sécurisé."
  - **Validation Option B en visio 04/06/2026** : le mentor confirme la stratégie "tout dans l'AppHost" plutôt que "compose généré + patch manuel". Minimise la maintenance double et garantit que le compose prod est généré déterministiquement depuis le code C#.
  - **Dev local en 1 clic** : `dotnet run` sur l'AppHost lance Postgres + API + Frontend simultanément. Plus besoin de jongler entre `docker compose up`, `dotnet run`, `dotnet watch` dans plusieurs terminaux.
  - **Injection automatique des connection strings** via `WithReference()` : plus de manipulation manuelle de `.env` côté dev. Sécurité améliorée (= ce que le mentor appelle "d'autant plus sécurisé").
  - **Dashboard intégré** sur `localhost:18888` (port par défaut Aspire) : logs centralisés, traces distribuées (OpenTelemetry natif), métriques. Couvre une partie du scope BACK-058 (logs centralisés) et BACK-059 (monitoring) en dev gratuitement.
  - **Service Discovery** : l'API trouve la DB par son nom logique (`postgres`), pas par URL hardcodée. Plus robuste aux changements d'infra.
  - **Composants intégrés prêts** (Postgres, Redis, RabbitMQ, etc.) : ajout d'un service tiers = 1 ligne dans l'AppHost.
  - **Pitch portfolio** : .NET Aspire est trendy en 2026, signal "veille active" pour entretiens.
- **Pourquoi Option B (tout dans l'AppHost) plutôt qu'Option A (patch manuel)** :
  - **Option A** : Aspire génère un compose minimal, on ajoute le reverse proxy nginx + healthchecks dans un `docker-compose.prod.override.yml` séparé. → 2 fichiers à synchroniser, drift facile au fil du temps.
  - **Option B (retenue)** : tout est décrit dans l'AppHost C# (services .NET + reverse proxy nginx via `WithContainer()` + healthchecks). Le compose généré est complet → 1 seule source de vérité.
- **Cohérence avec DEC-030 et DEC-031** :
  - **Aspire utilise Container Support SDK (DEC-030)** en interne pour générer les images des projets .NET. Pré-requis : DEC-030 doit être appliqué avant.
  - **Aspire push vers GHCR (DEC-031)** via `aspire publish --publisher docker-compose --registry ghcr.io --tag <version>`. Le registry est réutilisé.
  - Les 3 décisions sont **complémentaires** (Container Support SDK = génération, GHCR = distribution, Aspire = orchestration), pas concurrentes.
- **Alternative considérée — Continuer avec docker-compose manuel** :
  - Avantage : aucune migration, stack connu et fonctionnel.
  - Inconvénients : dev local nécessite plusieurs terminaux, pas de dashboard logs intégré, gestion manuelle des secrets, pas de signal portfolio "veille".
  - **Rejetée** : le bénéfice DX (developer experience) + observabilité + portfolio l'emporte sur le coût de migration (1 spike de 1-2 jours).
- **Limites assumées** :
  - **Vendor lock-in Microsoft** : Aspire est un framework propriétaire. Migration future hors écosystème .NET impliquerait de tout redécrire. Acceptable vu que le projet est 100% .NET.
  - **Courbe d'apprentissage** : nouveau concept (AppHost, ServiceDefaults, lifecycle). Géré par le spike BACK-065.
  - **Le VPS ne sait pas qu'Aspire existe** : il reçoit juste un `docker-compose.yml` standard généré par `aspire publish`. Aspire est un outil **dev-side**, transparent côté prod.
- **Sources** :
  - [.NET Aspire — docs officielles Microsoft](https://learn.microsoft.com/en-us/dotnet/aspire/)
  - [Aspire docker-compose publisher](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/manifest-format)
  - [WithContainer() API reference](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.containerresourcebuilderextensions.withcontainer)
  - Retour mentor 02/06/2026 + visio 04/06/2026 (cf. fiches/MENTORING-RETOURS.md)
- **Conséquences** :
  - **Création d'un nouveau projet** `MemoRecipe.AppHost` (type Aspire AppHost) dans la solution `memorecipe-api.sln`.
  - **Création d'un projet** `MemoRecipe.ServiceDefaults` (configuration commune OpenTelemetry, health checks, service discovery).
  - **L'AppHost devient le point d'entrée dev** : `dotnet run --project MemoRecipe.AppHost`.
  - **`docker-compose.yml` (dev)** : potentiellement supprimé ou maintenu pour fallback, à arbitrer en fin de spike BACK-065.
  - **`docker-compose.prod.yml`** : devient un **artefact généré** par `aspire publish --publisher docker-compose --registry ghcr.io`. Ne se modifie plus à la main.
  - **BACK-058 et BACK-059** (logs centralisés + monitoring) : partiellement couverts en dev par Aspire Dashboard. Décision sur scope prod à reposer au moment de leur implém.
  - **Dépendances NuGet** : ajout des packages `Aspire.Hosting.AppHost`, `Aspire.Hosting.PostgreSQL`, etc.
  - **Validation post-spike** : si le spike BACK-065 révèle des limitations bloquantes (compose généré non utilisable en prod, complexité ingérable), retour à docker-compose manuel acceptable (décision à reverser).
- **Conditions qui invalideraient ce choix** :
  - **Aspire ne supporte pas l'orchestration multi-conteneurs complète** (reverse proxy nginx custom + healthchecks complets) au moment du spike → fallback sur docker-compose manuel.
  - **Vendor lock-in devient bloquant** : besoin de migrer hors .NET ou hors écosystème Microsoft → repasser à docker-compose.
  - **Le compose généré n'est pas réutilisable tel quel en prod** (Option B échoue) → 2 stratégies à arbitrer : repasser à Option A (compose + patch) ou abandonner Aspire.
  - **Coûts dev (apprentissage + maintenance AppHost) dépassent les bénéfices DX** sur la durée → retour à docker-compose.
- **État** : DÉCIDÉ le 04/06/2026 (visio mentor) — Option B confirmée. À implémenter dans **BACK-065** (étape 2, après BACK-063 + BACK-064).


### DEC-033 : Migration des tests d'integration SQLite -> TestContainers (vrai Postgres prod-like)

- **Statut** : ✅ ACTIVE
- **Date** : 04 juin 2026 (visio mentor) — décision actée, implémentation tracée dans BACK-062 
- **Choix** : Migration progressive de **SQLite in-memory** (utilisé actuellement dans `CustomWebApplicationFactory` via `UseSqlite(":memory:")` + `EnsureCreated()`) vers **TestContainers** (lance un container `postgres:16-alpine` réel pendant les tests d'intégration). Stratégie d'application **mix SQLite + TC** vs **all-TC** à arbitrer au moment de l'implémentation (cf. heuristique dans MENTORING-RETOURS.md section "Arbitrages restants à prendre en solo"). Migrations EF Core appliquées via `MigrateAsync()` (pas `EnsureCreated()`) pour valider le vrai chemin migration prod.
- **Pourquoi** :
  - **Suggestion du mentor (retour LinkedIn 02/06/2026, cf. fiche MENTORING-RETOURS.md, suggestion A)** : "Tu peux regarder du côté de TestContainer si tu veux faire tes tests sur un vrai PostgreSQL et pas du in-memory."
  - **Audit JSONB (03/06/2026)** : 3 colonnes JSONB en schéma prod (`AllergensJson`, `JsonData`, `MetadataJson` — cf. DEC-004) sont silencieusement traduites en `TEXT` par SQLite. Aucune query JSONB-specific exécutée aujourd'hui dans les tests, mais le risque devient bloquant dès la première feature "search by allergen" (`@>`, `?`, `->>` operators).
  - **Audit dates** : les colonnes `TIMESTAMP WITH TIME ZONE` (Postgres) sont stockées en `TEXT` par SQLite (ISO string). Précision microseconde perdue + `DateTime.Kind` perdu au round-trip (`Unspecified` en SQLite vs `Utc` en Postgres+Npgsql). Risque latent : si une logique métier finit par dépendre de `.Kind` post-DB-read, comportement différent test/prod.
  - **Validation migrations EF Core** : `EnsureCreated()` actuel **ne joue pas** les migrations EF Core — il crée le schéma direct depuis le modèle. Donc une migration custom (raw SQL, opérations Postgres-specific) passerait les tests mais péterait en prod. `Migrate()` sur TC valide le vrai chemin.
  - **Validation en visio mentor 04/06/2026** : le mentor confirme l'usage de TC dans ses projets (intégration + E2E), partage le concept d'extension "dépendances tierces" (cf. ci-dessous).
- **Scope** :
  - **S'applique à** : tests d'intégration ASP.NET dans `MemoRecipe.Api.Tests` (suites `CorsTests`, `RateLimitingTests`, `SecurityHeadersMiddlewareTests`, `UploadValidationTests`).
  - **Ne s'applique PAS aux tests unitaires** : les services métier (`RecipeService`, `AuthService`, validators, pipeline IA) utilisent des **Fakes** (`FakeRecipeRepository`, etc.) → millisecondes, pas de DB. TestContainers ralentirait sans bénéfice. Stratégie Fakes conservée (cf. DEC-009 — Tests unitaires avec FakeRepository).
- **Audit des tests existants (03/06/2026, ligne par ligne)** :
  - `CorsTests` : **DB-agnostic** (endpoint protégé → 401 avant DB).
  - `SecurityHeadersMiddlewareTests` : **DB-agnostic** (endpoint protégé → 401 avant DB).
  - `RateLimitingTests` : **DB-dependent mais Postgres-agnostic** (INSERT + SELECT basiques sur Users — comportement identique SQLite/Postgres).
  - `UploadValidationTests` : **DB-dependent mais Postgres-agnostic** (INSERT + SELECT pour auth setup).
  - **0 test actuel Postgres-dependent** → SQLite couvre 100% fonctionnellement aujourd'hui. **TC est une anticipation** pour les futures features (search JSONB, recherche temporelle, validation migrations).
- **Extension future — Dépendances tierces** (concept apporté par le mentor en visio 04/06/2026) :
  - TestContainers ne se limite pas aux DB : on peut containeriser **n'importe quel service tiers** dont l'app dépend (programme Python, service IA, API externe mock, MinIO/S3, RabbitMQ, etc.).
  - **Applicabilité MemoRecipe — service IA `memoRecipe-ia`** : aujourd'hui remplacé par `FakeOcrScanService` dans `CustomWebApplicationFactory`. Pour des tests E2E réels (futur), TC pourrait lancer un vrai container Azure Function en plus du container Postgres → test du contrat HTTP API <-> Service IA bout en bout. **Pas prioritaire maintenant** (le Fake actuel suffit, et le vrai service IA appelle Mistral en externe → mocking quand même nécessaire), tracé comme extension future dans BACK-062.
- **Alternative considérée — Garder SQLite in-memory** :
  - Avantage : tests ultra-rapides (ms), aucune dépendance Docker pour les tests.
  - Inconvénient : divergence silencieuse schéma test vs prod (JSONB → TEXT, TIMESTAMPTZ → TEXT, migrations non jouées). Bloquant dès qu'une feature exploite du Postgres-specific.
  - **Rejetée à terme** mais conservée comme **option mix** : peut rester pour les tests "DB-agnostic" (CORS, headers, rate limiting) qui ont juste besoin d'une DB pour booter `WebApplicationFactory` sans l'exercer.
- **Sources** :
  - [TestContainers for .NET — docs officielles](https://dotnet.testcontainers.org/)
  - [Testcontainers.PostgreSql NuGet](https://www.nuget.org/packages/Testcontainers.PostgreSql/)
  - [EF Core Database.MigrateAsync()](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.migrateasync)
  - Retour mentor 02/06/2026 + visio 04/06/2026 (cf. fiches/MENTORING-RETOURS.md)
  - DEC-004 (PostgreSQL avec colonnes JSONB) — section "Conséquence sur les tests"
- **Conséquences** :
  - **Dépendance NuGet** ajoutée à `MemoRecipe.Api.Tests` : `Testcontainers.PostgreSql`.
  - **Création fixture** `PostgresContainerFixture : IAsyncLifetime` qui lance/kill un container `postgres:16-alpine`.
  - **Refacto `CustomWebApplicationFactory`** : remplacer `UseSqlite(conn)` par `UseNpgsql(container.GetConnectionString())`. Selon stratégie mix vs all-TC, possibilité de maintenir 2 factories (`CustomWebApplicationFactorySqlite` pour DB-agnostic + `CustomWebApplicationFactoryPostgres` pour DB-dependent).
  - **Remplacement `EnsureCreated()` -> `await db.Database.MigrateAsync()`** : applique les vraies migrations EF Core → validation du chemin prod.
  - **Stratégie d'isolation** entre tests (à arbitrer à l'implém) : transaction rollback / TRUNCATE / container par classe. Suggéré dans MENTORING-RETOURS.md : démarrer avec **container par classe** (le plus simple), affiner si trop lent.
  - **Performance** : premier run plus lent (~30s incluant download + start container), runs suivants ~10s (image cachée). Acceptable pour integration tests.
  - **CI/CD** : GitHub Actions a Docker disponible sur les runners GitHub-hosted par défaut → pas de friction supplémentaire pour BACK-008.
  - **Documentation** : note dans `MemoRecipe.Api.Tests/README.md` (ou DEC dédiée) sur la stratégie de test, vocabulaire DB-agnostic / DB-dependent / Postgres-dependent (cf. MENTORING-RETOURS.md section "Vocabulaire clé").
- **Conditions qui invalideraient ce choix** :
  - **Docker indisponible sur l'environnement de test** (machine dev sans Docker Desktop, CI sans Docker support) → fallback SQLite + tests Postgres-specific skip.
  - **Coût TestContainers (~1s/test, ~5s setup) devient bloquant** sur un volume de tests gigantesque (1000+ tests d'intégration) → arbitrer container partagé vs containers per-class, ou repasser à SQLite sur les suites DB-agnostic.
  - **Migration vers un nouveau moteur DB non-Postgres** (improbable, cf. DEC-004 stable) → réévaluer.
- **État** : DÉCIDÉ le 04/06/2026 (visio mentor). **APPLIQUÉ le 13/06/2026** via BACK-062 (PR `feature/BACK-062-testcontainers`). 18/18 tests d'intégration passent contre vrai Postgres `postgres:16-alpine` lancé en container TestContainers. Résout aussi en cascade BACK-067 (régression `RequireConfig` fail-fast) via variables d'environnement système set dans un static constructor de `CustomWebApplicationFactory`. Stratégie d'isolation retenue : un container par classe de tests via `IClassFixture` (optimisation `ICollectionFixture` possible plus tard si volume tests augmente).


### DEC-034 : Report du fix collation Postgres dev lors du passage Debian -> Alpine (warning accepte temporairement)

- **Statut** : ✅ ACTIVE (dette technique tracée dans BACK-068, non-bloquante en attendant l'implémentation de BACK-029)
- **Date** : 11 juin 2026
- **Choix** : Pendant l'implémentation de BACK-066 (alignement Postgres dev `:16` -> `:16-alpine`), un warning `database "memorecipe_db" has no actual collation version, but a version was recorded` apparaît à chaque connexion psql. La commande standard `ALTER DATABASE memorecipe_db REFRESH COLLATION VERSION` échoue avec `ERROR: invalid collation version change` car elle gère seulement les changements de version dans le même provider, pas un changement cross-provider (glibc Debian -> musl Alpine). Le fix propre nécessite une procédure `pg_dump` + `dropdb` + `createdb` + `pg_restore`. **Décision** : **reporter ce fix** dans un ticket dédié (**BACK-068**) plutôt que de l'inclure dans BACK-066. Accepter le warning de façon temporaire.
- **Pourquoi reporter** :
  - **Zéro impact pratique aujourd'hui** : aucune feature actuelle ne fait de tri textuel sensible aux collations (pas de `ORDER BY title` avec accents/ligatures qui nécessiterait une cohérence parfaite). Les opérations CRUD basiques (INSERT/UPDATE/SELECT par PK ou FK) ne sont pas affectées.
  - **Scope BACK-066 strict** : BACK-066 est un **quick win** d'alignement d'image Docker (~10 min). Embarquer une procédure `pg_dump/restore` (~30-45 min) dans le même PR ferait gonfler le scope et masquerait l'objectif initial. Cohérent avec les principes "atomic commits" + "un sujet = un ticket = une PR".
  - **REINDEX déjà fait** : le `REINDEX DATABASE memorecipe_db` exécuté dans BACK-066 a recréé les **index** avec le nouveau provider musl. Les requêtes utilisant ces index ont des résultats cohérents. Seule la **métadonnée Postgres** garde une référence à l'ancien provider, ce qui se manifeste par le warning à la connexion (pas par un comportement erroné des requêtes).
  - **Documentation traçable** : BACK-068 trace le fix complet avec procédure step-by-step, critères d'acceptation, et dépendance explicite envers BACK-029 (recherche et filtres avec tri textuel). Le "Toi du futur" qui voudra implémenter BACK-029 saura qu'il faut d'abord clore BACK-068.
- **Alternative considérée — Faire le fix tout de suite dans BACK-066** :
  - Avantage : warning éliminé immédiatement, scope "Postgres dev/prod consistent" 100% complet.
  - Inconvénients : (1) scope creep de BACK-066 (passage de 10 min à ~1h), (2) la procédure `pg_dump/restore` est manuelle et hors du fichier Compose — elle ne se commit pas comme un changement de code, donc le commit serait un mélange de modif de fichier + procédure documentée, ce qui est moins propre, (3) prend du temps de session pour un bénéfice nul aujourd'hui.
  - **Rejetée** : préférence pour les commits/PRs atomiques + le fix sera fait au moment où il deviendra utile (juste avant BACK-029).
- **Sources** :
  - [Postgres docs — ALTER DATABASE (REFRESH COLLATION VERSION)](https://www.postgresql.org/docs/current/sql-alterdatabase.html)
  - [Postgres collation provider docs](https://www.postgresql.org/docs/current/collation.html)
  - Error message constaté en session 11/06/2026 : `ERROR: invalid collation version change`
- **Conséquences** :
  - **Warning visible** à chaque connexion psql en dev. Cosmétique, signale une dette technique connue. Pas de masquage d'autres warnings importants (Postgres logs distinctement).
  - **CRUD non impacté** : INSERT, UPDATE, DELETE, SELECT par PK/FK fonctionnent normalement. C'est uniquement le tri textuel basé sur les locales (qui n'est pas utilisé dans le code actuel) qui pourrait donner des résultats légèrement différents entre la version glibc historique et la version musl actuelle.
  - **Dette technique tracée** : BACK-068 (P2) avec procédure complète + critères d'acceptation + dépendance sur BACK-029.
  - **Onboarding contributeurs** : les nouveaux contributeurs qui clonent le repo n'ont pas le warning (ils créent un volume `postgres_data` neuf directement avec le provider musl). Le warning ne concerne que le volume historique de l'ancien dev existant. À mentionner dans le runbook de migration si besoin.
- **Conditions qui invalideraient ce choix (== déclenchent l'implémentation de BACK-068)** :
  - **Implémentation d'une feature utilisant un tri textuel** sur des champs susceptibles de contenir des accents/ligatures (typiquement BACK-029 recherche/filtres avec `ORDER BY title`). Le warning devient un risque réel : possibilité de tri non-déterministe entre dev et prod si les providers de collation finissaient par diverger encore plus.
  - **Multiplication des warnings** : si d'autres warnings critiques apparaissent et que le warning de collation noie le signal, il faut le traiter pour récupérer un log propre.
  - **Changement de provider Postgres** (improbable) : si on revenait à un provider glibc (passage à postgres:16 Debian classique), l'incohérence serait inversée — préférable de tout traiter d'un coup à ce moment-là.
  - **Découverte d'un bug réel** lié à la collation (résultats de requêtes différents en dev et en prod sur des chaînes de caractères) : le warning passerait de cosmétique à symptôme d'un vrai problème.
- **État** : DÉCIDÉ et appliqué le 11/06/2026. Fix tracé dans **BACK-068** (P2) avec étapes + critères d'acceptation. À réévaluer au moment du planning de **BACK-029** (recherche et filtres).


### DEC-035 : Sélection du LLM provider via variable d'environnement (Factory Pattern)

- **Statut** : ✅ ACTIVE. Le pattern Factory reste en vigueur. La liste des valeurs `AI_PROVIDER` a été étendue au fil du temps, elle compte aujourd'hui 6 valeurs : `Fake` (dev uniquement), `Mistral`, `Gemini`, `Groq`, `MistralVision`, `GeminiVision`. Voir [DEC-044](#dec-044) et [DEC-045](#dec-045) pour les extensions Vision.
- **Date** : 18 juin 2026 (spike BACK-070 préparatoire à BACK-069)
- **Choix** : Le provider LLM de l'Azure Function `memoRecipe-ia` est sélectionné dynamiquement au démarrage via la variable d'environnement **`AI_PROVIDER`** (valeurs valides : `Fake`, `Mistral`, `Gemini`). `Program.cs` lit cette valeur et instancie l'implémentation correspondante de `IChatCompletionClient` via un `switch` dans le `ConfigureServices` du `HostBuilder`. Un **garde-fou anti-Fake-en-Production** throw `InvalidOperationException` au démarrage si `AZURE_FUNCTIONS_ENVIRONMENT=Production` et `AI_PROVIDER=Fake` (fail-fast). En cas de provider inconnu, le message d'erreur du `default` du switch est **conditionnel selon l'environnement** (ne mentionne pas `Fake` comme valeur valide en Production).
- **Pourquoi** :
  - **Anti-pattern remplacé** : avant BACK-070, la sélection du provider se faisait en commentant/décommentant manuellement du code dans `Program.cs` (`// MistralChatCompletionClient` vs `services.AddSingleton<IChatCompletionClient, FakeChatCompletionClient>()`). Risque énorme de commit accidentel du mauvais code, impossible de switcher sans recompiler, pas auditable. **Inacceptable en pratique pro**.
  - **12-Factor App — Factor III "Config in the environment"** : config externalisée via variable d'environnement, jamais en dur dans le code. Le même binaire tourne en dev, pré-prod, prod, sans recompilation — seules les valeurs d'env changent.
  - **Open/Closed Principle (SOLID)** : ajouter un nouveau provider (ex: Anthropic, Groq) = nouvelle classe `XxxChatCompletionClient` + nouveau `case` dans le switch. **Zéro modification** des implémentations existantes ni du code métier (pipeline, RecipeAiService). Le code est "ouvert à l'extension, fermé à la modification".
  - **Architecture hexagonale Port/Adapter respectée** : `IChatCompletionClient` est le Port (interface), chaque implémentation est un Adapter (Mistral, Gemini, Fake). Le code métier ne sait pas quel provider tourne — c'est la magie de la DI.
  - **Fail-Fast Principle** : valider la config au démarrage (pas à la première requête HTTP). Un déploiement en Production avec `AI_PROVIDER=Fake` (oubli humain) **refuse de démarrer** avec un message explicite, plutôt que de retourner silencieusement la recette de cheesecake hardcodée du `FakeChatCompletionClient` à tous les utilisateurs. Le coût d'un bug détecté au boot < le coût d'un bug détecté en prod sous trafic.
  - **Switch facile entre providers pour spike / debug / benchmark** : changer la valeur de `AI_PROVIDER` dans `local.settings.json` (dev) ou dans les Application Settings Azure (prod) + restart = swap du LLM en quelques secondes, sans toucher au code. Démontré dans BACK-070 pour comparer Mistral vs Gemini sur les mêmes recettes.
  - **Préparation à BACK-069** : ce Factory Pattern simple (env var globale) est le **prélude** au Factory Pattern par-utilisateur que BACK-069 va implémenter (`IChatCompletionClientFactory.GetForUserAsync(userId)` qui lit la config IA du user en BDD).
- **Pourquoi `AI_PROVIDER` plutôt qu'un fichier de config dédié** :
  - **Cohérence avec le reste** : `MISTRAL_API_KEY`, `GEMINI_API_KEY`, `AZURE_FUNCTIONS_ENVIRONMENT` sont déjà des env vars → 1 mécanisme unique pour toute la config.
  - **Compatible Azure Functions** : les Application Settings du portail Azure deviennent automatiquement des env vars dans le worker. Pas besoin de gérer un fichier de config à part en prod.
  - **`Environment.GetEnvironmentVariable("X") ?? "default"`** : pattern C# idiomatique, 1 ligne, lisible.
- **Pourquoi fail-fast plutôt que fallback silencieux sur Fake** :
  - Un fallback silencieux (`if Production && Fake → utiliser Mistral à la place`) **masque le bug de config**. Le déploiement réussit, l'app tourne, mais quelqu'un découvre 3 semaines plus tard que l'env var n'était pas définie en prod. Entre-temps, des coûts API potentiellement non maîtrisés.
  - Le fail-fast garantit qu'un déploiement mal configuré est détecté **dans la minute** par l'équipe ops, avec un message d'erreur explicite : "AI_PROVIDER cannot be 'Fake' in Production. Set AI_PROVIDER to 'Mistral' or 'Gemini'."
- **Pourquoi message d'erreur du `default` conditionnel selon l'environnement** :
  - En Production, lister `Fake` comme valeur valide dans le message d'erreur est trompeur (puisque le garde-fou la rejetterait juste après). Mieux : ne pas la mentionner du tout en Prod, pour éviter qu'un ops mal renseigné essaie de la set.
  - En Dev, lister les 3 valeurs (`Fake`, `Mistral`, `Gemini`) est utile pour onboarder un nouveau dev.
  - Implémentation : opérateur ternaire `environnement == "Production" ? "'Mistral', 'Gemini'" : "'Fake', 'Mistral', 'Gemini'"`.
- **Alternative considérée — Configuration via fichier `appsettings.json`** :
  - Avantage : standard ASP.NET, fortement typé via `IOptions<T>`, validation via Data Annotations.
  - Inconvénients : Azure Functions Isolated utilise `local.settings.json` pour les env vars locales (pas un appsettings.json) → cohérence cassée. En prod, faut maintenir un appsettings.Production.json en plus des Application Settings Azure = duplication de config.
  - **Rejetée** : trop lourd pour le besoin (1 seule valeur à lire).
- **Alternative considérée — Multiple Function App déployées séparément (1 par provider)** :
  - Avantage : isolation totale entre providers, peut router via une porte d'entrée.
  - Inconvénients : multiplication des coûts d'infra (chaque Function App = ressource Azure facturable), maintenance triplée, configuration et déploiement multipliés.
  - **Rejetée** : disproportionné pour un projet portfolio, et le Factory Pattern intra-process est suffisant.
- **Sources** :
  - [12-Factor App — Factor III : Config](https://12factor.net/config)
  - [SOLID — Open/Closed Principle](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle)
  - [Architecture hexagonale (Ports and Adapters) — Alistair Cockburn](https://alistair.cockburn.us/hexagonal-architecture/)
  - [Microsoft.Extensions.DependencyInjection — Singleton lifetime](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
  - [Azure Functions Isolated worker — local.settings.json vs Application Settings](https://learn.microsoft.com/en-us/azure/azure-functions/functions-develop-local#local-settings-file)
- **Conséquences** :
  - **3 implémentations de `IChatCompletionClient` cohabitent** dans `memoRecipe-ia/Infrastructure/AI/` : `FakeChatCompletionClient` (existant), `MistralChatCompletionClient` (existant, décommenté), `GeminiChatCompletionClient` (nouveau, BACK-070).
  - **Nouvelle dépendance environnement** : `AI_PROVIDER` doit être défini explicitement dans `local.settings.json` en dev (default "Fake" si absent) et dans les Application Settings Azure en prod (sinon throw au démarrage).
  - **Documentation** : la fiche `documentation/fiches/LANCEMENT-APP-DEV.md` (créée pendant BACK-070) explique comment switcher de provider en dev.
  - **Compatibilité ascendante** : tout le code métier (`RecipePipeline`, `RecipeAiService`, `ExtractOcrFunction`) continue d'utiliser `IChatCompletionClient` sans modification. Le swap est totalement transparent pour ces couches.
  - **Préparation BACK-069** : la factory globale env-var va être **étendue en factory par-utilisateur** au moment de BACK-069. Le `switch (aiProvider)` deviendra un `switch (userConfig.Provider)` après lecture de la config user en BDD. Architecture progressive maîtrisée.
  - **Sécurité — risque identifié pendant BACK-070** : les requêtes HTTP sortantes vers les APIs LLM contiennent la clé en query string (cas Gemini) → loggées par défaut dans la console Function. Mitigation immédiate appliquée dans BACK-070 : `logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning)` dans `Program.cs` du worker. Mitigation complète à prévoir dans BACK-069 : scrubbing actif des patterns sensibles (`key=`, `token=`, `api_key=`, `password=`, `authorization=`) via Serilog Filter.
- **Conditions qui invalideraient ce choix** :
  - **Plus de 1 provider actif simultanément en prod** (ex: A/B testing entre Mistral et Gemini pour mesurer la qualité) → le switch global devient insuffisant, il faut une factory plus avancée (DEC-XXX future).
  - **Provider par utilisateur** (= scope BACK-069) → la factory env-var est dépassée, mais elle reste valable comme **fallback serveur** (le user qui n'a pas configuré sa clé utilise le provider par défaut serveur).
  - **Migration vers .NET Aspire (BACK-065)** → la configuration sera centralisée dans l'AppHost C#. Le pattern Factory reste, mais la source de configuration change.
- **État** : APPLIQUÉ le 18/06/2026 via **BACK-070** (Mistral + Gemini) puis **étendu le 19/06/2026 via BACK-071** (ajout Groq Llama 3.3 70B comme 3e provider, PR #20 merge commit `707ef63`). 3 implémentations de `IChatCompletionClient` cohabitent dans `memoRecipe-ia/Infrastructure/AI/` : `MistralChatCompletionClient`, `GeminiChatCompletionClient`, `GroqChatCompletionClient` (+ `FakeChatCompletionClient` pour tests). Le switch dans `Program.cs` les sélectionne via `AI_PROVIDER` env var (`Mistral` | `Gemini` | `Groq` | `Fake`). Open/Closed Principle vérifié en pratique : ajout du 3e provider sans modification des 2 existants ni du code métier. Factory étendue par-user prévue dans **BACK-069**.


### DEC-036 : Choix de Groq (Llama 3.3 70B) comme provider de fallback serveur pour la stratégie freemium de BACK-069

- **Statut** : 🟡 PARTIELLEMENT SUPERSEDED par [DEC-043](#dec-043) puis [DEC-045](#dec-045). Le principe "provider serveur par défaut + BYO Key" reste valide pour V2, mais le provider serveur par défaut n'est plus Groq (text-only), il est désormais MistralVision (voir DEC-043 pour le pivot Vision, puis DEC-045 pour le pivot vers Mistral au lieu de Gemini). Groq reste dans le codebase comme option text-only via le Factory Pattern ([DEC-035](#dec-035)), le switch entre providers reste trivial via la variable d'environnement `AI_PROVIDER`.
- **Note modèle** : depuis la dépréciation par Groq du modèle Llama 3.3 70B Versatile, le modèle effectivement utilisé par `GroqChatCompletionClient` est désormais `openai/gpt-oss-120b` (modèle open-source hébergé sur infra Groq). Les rate limits et coûts documentés dans le corps historique de cette DEC restent indicatifs, à recalibrer sur la grille du nouveau modèle si Groq est réactivé sérieusement en V2.
- **Date** : 18 juin 2026 (spike BACK-071 préparatoire à BACK-069)
- **Choix** : **Groq Llama 3.3 70B Versatile** est retenu comme provider de fallback serveur pour la stratégie freemium hybride de BACK-069 (essais gratuits avec clé serveur avant que l'utilisateur configure la sienne). 3 garde-fous accompagnent ce choix : (1) **maximum 2 essais gratuits par jour par utilisateur** (compteur applicatif BDD, reset à minuit UTC), (2) **compteur applicatif par minute** côté serveur pour détecter les pics et anticiper le 429 Groq (30 req/min), (3) **message d'information visible en UI** prévenant l'utilisateur que sur la version gratuite, des difficultés de scan peuvent survenir en cas de forte affluence. Groq remplace donc Gemini Flash qui avait été initialement proposé dans BACK-069 mais s'est révélé insuffisant en throughput après le spike BACK-070.
- **Pourquoi Groq plutôt que Gemini Flash, Mistral Small, ou autres** :
  - **Free tier journalier le plus généreux** : **14 400 scans/jour** (reset minuit UTC) vs 1 500/jour pour Gemini Flash et ~250-500/mois pour Mistral free. Permet d'absorber confortablement les volumes "portfolio" (jusqu'à ~100k users actifs/mois) à 0€ côté opérateur.
  - **Throughput minute correct** : 30 req/min (2× supérieur à Gemini Flash 15 req/min), suffisant pour un projet portfolio. Mistral est meilleur (60 req/min) mais perd sur le quota mensuel/journalier.
  - **Pas de carte bancaire requise** à l'inscription, ni pour activer le free tier (contrairement à OpenAI qui exige une CB dès le départ).
  - **Qualité parsing comparable** à Mistral Small / Gemini Flash pour le cas d'usage "parser une recette OCR en JSON structuré" (validé en spike BACK-070/071 sur la recette "Quiche sans pâte aux poireaux et thon").
  - **Vitesse d'inférence ultra-rapide** : LPU custom Groq → temps de réponse perçu ~200-500ms vs 1-2s pour les autres providers cloud. Meilleure UX de scan.
  - **API REST compatible OpenAI** (format `messages[].role/content`) → adapter Groq quasi-identique à Mistral, **zéro nouveau pattern à apprendre** côté code (cf. `GroqChatCompletionClient.cs` créé en 5 min sur BACK-071).
  - **Cohérence avec l'analyse coûts** (cf. tableau ci-dessous) : 0€/mois côté opérateur jusqu'à ~100k users actifs/mois, et coût raisonnable au-delà.
- **Analyse comparative chiffrée (issue de BACK-071)** :
  - **Coût par scan** (~2 000 tokens : 1 500 input + 500 output) :
    - Gemini Flash : ~$0.00026/scan (le moins cher en absolu)
    - Mistral Small : ~$0.0006/scan
    - Groq Llama 3.3 70B : ~$0.0013/scan
    - Claude Haiku : ~$0.004/scan (le plus cher)
  - **Coût mensuel côté opérateur selon volume** (scénario portfolio modéré, avg 3 scans/user/mois) :
    - 10 users : 0€ avec n'importe quel provider
    - 1 000 users : 0€ avec Groq (3 000 scans absorbés free tier), ~1.50€ Mistral, ~0€ Gemini (sous free 45k)
    - 10 000 users : ✅ **0€ avec Groq** (30 000 scans), ~17€ Mistral, ~3€ Gemini (au-delà free)
    - 100 000 users : ✅ **0€ avec Groq** (300 000 scans, sous 432k free), ~170€ Mistral, ~67€ Gemini
    - 500 000 users : ~1 387€ Groq, ~860€ Mistral, ~273€ Gemini (Gemini moins cher EN ABSOLU à très grande échelle mais throughput catastrophique pour ce volume)
  - **Throughput** :
    - Pic 30 users/minute : ✅ Groq OK (30/min) | ❌ Gemini bloque (15/min) | ✅ Mistral OK (60/min)
    - Pic 100 users/heure : ✅ Groq large (14 400/jour) | ⚠️ Gemini limit 1500/jour | ✅ Mistral OK mais coût $$
- **Garde-fous décidés (à implémenter dans BACK-069)** :
  - **Garde-fou utilisateur** : maximum **2 essais gratuits par jour** par compte utilisateur (table `free_tier_usage` : `UserId`, `Date`, `Count`. Index unique sur (`UserId`, `Date`). Reset implicite à minuit UTC car nouvelle entrée chaque jour). Au-delà → modal "Tu as utilisé tes 2 essais gratuits aujourd'hui. Configure ta clé pour continuer OU reviens demain."
  - **Garde-fou serveur global** : **compteur applicatif par minute** (table ou cache mémoire `IMemoryCache` avec TTL 60s). Si compteur global ≥ 28 dans la minute glissante (marge de 2 sur les 30 de Groq), refuser temporairement les nouveaux essais avec message "Service IA temporairement saturé, réessaie dans 1 min" — évite proactivement le 429 Groq.
  - **Information utilisateur (UI)** : bannière ou tooltip visible sur la page de scan : "**Sur la version gratuite, des difficultés peuvent survenir en cas de forte affluence. Pour une expérience optimale, configure ta propre clé IA gratuite dans Settings.**" Texte clair, non culpabilisant, qui invite à la conversion vers BYO key.
  - **Pas de cap global serveur explicite** au démarrage (= on accepte de payer si dépassement, mais avec alerting log pour anticiper). À ajouter plus tard si volumes massifs constatés.
  - **Gestion du 429 réel** (si malgré le garde-fou minute on tape le quota Groq) : intercepter `HttpRequestException` avec status 429 → ne PAS comptabiliser dans le compteur user (pas de double-charge), retourner message frontend clair invitant à réessayer dans 1 min ou configurer sa clé.
- **Sources** :
  - [Groq Cloud Pricing](https://groq.com/pricing/)
  - [Groq Rate Limits documentation](https://console.groq.com/docs/rate-limits)
  - [Mistral AI Pricing](https://docs.mistral.ai/platform/pricing/)
  - [Google AI Studio Free Tier](https://ai.google.dev/pricing)
  - Spike BACK-070 (test E2E Gemini 429) + BACK-071 (test E2E Groq + analyse coûts)
- **Alternative considérée — Mistral Small** :
  - Avantage : throughput minute meilleur (60 req/min vs 30 Groq), code adapter déjà testé E2E dans BACK-070.
  - Inconvénients : free tier mensuel beaucoup plus restreint (~250-500 scans/mois vs 432 000 Groq), coût plus rapide à grandir, pas de visibilité sur les quotas free exacts.
  - **Rejetée** : Groq offre un free tier journalier 28× supérieur à Mistral mensuel, ce qui prime sur l'avantage throughput minute (rarement critique pour un portfolio).
- **Alternative considérée — Multi-provider rotation** (Groq → Mistral → Gemini si quota saturé) :
  - Avantage : robustesse maximale (jamais bloqué tant qu'au moins 1 provider OK).
  - Inconvénients : complexité architecture (état partagé, logique rotation, gestion des 3 clés et leurs quotas), overkill pour un projet portfolio.
  - **Rejetée pour MVP** : à reconsidérer si l'app atteint des volumes > 100k users actifs/mois (= "joli problème à avoir").
- **Alternative considérée — Pas de fallback serveur du tout (strict BYO key)** :
  - Avantage : 0€ côté opérateur, simplicité maximale.
  - Inconvénients : friction énorme pour la démo entretien (recruteur doit créer un compte Groq pour tester), conversion utilisateurs catastrophique sur un portfolio.
  - **Rejetée** : un projet portfolio doit pouvoir être testé en 30 secondes par un recruteur, le freemium est crucial.
- **Conséquences** :
  - **`GROQ_API_KEY` à provisionner** dans les Application Settings Azure en prod (stockée dans un gestionnaire de mots de passe côté opérateur).
  - **`AI_PROVIDER=Groq`** comme défaut serveur en prod (cf. DEC-035 — factory env var).
  - **2 nouvelles tables/structures** à ajouter dans le schéma BDD de BACK-069 :
    - `free_tier_usage` (UserId, Date, Count) — quota journalier par user.
    - Compteur minute en `IMemoryCache` (volatile, perdu au restart — acceptable car protection best-effort).
  - **Component UI BACK-069** : bannière "version gratuite limitée" sur la page de scan, persistant tant que pas de config user.
  - **Documentation BACK-069 + politique de confidentialité (BACK-006)** : mentionner Groq comme provider tiers de scan recettes en version gratuite. Conformité RGPD (donnée envoyée à Groq Inc. — DPA Groq à vérifier au moment de BACK-006).
  - **Surveillance à mettre en place plus tard** (post-MVP) : dashboard ou alertes Application Insights sur le compteur jour/minute pour anticiper l'atteinte des seuils Groq.
- **Conditions qui invalideraient ce choix** :
  - **Volume serveur dépassant régulièrement 14 400 scans/jour** (= ~5 000 users actifs/jour avec 2-3 scans) → migrer vers tier payant Groq OU multi-provider rotation OU passer à un fallback Mistral Small payant à la demande.
  - **Dégradation de qualité du parsing** observée sur Llama 3.3 70B (peu probable, mais à surveiller) → bascule vers Mistral Small en fallback.
  - **Changement de politique Groq** (suppression du free tier 14 400/jour, ajout CB obligatoire) → bascule vers Mistral ou Together AI.
  - **Atteinte de volumes massifs imprévus** (>500k users actifs/mois) → re-arbitrer entre Groq payant (~1 400€/mois pour 500k users à 3 scans/mois) et Gemini Flash payant (~273€/mois mais throughput catastrophique pour ce volume — improbable).
- **État** : DÉCIDÉ le 18/06/2026 via **BACK-071** (spike technique validé E2E), **MERGÉ sur main le 19/06/2026** (PR #20, merge commit `707ef63`). À **APPLIQUER dans BACK-069** : les 3 garde-fous (compteur jour user, compteur minute serveur, bannière UI) sont à coder dans BACK-069 en même temps que la factory par-user.

- **🟡 PARTIELLEMENT SUPERSEDED par [DEC-043](#dec-043) (09/08/2026) puis affiné par [DEC-045](#dec-045) (10/08/2026)** : le rôle central de Groq positionné ici comme provider de fallback serveur pour la stratégie freemium de BACK-069 a été réévalué suite à deux pivots successifs.
  - **Ce qui reste valide de cette DEC** : Groq (Llama 3.3 70B) reste un provider techniquement pertinent et présent dans le codebase via le Factory Pattern ([DEC-035](#dec-035)). Les rate-limits Groq analysés ici (30 RPM, 14 400 requêtes par jour free tier) restent factuels et exploitables si Groq est réactivé plus tard.
  - **Ce qui est superseded** : la position de Groq comme provider par défaut serveur en prod (mention `AI_PROVIDER=Groq` dans les conséquences) est superseded. Depuis [DEC-043](#dec-043), le principe du provider par défaut a basculé vers un modèle Vision multimodal (écart qualitatif mesuré environ 4× sur les photos complexes). Depuis [DEC-045](#dec-045), le provider Vision par défaut retenu est MistralVision (Mistral AI, hébergement UE, RGPD-natif, Experiment tier gratuit sans carte bancaire) et non Gemini (contrainte prepayment AI Studio incompatible avec la posture zéro coût du projet).
  - **Impact sur la stratégie freemium BACK-069 (V2)** : la stratégie "provider serveur par défaut + Bring-Your-Own-Key pour utilisateurs qui veulent leur propre quota" reste pertinente sur le principe. Ce qui change, c'est l'identité du provider serveur par défaut, désormais MistralVision. Groq peut rester une option économique text-only proposée aux utilisateurs BYO qui préfèrent ce compromis.
  - **Décision finale sur Groq en V1** : Groq est conservé dans le codebase comme provider text-only secondaire, sélectionnable via `AI_PROVIDER=Groq`. Aucun code mort. Utile en cas de panne d'un provider Vision ou de contrainte réglementaire spécifique future.

---

### DEC-037 : Soft delete RGPD Art. 17 avec login-check seul pour MVP, cron auto reporté à BACK-077 (Observability-Before-Features)

- **Statut** : ✅ ACTIVE
- **Contexte** : BACK-005 (RGPD Suppression de compte utilisateur) implémente le pattern soft delete avec délai de grâce de 30 jours. Deux mécanismes possibles pour purger définitivement les comptes expirés : (a) un check au login (l'user qui se reconnecte après J+30 déclenche la purge à ce moment-là), (b) un cron en arrière-plan (`IHostedService`) qui tourne 1× par jour et purge tous les comptes dont `DeleteRequestedAt < NOW() - 30 days`.

- **Choix** : Pour la version MVP de BACK-005, on implémente UNIQUEMENT le **login-check** (a). Le **cron auto** (b) est reporté à un nouveau ticket dédié **BACK-077**, lui-même bloqué par 3 prérequis : **BACK-078** (backup PostgreSQL automatique), **BACK-010** (logging structuré Serilog), **BACK-079** (monitoring + alertes sur opérations critiques).

- **Pourquoi cette décision** :
  1. **Principe SRE "Observability before features"** : on ne déploie pas une opération AUTOMATIQUE et DESTRUCTIVE (suppression de users en cascade) sans filets de sécurité solides. Un bug logique dans le cron (mauvais `WHERE`, race condition, etc.) pourrait supprimer des données users massivement sans qu'on s'en rende compte avant des heures voire des jours.
  2. **Backup obligatoire** : sans `pg_dump` automatique quotidien (BACK-078), toute erreur du cron = perte définitive de données users. Inacceptable pour un projet conforme RGPD.
  3. **Logging structuré obligatoire** : sans Serilog (BACK-010), impossible de tracer correctement chaque exécution du cron (combien de users purgés, lesquels, pourquoi, erreurs partielles, etc.).
  4. **Monitoring/alertes obligatoires** : sans alertes (BACK-079), un cron qui supprime 1000 users par erreur ne déclencherait aucune notification → on ne s'en rendrait compte que des jours plus tard via plaintes users.
  5. **Pas de prod publique actuellement** : MemoRecipe est en dev, pas d'users réels à protéger immédiatement. Le login-check seul couvre 80% des cas (users qui reviennent) et suffit pour MVP fonctionnel. La conformité RGPD totale (couvrir les 20% restants = users fantômes qui ne reviennent jamais) sera activée AVANT la mise en prod publique via BACK-077.
  6. **Introduction progressive** : `IHostedService` est mis en place dans BACK-077 après que les filets (BACK-078/010/079) soient en place — moins risqué qu'un déploiement combiné feature + observabilité.

- **Alternatives écartées** :
  - **Tout faire dans BACK-005** (soft delete + login-check + cron auto + backup + monitoring) → PR énorme, risque qualité élevé, mélange de plusieurs préoccupations (RGPD + Infra + Observabilité).
  - **Login-check seul de manière permanente** → viole RGPD Art. 17 ("dans les meilleurs délais") car les users fantômes restent indéfiniment en BDD.
  - **Cron auto sans backup ni monitoring** → joue à la roulette russe avec les données users. À proscrire absolument.

- **Conséquences** :
  - BACK-005 mergeable rapidement avec un scope clair et testable.
  - 3 tickets séparés (BACK-077/078/079) avec des responsabilités claires, mergeables indépendamment dans le bon ordre.
  - **Avant la mise en prod publique** : BACK-078 → BACK-010 → BACK-079 → BACK-077 → ensuite seulement on déploie.
  - Documentation utilisateur : la `Privacy.razor` section 5 mentionne le délai de grâce 30 jours et précise "via un processus automatisé quotidien" (blocs activés lors du merge BACK-077).

- **Date** : 2026-06-23, identifié pendant l'implémentation de BACK-005 sur la question de la sécurité d'une opération destructive automatique en l'absence de backup/monitoring.

- **État** : APPLIQUÉ EN 2 TEMPS.
  - **Phase 1 — Login-check purge MVP** : implémentée le 29/06/2026 via **BACK-005** (PR #25 `feature/BACK-005-soft-delete-account`, merge commit `c2480ac`). 12 commits atomiques. Test E2E exhaustif validé (7 scénarios + purge >30j simulée via `UPDATE` SQL manuel). Stratégie login-check seule = MVP fonctionnel couvrant les users qui reviennent (80% des cas).
  - **Phase 2 — Cron auto (couverture 100% des cas RGPD Art. 17)** : implémentée le 29/07/2026 via **BACK-077** (PR #41 `feature/BACK-077-account-purge-cron`, merge commit `7445588`). 5 commits atomiques : config `AccountPurgeOptions` + service `AccountPurgeService : BackgroundService` + registration + 4 tests unitaires TestContainers + activation blocs Privacy.razor. Prérequis satisfaits en amont : [BACK-078](../documentation/BACKLOG.md#back-078) partie 1 mergée (backup local chiffré GPG asymétrique, filet de sécurité en cas de bug catastrophique), [BACK-010](../documentation/BACKLOG.md#back-010) mergé (Serilog structuré pour tracer chaque exécution), [BACK-079](../documentation/BACKLOG.md#back-079) mergé (alerting `NotifyMassPurgeAsync` déclenché à chaque run). Test manuel E2E validé sur vrai Postgres dev.
  - **RGPD Art. 17 100% couvert** : login-check + cron auto couvrent respectivement les users actifs (récupération / annulation demande) et les users fantômes (purge automatique à J+30). Aucun compte marqué pour suppression ne peut rester indéfiniment en BDD.

---

### DEC-038 : Stratégie backup PostgreSQL — GPG asymétrique + règle 3-2-1 + découpage BACK-078 en 2 parties

- **Statut** : ✅ ACTIVE (partie 1 backup local chiffré appliquée, partie 2 off-site automatisé repriorisée V1.1, off-site V1 assuré manuellement sur médium physique séparé)
- **Date** : 06 juillet 2026 (identifié pendant le cadrage de BACK-078)

- **Choix** : La stratégie backup de MemoRecipe repose sur 4 décisions clés :
  1. **Format `pg_dump` custom** (`.dump` binaire compressé) plutôt que plain SQL — plus compact (~30-50%), restauration plus rapide, sélective possible.
  2. **Chiffrement asymétrique GPG** (paire de clés publique/privée) plutôt que symétrique (mot de passe partagé) — évite le paradoxe "clé co-localisée avec le backup".
     - **Clé publique GPG** stockée sur le VPS de production (sert uniquement à chiffrer).
     - **Clé privée GPG** stockée mais JAMAIS sur le VPS.
     - **Passphrase de la clé privée** dans un gestionnaire de mots de passe.
     - Résultat : compromise du VPS = attaquant vole des `.dump.gpg` illisibles sans la clé privée.
  3. **Règle 3-2-1** appliquée : 3 copies des données (BDD prod + backup local VPS + backup externe), 2 supports différents (disque VPS + service externe), 1 copie hors-site (service off-site S3-compatible ou SFTP à définir en partie 2).
  4. **Découpage BACK-078 en 2 parties** :
     - **Partie 1 (à traiter maintenant)** : script `backup.sh` sur VPS = `pg_dump` + chiffrement GPG + stockage local `/backups/` + rétention 30j + cron quotidien pendant les heures creuses. Débloque le principal filet de sécurité (permet de restaurer en cas de bug BDD ou migration foireuse). **Autonome, faisable en local sans VPS opérationnel.**
     - **Partie 2 (avant mise en prod publique)** : script `upload.sh` = copie hors-site vers un service de stockage externe (S3-compatible ou SFTP, à sélectionner en partie 2) via `rsync`/`rclone`/`sftp` + rétention 90j côté externe + cron hebdomadaire. Complète la conformité RGPD Art. 32 (portabilité + résilience).

- **Pourquoi ces choix** :
  - **`pg_dump` custom vs plain SQL** : compression native pgdump, restore sélectif possible (`pg_restore --table=...`), plus rapide sur grosses BDD. Pas d'inconvénient pour une BDD MemoRecipe (~quelques Go maxi).
  - **GPG asymétrique vs symétrique** : le paradoxe classique "où stocker le mot de passe de déchiffrement" est résolu — clé privée SÉPARÉE du serveur qui produit les backups. Compromise du VPS n'expose PAS les données. Défense en profondeur (RGPD Art. 32).
  - **Découpage 2 parties** : la partie 1 SEULE (backup + chiffrement local) apporte 90% de la valeur métier (résilience contre bug/migration/incident logiciel). La partie 2 ajoute la protection contre incident matériel/physique du VPS (crash disque, incendie datacenter, hébergeur indisponible). Séparer les 2 permet un cycle d'apprentissage progressif (backup basique → sécurisation avancée) et 2 PRs plus reviewables.
  - **PAS de `uploads.tar.gpg`** : MemoRecipe ne persiste actuellement aucun fichier sur disque (les entités `RecipeImage` et `OCRExtraction` stockent uniquement des URLs). À rajouter QUAND un vrai stockage d'images (Cloud Storage, CDN) sera ajouté au projet (nouveau ticket futur).
  - **PAS de `env.gpg`** : les secrets sont dans `.env` (gitignored) et déjà backupés dans un gestionnaire de mots de passe. Redondance inutile. En cas de recovery, le `.env` se recrée à partir du gestionnaire de mots de passe.

- **Alternatives écartées** :
  - **Chiffrement symétrique GPG (mot de passe partagé)** : simple à mettre en place mais paradoxal — si on stocke le mot de passe sur le VPS pour l'automatisation, un attaquant qui compromet le VPS déchiffre les backups. Écartée.
  - **`age` au lieu de GPG** : moderne (2019), syntaxe plus simple, sécurité solide (Ed25519 + ChaCha20-Poly1305). MAIS compétence moins universelle que GPG, moins portable sur les serveurs Linux "old school". GPG retenu pour valeur portfolio et universalité.
  - **Chiffrement au niveau du volume Docker (LUKS)** : protège seulement au repos local. Ne couvre PAS les backups qui sortent du volume (copies vers external storage). Insuffisant seul.
  - **Skip BACK-078 en s'appuyant sur les backups managés de l'hébergeur** : envisageable si un service backup managé est activé (Auto Backup VPS, snapshots). MAIS conformité RGPD Art. 32 exige que le responsable de traitement prouve son contrôle sur les backups — un backup managé hébergeur seul ne suffit pas (dépendance sous-traitant, portabilité limitée, restauration granulaire absente).
  - **Backup vers un service off-site dès la partie 1** : possible mais complexifie la partie 1 avec setup credentials externes + rclone/sftp. Découpage 2 parties permet de valider le cœur (backup + restore local) avant d'ajouter la couche transport.

- **Sources** :
  - [PostgreSQL Docs — pg_dump / pg_restore](https://www.postgresql.org/docs/16/backup-dump.html)
  - [GPG Handbook (Free Software Foundation)](https://www.gnupg.org/documentation/manuals/gnupg/)
  - [Règle 3-2-1 backup — US-CERT](https://www.cisa.gov/uscert/ncas/tips/ST19-006)
  - [RGPD Art. 32 — Sécurité du traitement](https://gdpr-info.eu/art-32-gdpr/)
  - Documentation générique services de stockage off-site : plusieurs options S3-compatibles ou SFTP disponibles sur le marché (comparatif tenu à jour dans les notes ops privées).

- **Conséquences** :
  - **Setup 1× (30 min)** : générer paire de clés GPG sur la workstation maintenance, exporter la clé publique, sauvegarder la clé privée dans un gestionnaire de mots de passe et un support offline additionnel.
  - **Nouveau dossier `infra/backup/`** dans le repo avec les scripts `backup.sh` + `restore.sh` + `Dockerfile` du container backup.
  - **Nouveau service `backup`** dans `docker-compose.prod.yml` (alpine + pg_dump + gpg + cron).
  - **Fiche `POSTGRES-BACKUP-CHEATSHEET.md`** ajoutée à `documentation/fiches/` pour référence rapide (chiffrer, déchiffrer, restaurer).
  - **Section "Backup & Restore"** ajoutée à `DEPLOYMENT.md`.
  - **BACK-077 (cron purge auto) débloqué** dès que la partie 1 est mergée + off-site en place (automatisé ou manuel), car opération destructive automatisée nécessite filet backup + monitoring.

- **Conditions qui invalideraient ce choix** :
  - **Ajout d'un stockage de fichiers persistants** (images uploadées, PDF, etc.) → étendre BACK-078 avec `uploads.tar.gpg`.
  - **BDD très grosse** (>100 Go) → passer à un backup incrémental (WAL archiving) au lieu de full `pg_dump` quotidien.
  - **Multi-tenant avec conformité stricte** → migrer vers une solution managée type Barman ou pgBackRest avec point-in-time recovery.

- **État** : DÉCIDÉ le 06/07/2026.
  - **PARTIE 1** : ✅ MERGÉE le 07/07/2026 (PR #26 `feature/BACK-078p1-backup-basic`). Backup local automatisé chiffré + test E2E restore validé.
  - **PARTIE 2** : ⚠️ REPRIORISÉE le 29/07/2026 pour tenir la fenêtre V1 launch. Le découpage initial "avant la mise en prod publique" a été révisé en 2 volets : (a) pour V1, l'off-site est **opérateur-managed sur médium physique séparé** — mesure raisonnable au sens RGPD Art. 32 pour un volume V1 (quelques dizaines d'users maxi), satisfait la règle 3-2-1 (3 copies, 2 supports, 1 hors-site) mais dépend de la discipline opérateur ; (b) l'automatisation du off-site via `rclone` + object storage S3-compatible ou SFTP est tracée dans le backlog privé pour V1.1 afin de supprimer la dépendance opérateur. Procédure de la V1 (médium physique) documentée dans le runbook ops privé, mentionnée génériquement dans `DEPLOYMENT.md` public. Le split 2-parties de la décision initiale reste conceptuellement valide — seule la temporalité de la partie 2 a été révisée.

---

### DEC-039 : Canal d'alerting — Telegram Bot API + abstraction `INotificationChannel`

- **Statut** : ✅ ACTIVE
- **Date** : 13 juillet 2026 (identifiée pendant le cadrage de BACK-079)

- **Choix** : Pour l'alerting critique du projet MemoRecipe (BACK-079), on retient **2 décisions couplées** :
  1. **Telegram Bot API** comme canal d'alerte instantanée par défaut. Setup en 5 min via `@BotFather` sur Telegram — récupération d'un `BotToken` + création d'un canal privé "MemoRecipe Alerts" pour récupérer un `ChatId`. Envoi via simple `POST https://api.telegram.org/bot<TOKEN>/sendMessage` avec `chat_id` + `text` (support Markdown/HTML basique).
  2. **Abstraction `INotificationChannel`** dans `MemoRecipe.Application/Notifications/` implémentée par `TelegramNotificationChannel` dans `MemoRecipe.Infrastructure/Notifications/` (pattern Ports/Adapters cohérent avec le reste de la Clean Architecture). Le service métier `AlertingService` dépend uniquement de l'interface — le canal réel est injecté via DI. Résultat : swap Telegram → Discord/Slack/Teams/email = **ajouter 1 classe adapter + changer 1 ligne DI dans `Program.cs`**, aucun changement dans le code métier.

- **Pourquoi ces choix** :
  - **Telegram Bot API** : setup ~5 min via `@BotFather` (aucun SDK, aucun OAuth, aucun renouvellement de token), simple `POST` HTTP, rate limit 30 msg/sec largement au-delà du volume prévu (~10-50 alertes/jour), notification push mobile instantanée, gratuit sans quota mensuel (contrairement à Slack free tier 10k msg/mois), canal privé possible pour isoler les alertes du projet des autres notifications.
  - **Discord écarté** : moins courant en contexte entreprise (parfois bloqué en réseau pro, perçu comme "app gaming" par certaines DSI). Pas de gain net vs Telegram pour un projet mono-mainteneur.
  - **Email écarté** : latency imprévisible (SMTP, greylist, spam), risque élevé de finir en spam, pas d'organisation par thread, tronqué sur mobile. Adapté aux rapports périodiques archivables, pas aux alertes temps réel.
  - **PagerDuty / OpsGenie écartés** : coût significatif (~15-25$/user/mois), overkill pour un projet mono-mainteneur.
  - **Abstraction `INotificationChannel` (Ports/Adapters)** : le canal réel est un détail d'infrastructure qui peut évoluer selon le contexte de déploiement. Le service métier `AlertingService` doit rester agnostique du canal pour garantir la portabilité (swap vers Slack/Teams/email en contexte entreprise) + la testabilité (via `FakeNotificationChannel` en tests unitaires). Coût du pattern = ~20 lignes de plus, bénéfice = swap trivial + isolation du métier vs infra.

- **Alternatives écartées** :
  - **Câbler Serilog directement à un sink Discord/Telegram** (via `Serilog.Sinks.Discord` ou équivalent) : rapide mais **anti-pattern SRP** — Serilog est un logger, pas un système d'alerting. Un logger doit tout logger (Info/Warning/Error), un système d'alerting doit **filtrer** (seulement Warning+ ou selon règles métier), **débouncer** (éviter le spam si 100 erreurs 500 en 1 min), **enrichir** (contexte, seuil dépassé, historique) et **router** (canaux différents selon sévérité). Ces concerns métier n'ont rien à faire dans un sink de log. `AlertingService` reste une couche métier dédiée qui consomme les événements et décide s'il faut alerter — Serilog capture les événements, `AlertingService` décide quoi en faire.
  - **Push notification directe (FCM/APNs)** : demande un compte développeur Google/Apple + une app cliente installée côté récepteur. Overkill pour un projet solo.
  - **Webhook vers un service tiers de routing d'alertes (n8n, Zapier)** : introduit une dépendance externe supplémentaire + un point de latence. Utile en équipe multi-outils, pas pour un solo dev.

- **Sources** :
  - [Telegram Bot API](https://core.telegram.org/bots/api) — documentation officielle
  - [Telegram Bot tutorial (@BotFather)](https://core.telegram.org/bots/tutorial) — création d'un bot en 3 clics
  - [Ports and Adapters pattern (Alistair Cockburn)](https://alistair.cockburn.us/hexagonal-architecture/) — origine du pattern qui justifie l'abstraction `INotificationChannel`
  - [OWASP A09:2025 Security Logging and Alerting Failures](https://owasp.org/Top10/A09_2021-Security_Logging_and_Monitoring_Failures/) — obligation d'alerter sur événements critiques
  - [Comparaison canaux d'alerting DevOps](https://sre.google/sre-book/monitoring-distributed-systems/) — Google SRE Book chapitre monitoring (les canaux d'alerte doivent être choisis pour maximiser la réactivité, pas la commodité de l'outil)

- **Conséquences** :
  - **Setup 1× (~10 min)** : créer bot Telegram via `@BotFather`, récupérer `BotToken`, créer canal privé "MemoRecipe Alerts", récupérer `ChatId` (via `getUpdates` API après ajout du bot au canal).
  - **Nouveau namespace `MemoRecipe.Application.Notifications`** (interface + enum `AlertLevel` Info/Warning/Critical).
  - **Nouveau namespace `MemoRecipe.Infrastructure.Notifications`** (`TelegramNotificationChannel` avec `HttpClient` injecté via `AddHttpClient<>`).
  - **Nouveau service `AlertingService`** dans `MemoRecipe.Application.Services.Monitoring` qui décide **quand** alerter (règles métier : purge > 50 users, login fail > 100/5min, erreurs 500 > 10/5min, backup > 25h).
  - **Configuration `appsettings.json`** : sections `Alerting` (seuils par règle) + `Telegram` (`BotToken`, `ChatId` en placeholders `CHANGE_ME`).
  - **Secrets** : vraies valeurs `BotToken` + `ChatId` dans `appsettings.Development.json` (gitignored) + variables d'environnement `Telegram__BotToken` / `Telegram__ChatId` en prod. Fail-fast au démarrage si absentes (via `RequireConfig` déjà en place — BACK-004).
  - **Tests unitaires** : `AlertingService` testé avec un `FakeNotificationChannel` (implémentation qui capture les envois en mémoire), permet d'asserter "après 51 users purgés, un envoi de niveau Critical a été déclenché".
  - **Pattern documenté dans `AlertingService`** pour ne pas leaker le `BotToken` : jamais logué, jamais retourné dans une réponse HTTP, jamais dans un message d'erreur. Discipline no-leak cohérente avec BACK-010.

- **Conditions qui invalideraient ce choix** :
  - **Passage en équipe (multi-devs on-call)** : Telegram individuel devient insuffisant — nécessité de router les alertes vers un canal partagé Slack/Teams avec système d'astreinte/rotation. À ce moment-là, ajouter un `SlackNotificationChannel` en parallèle du `TelegramNotificationChannel` (le pattern `INotificationChannel` supporte plusieurs canaux simultanés) OU migrer vers PagerDuty/OpsGenie pour la gestion des rotations.
  - **Volume d'alertes explose (>1000/jour)** : rate limit Telegram (30 msg/sec) deviendrait le bottleneck — nécessité de débouncer/aggréger côté `AlertingService` avant envoi, OU passer à un système dédié comme Grafana Alerting.
  - **Compliance stricte (banque, santé)** : Telegram (serveurs hors UE) pourrait poser un problème RGPD/résidence des données si les alertes contiennent des données utilisateurs. À ce moment-là, migrer vers un canal européen (Slack EU tier, email SMTP français, ou solution self-hosted comme Mattermost).

- **État** : DÉCIDÉ le 13/07/2026 pendant le cadrage de BACK-079. **À APPLIQUER dans `feature/BACK-079-monitoring-alerts`** (cette semaine).

---

### DEC-040 : MVP V1 sans scan IA — feature reportée en V2

- **Statut** : 🟡 SUPERSEDED par [DEC-043](#dec-043--pivot-du-provider-ia-par-défaut--vision-llm-google-gemini-plutôt-quocr--text-only-llm-groq). Le scan IA est réactivé pour V1, feature flag `Features:ScanRecipeEnabled=true` par défaut en prod. Le feature flag [BACK-092](../documentation/BACKLOG.md#back-092) créé pour cette DEC reste dans le code comme kill switch d'urgence, activable par environnement.
- **Date** : 19 juillet 2026 (identifiée pendant la revue post-BACK-033 UI)

- **Choix** : Livrer la V1 de MemoRecipe en prod publique avec **uniquement la création manuelle de recettes**. La feature "scan de recette par IA" (feature initialement identifiée comme core différenciant) est **reportée en V2**, après stabilisation V1 en prod avec de vrais utilisateurs.

- **Pourquoi ces choix** :
  - **Time-to-market réduit d'environ 2 semaines** : le chemin critique prod perd BACK-083 (sécurisation LLM, ~4-5h, P0 si IA activée), BACK-033 partie IA (~2h), BACK-072 (investigation qualité parsing, ~3h). Total ~10-12h de travail bloquant retirés du chemin critique V1.
  - **Focus qualité UX manuelle** : sans le "wow factor" IA à cacher les défauts, l'UX du formulaire manuel doit être excellente. BACK-090 (refonte mobile-first) devient encore plus critique et prend toute l'attention V1.
  - **Coûts LLM différés** : pas d'appels payants Mistral/Gemini/Groq en V1 → zéro coût variable prod, pas de surveillance de dépassement, pas de risque d'abus par utilisateur malveillant.
  - **Sécurité LLM (BACK-083) devient non-bloquante V1** : les 4 axes (prompt injection prevention + rate limit IA + audit trail + monitoring coûts) ne sont critiques que si le scan IA est actif. En V1 avec IA désactivée, le risque OWASP LLM01-10 disparaît.
  - **Feedback utilisateur réel avant sur-ingénierie** : lancer V1 en manuel permet de mesurer si les users veulent vraiment le scan IA (peut-être qu'ils préfèrent copier-coller depuis un blog), ou s'ils préfèrent un autre pattern (import depuis URL, import massif via export/import).

- **Alternatives écartées** :
  - **Lancer V1 avec IA activée mais qualité imparfaite** : risque de coûts LLM incontrôlés + expérience utilisateur dégradée (parsing incomplet → recettes fausses → utilisateurs déçus). Renoncement à une belle vitrine tant que la qualité n'est pas prouvée.
  - **Attendre une qualité IA parfaite pour V1** : violation du principe MVP. Le "parfait" n'arrive jamais sans feedback réel. On boucle sur BACK-072 (investigation qualité) indéfiniment sans jamais publier.
  - **V1 = uniquement scan IA sans création manuelle** : impossible fonctionnellement. La saisie manuelle est le fallback obligatoire pour tous les cas où le scan échoue.

- **Sources** :
  - Principe MVP de Eric Ries ("The Lean Startup") — ship early, iterate with real feedback
  - Pattern "Feature Flag" pour cacher progressivement les features (Martin Fowler)
  - Retour d'expérience produit : les features "wow" reportées en V2 sont souvent découvertes moins critiques une fois les utilisateurs interrogés

- **Conséquences** :
  - **Nouveau ticket [BACK-092](../documentation/BACKLOG.md#back-092)** : feature flag pour désactiver le scan IA en V1 (~30 min-1h, P1).
  - **[BACK-033](../documentation/BACKLOG.md#back-033)** : marqué 🟠 EN COURS (partie UI DONE le 19/07, partie prompt IA structuré reportée V2).
  - **[BACK-083](../documentation/BACKLOG.md#back-083)** : P0 conservé sur le principe (bloquant IA), mais **non-bloquant V1** (car IA désactivée). Nouveau libellé : "P0 pour activation V2".
  - **[BACK-072](../documentation/BACKLOG.md#back-072)** : investigation qualité parsing IA → reportée V2.
  - **[BACK-069](../documentation/BACKLOG.md#back-069)** : Bring-Your-Own-IA → également V2 (dépend du scan IA).
  - **[BACK-090](../documentation/BACKLOG.md#back-090)** : reste P1 mais devient prioritaire absolu — la création manuelle sera la seule voie utilisable en V1.
  - **Chemin critique V1 révisé** : BACK-033 UI ✅ + BACK-090 + BACK-085 + BACK-092 + BACK-007p3 → **prod V1 estimée S31 (fin juillet - début août)**.
  - **UI du bouton "Importer une recette"** masqué en V1 via feature flag. Route `/recipes/scan` retirée du sitemap public V1.

- **Conditions qui invalideraient ce choix** :
  - **Feedback beta massif "je veux le scan IA absolument"** avant même la sortie V2 : accélérer la reintégration (activer feature flag + prioriser BACK-033 partie IA + BACK-083 immédiatement).
  - **Percée qualité LLM inattendue** (nouveau modèle open source ultra-fiable, prompt engineering breakthrough) : re-évaluer la maturité de la partie IA avant V2.
  - **Décision produit de repositionner MemoRecipe comme "app IA-first"** au lieu de "gestion de recettes personnelle" : ce cas nécessiterait de réactiver le scan IA comme feature core V1 (mais changement de vision produit, hors scope de cette décision).

- **État** : DÉCIDÉ le 19/07/2026 pendant la revue post-BACK-033 UI. APPLIQUÉ immédiatement : DEC-040 ajoutée, BACKLOG mis à jour pour cohérence (BACK-033 statut, BACK-083/072/069 note contexte V1/V2, nouveau BACK-092 feature flag). **Complément frontend** : le feature flag `Features:ScanRecipeEnabled` est propagé à la navigation Web via un service `IFeatureFlagsService` et un champ `_scanEnabled` dans les pages. Quand `ScanRecipeEnabled = false`, les entrées de navigation "Scanner une recette" sont conditionnellement masquées dans `SideBar`, `BottomNavBar` et les bandeaux info `Privacy` et `Legal` (pattern fail closed côté client, complémentaire de la protection API).

- **⚠️ SUPERSEDED par [DEC-043](#dec-043) (09/08/2026)** : cette DEC-040 est **caduque**. Deux pivots stratégiques successifs ont inversé la décision :
  - **06/08/2026** (pivot #1) : décision de réintégrer le scan IA en V1 avec approche Groq-only MVP + safeguards (au lieu du report V2 initialement acté ici). Motivations : le scan IA est une feature core différenciante du produit ; ship V1 sans scan = beta test dénaturé + feedback biaisé.
  - **09/08/2026** (pivot #2 [DEC-043](#dec-043)) : pivot du provider IA par défaut vers Vision LLM (Gemini) suite à baseline mesurée révélant écart qualitatif ×4 par rapport au pipeline OCR + text-only initialement prévu.
  - **Conséquences concrètes du remplacement** : le feature flag [BACK-092](../documentation/BACKLOG.md#back-092) créé par cette DEC reste utile (kill switch d'urgence, activation contrôlée par environnement), mais il est désormais activé par défaut en prod V1 (`ScanRecipeEnabled = true`). Les tickets [BACK-033](../documentation/BACKLOG.md#back-033), [BACK-072](../documentation/BACKLOG.md#back-072), [BACK-069](../documentation/BACKLOG.md#back-069), [BACK-083](../documentation/BACKLOG.md#back-083) reprennent leur criticité V1 (adaptés au nouveau provider Vision).
  - **Pourquoi garder cette DEC dans le doc plutôt que la supprimer** : traçabilité de l'historique de décision (portfolio / futur audit) + explication du feature flag [BACK-092](../documentation/BACKLOG.md#back-092) qui reste dans le code même après réactivation du scan.

---

### DEC-041 : `MainLayout.razor` — code-behind extrait, CSS `<style>` inline conservé pragmatiquement

- **Statut** : ✅ ACTIVE
- **Date** : 22 juillet 2026 (identifiée pendant BACK-096 phase 2)

- **Choix** : Extraire le C# (thème MudBlazor) dans `MainLayout.razor.cs` en partial class, **mais garder le bloc `<style>` inline** dans `MainLayout.razor` au lieu de créer un `MainLayout.razor.css` scoped.

- **Pourquoi ces choix** :
  - **Code-behind gagnant** : la déclaration du `MudTheme` (~40 lignes de C#) sort du markup, cohérent avec la convention post-BACK-090 (`[Inject]`, `default!`, partial class dans `.razor.cs`). Aucun compromis.
  - **CSS scoped problématique sur ce fichier spécifique** : la classe `main-content-with-mobile-padding` cible `<MudMainContent>` (sous-composant MudBlazor). Le scoping Blazor n'ajoute pas l'attribut `b-xxx` aux éléments internes des sous-composants → nécessite `::deep .main-content-with-mobile-padding { ... }` pour matcher.
  - **Reproduction du choix pragmatique BACK-090** : la même problématique avait déjà été rencontrée et résolue de la même façon (CSS inline gardé pour éviter le coût cognitif de `::deep`). Renoncement conscient documenté cette fois-ci pour éviter le débat une 3ème fois.
  - **Nouveaux styles compatibles avec le pattern inline** : le skip-link a11y (`.skip-link`, `#main-content:focus`) ajouté en BACK-096 s'insère naturellement dans le même bloc `<style>` — pas de gain à les extraire séparément.

- **Alternatives écartées** :
  - **Extraire tout en `.razor.css` avec `::deep`** : cohérence maximum, mais rétablit ce qui avait été abandonné consciemment en BACK-090. Coût cognitif > bénéfice pour un cas isolé.
  - **Déplacer le CSS dans `wwwroot/css/app.css` global** : casse la co-localisation "styles proches du composant qui les utilise". Pas de scoping non plus.
  - **Attendre que MudBlazor expose une API `id` sur `<MudMainContent>`** : dépendance externe non contrôlée, pas de garantie de calendrier.

- **Sources** :
  - Journal BACK-090 (20/07/2026) : "CSS scoped abandonné pour `MainLayout`, gardé pour `RecipeStickyActionBar`" — décision initiale documentée
  - Documentation officielle Blazor CSS isolation : le `::deep` combinator est le mécanisme officiel pour cibler les descendants de sous-composants, mais son usage est réservé aux cas où il apporte plus qu'il ne coûte
  - Rule of Two Hats (Kent Beck) : ne pas mélanger refonte visuelle et refonte technique dans la même feature — la BACK-096 est une refonte visuelle a11y, pas une refonte technique de l'architecture CSS

- **Conséquences** :
  - **Nouveau ticket [BACK-098](../documentation/BACKLOG.md#back-098)** : "Explorer une solution CSS scoped pour `MainLayout.razor` sans `::deep`" (P3, ~30 min, dette technique post-V1). À réévaluer quand MudBlazor évoluera ou qu'une pause qualité sera dispo.
  - **`MainLayout.razor.cs` créé** : partial class avec le champ `readonly MudTheme _theme` uniquement, pas de base class explicite (unifiée avec le `@inherits LayoutComponentBase` du `.razor`).
  - **Convention documentée** : pour les autres layouts / composants où le CSS ne cible que des éléments directs (pas de sous-composants), continuer à extraire en `.razor.css` scoped (pattern BACK-090 respecté).
  - **Le fichier `MainLayout.razor`** reste hybride : markup + `<style>` inline. C'est un choix conscient, pas un oubli.

- **Conditions qui invalideraient ce choix** :
  - **MudBlazor expose `Id` ou équivalent scopable sur `MudMainContent`** : permettrait de cibler directement l'élément rendu avec l'attribut `b-xxx` sans `::deep`. Trigger pour BACK-098.
  - **Refonte plus large de l'architecture CSS** (ex : migration Tailwind, adoption CSS Modules côté Blazor) : le débat inline vs scoped devient obsolète. Trigger pour repenser globalement.
  - **Ajout d'un 3ème cas similaire** (styles inline dans un autre layout/composant à cause de `::deep`) : signal que le pattern devient une dette systémique, il faudra une décision architecturale globale.

- **État** : DÉCIDÉ le 22/07/2026 pendant BACK-096. APPLIQUÉ : `MainLayout.razor` refactoré avec code-behind + skip-link, CSS inline conservé, ticket BACK-098 créé pour re-exploration future.

---

### DEC-042 : Hébergement production — VPS Lite dédié (Docker manuel) + IA sur plateforme serverless externe

- **Statut** : ✅ ACTIVE (à appliquer au prochain runbook de déploiement, BACK-009)
- **Date** : 04 août 2026

- **Choix** :
  1. MemoRecipe se déploie sur un **VPS Linux dédié** (offre d'entrée de gamme d'un hébergeur européen), distinct de tout autre serveur mutualisé.
  2. **Docker n'est pas préinstallé** : il est installé manuellement via l'accès root standard (SSH + élévation de privilèges), une fois la distribution Linux choisie au provisioning.
  3. Le composant IA (OCR + appel LLM, cf. [DEC-018](#dec-018)) reste un **service externe** appelé par l'API via une URL configurable — il n'est **pas** conteneurisé sur le VPS. Il cible une plateforme serverless de type "container à la demande" (facturation à l'usage), et non un plan toujours-actif.
  4. Le VPS reste dimensionné pour l'usage réel actuel (site Blazor WASM + API .NET + PostgreSQL en Docker Compose), avec un chemin de montée en gamme (offre supérieure du même hébergeur) disponible sans changement d'architecture si le trafic croît.

- **Pourquoi ces choix** :
  - **Serveur dédié plutôt que mutualisation avec d'autres projets** : un service en production ne doit pas dépendre de la charge d'autres usages sur la même machine. L'isolation complète élimine tout risque de contention de ressources ou d'incident croisé entre projets indépendants.
  - **VPS d'entrée de gamme plutôt qu'une offre supérieure** : l'empreinte mémoire des 4 services du `docker-compose.prod.yml` (web, api, postgres, backup) est modeste et documentée (plafonds `mem_limit` par service). Payer pour une capacité inutilisée n'apporte aucune valeur tant que le trafic réel ne le justifie pas ; la migration vers une offre supérieure du même hébergeur est un simple changement de palier, pas une migration technique.
  - **Docker manuel plutôt qu'un PaaS packagé** : cohérent avec le pipeline Docker Compose déjà construit et durci (BACK-007, [DEC-027](#dec-027), [DEC-031](#dec-031)) — l'accès root sur une distribution Linux standard suffit à installer Docker sans dépendre d'une offre PaaS propriétaire de l'hébergeur.
  - **IA sur plateforme serverless externe plutôt que conteneurisée sur le VPS** : le projet IA dépend de librairies natives de reconnaissance optique de caractères, et son usage est ponctuel et irrégulier (un appel par scan utilisateur), avec des pics CPU sur des opérations courtes. Un modèle serverless facturé à l'usage absorbe cette charge sans dimensionner le VPS en permanence pour des pics rares, et sans faire concurrence aux services toujours actifs (site, API, BDD) sur la même machine. Le rate-limiting déjà prévu ([BACK-083](../documentation/BACKLOG.md#back-083)) plafonne par ailleurs le risque de dérive de coût liée à l'usage.

- **Alternatives écartées** :
  - **Cohabitation avec d'autres sites déjà hébergés sur un serveur mutualisé existant** : écartée pour isolation complète et suppression de tout risque de contention de ressources entre projets indépendants.
  - **Tout héberger sur une plateforme cloud managée (site + API + BDD)** : écartée — une charge "toujours active" (site/API/BDD tournant en continu) est structurellement plus coûteuse sur des services managés facturés en continu que sur un serveur dédié à coût fixe, et cela abandonnerait le pipeline Docker Compose déjà construit et testé sans bénéfice fonctionnel.
  - **Conteneuriser le projet IA sur le même VPS que le reste** : écartée — ferait concurrencer un workload CPU-intensif ponctuel avec les services always-on, et imposerait un palier VPS supérieur en permanence pour un besoin qui n'est qu'occasionnel.
  - **PaaS Docker managé propriétaire de l'hébergeur** : écartée — moins de contrôle et moins de valeur d'apprentissage que la gestion directe d'un VPS + Docker Compose déjà maîtrisée.

- **Sources** :
  - Documentation officielle de l'hébergeur sur l'accès root SSH des offres VPS concernées.
  - Documentation officielle Docker — installation standard sur une distribution Linux avec accès root (indépendante de tout hébergeur spécifique).
  - Documentation officielle de la plateforme serverless ciblée pour l'IA — modèle de facturation à l'usage avec quota gratuit mensuel.

- **Conséquences** :
  - [BACK-009](../documentation/BACKLOG.md#back-009) (HTTPS forcé) doit désormais prévoir une installation complète du reverse proxy + certificat sur un serveur vierge, et non l'ajout d'un site virtuel supplémentaire sur un reverse proxy déjà en place — le runbook sera mis à jour en conséquence.
  - Le déploiement futur du scan IA (V2, [BACK-033](../documentation/BACKLOG.md#back-033) / [BACK-083](../documentation/BACKLOG.md#back-083)) ciblera la plateforme serverless externe, pas le VPS — à documenter dans le runbook dédié le moment venu.

- **Conditions qui invalideraient ce choix** :
  - **Croissance de trafic dépassant la capacité du palier choisi** → montée de gamme chez le même hébergeur, sans changement d'architecture Docker Compose.
  - **Volume d'usage IA dépassant durablement le quota gratuit de la plateforme serverless** → réévaluer le modèle de facturation ou l'hébergement de ce composant.

- **État** : DÉCIDÉ le 04/08/2026. À appliquer au prochain runbook de déploiement ([BACK-009](../documentation/BACKLOG.md#back-009)).

---

### DEC-043 : Pivot du provider IA par défaut — Vision LLM (Google Gemini) plutôt qu'OCR + text-only LLM (Groq)

- **Statut** : 🟡 PARTIELLEMENT SUPERSEDED par [DEC-045](#dec-045). Le principe fondamental de cette DEC reste valide : Vision LLM multimodal direct plutôt qu'OCR local + text-only LLM (écart qualitatif mesuré environ 4× sur les photos complexes). En revanche, le choix spécifique du provider Vision (Google Gemini Flash-Lite) a été remplacé par MistralVision suite à la contrainte prepayment AI Studio découverte lors du test runtime post-implémentation code (US-A2-10). MistralVision offre les mêmes bénéfices multimodaux avec en plus l'hébergement UE, la souveraineté RGPD-native et un free tier accessible sans carte bancaire.
- **Date** : 09 août 2026

- **Choix** :
  1. **Google Gemini Vision (modèle Flash-Lite)** devient le provider par défaut pour la fonctionnalité de scan de recettes en V1, à la place du pipeline précédemment retenu qui consistait à extraire le texte de l'image via OCR local (Tesseract) puis à structurer ce texte via un LLM text-only (Groq / Llama 3.3 70B, cf. [DEC-036](#dec-036)).
  2. Le pipeline OCR local + provider text-only reste **présent dans le code** en tant que **provider de fallback secondaire**, sélectionnable via la variable d'environnement `AI_PROVIDER` (cf. [DEC-035](#dec-035)). Aucun retrait de code — le Factory Pattern existant permet la coexistence sans surcoût de maintenance.
  3. L'authentification à l'API Vision utilise une **clé API bind à un compte de service** (pattern IAM standard du fournisseur cloud), stockée en **Docker Secret** en production (pattern [BACK-004](../documentation/BACKLOG.md#back-004)) et via `appsettings.Development.json` gitignored en développement.
  4. Un **plafond budgétaire mensuel** est configuré au niveau du compte cloud du projet, avec **coupure automatique du service** en cas de dépassement (protection "hard cap" absolue).

- **Pourquoi ces choix** :
  - **Écart qualitatif mesuré massif sur cas complexes** : sur un scan comparatif d'une photo de recette à mise en page artistique (page magazine / réseau social), le pipeline OCR local + LLM text-only atteint un score d'extraction d'environ 25% (titre tronqué, plusieurs champs métier absents du schéma, hallucinations sur les ingrédients, quantités et fractions massacrées par l'étape OCR intermédiaire), alors que le Vision LLM sur la même image atteint environ 95% (extraction complète, aucune hallucination, structuration correcte des étapes). Ratio approximatif de 4× en qualité perçue sur ce type de source.
  - **Cause racine du différentiel** : l'étape OCR intermédiaire dégrade massivement le signal (chiffres/fractions perdus, éléments non-textuels ignorés, layout créatif non compris). Un modèle multimodal lit directement l'image et exploite le contexte visuel (positionnement, encadrés, hiérarchie typographique) — signal qui est structurellement inaccessible à un pipeline OCR → text.
  - **Budget compatible avec l'usage cible** : le modèle Flash-Lite est facturé à un ordre de grandeur d'environ $0.0004 par scan (mesuré ~2000 tokens par scan moyen, incluant l'encodage image en tuiles + prompt + output JSON). Pour un usage cible beta (~100 scans/jour), coût mensuel estimé ~$1. Pour un scale V1 stable (~500 scans/jour), ~$5/mois. Le crédit d'essai gratuit du fournisseur cloud (~$300 sur 90 jours pour un nouveau compte) couvre plusieurs années de beta à ce rythme.
  - **Résilience via Factory Pattern déjà en place** : le pattern retenu en [DEC-035](#dec-035) permet de switcher entre providers via une simple variable d'environnement, sans redéploiement de code. Si le fournisseur Vision change sa politique commerciale ou technique de manière défavorable, un basculement vers le provider de fallback (OCR + text-only) est immédiat.
  - **Support natif multi-formats** : le provider Vision accepte nativement JPEG, PNG, WebP, HEIC et PDF. Cela rend obsolète le ticket initialement prévu pour ajouter le support WebP côté backend ([BACK-039](../documentation/BACKLOG.md#back-039) partie WebP) — la fonctionnalité est acquise sans code additionnel.

- **Alternatives écartées** :
  - **Rester sur le pipeline OCR + text-only et améliorer le prompt** : écarté — l'analyse du prompt existant révèle des champs métier absents du schéma JSON demandé (ex : temps de préparation, temps de cuisson, difficulté), ce qui explique une partie des extractions manquantes. Corriger ce point améliorerait le score mais ne résoudrait pas la cause racine (OCR dégradé sur cas complexes). Le plafond de qualité atteignable resterait insuffisant pour l'expérience utilisateur cible.
  - **Rester sur le pipeline OCR et investir dans un OCR de meilleure qualité** (préprocessing image + moteur OCR différent) : écarté pour V1 — chantier de plusieurs jours pour un gain incrémental, alors qu'un modèle multimodal résout le problème structurellement et immédiatement. Amélioration éventuelle du chemin de fallback reportée post V1 (initialement tracée dans US-A2-08 Alpha.2 puis reportée dans [BACK-105](../documentation/BACKLOG.md#back-105) au scope élargi WebP + PDF + HEIC + refacto utilitaire de validation, décision consolidée le 21/08/2026).
  - **Auto-hébergement d'un modèle Vision open-source** (par exemple modèle multimodal libre exécuté sur GPU) : écarté pour V1 — nécessite un investissement matériel significatif (GPU dédié) et une expertise d'exploitation qui ne se justifie ni à l'échelle actuelle ni au coût unitaire mesuré du provider cloud. Reste envisageable en V3 si un enjeu de souveraineté des données ou de scale change la donne.
  - **Fournisseur Vision commercial d'un tiers différent** (ex : autre grand acteur cloud, ou API tierce spécialisée) : écarté pour V1 — le fournisseur retenu offre le meilleur ratio qualité/prix mesuré + un free tier généreux + un modèle de facturation à l'usage prévisible. Le pattern Factory permet de rebasculer sans coût architectural si un autre acteur devient plus intéressant plus tard.

- **Sources** :
  - Test qualitatif comparatif réalisé sur une photo de recette réelle représentative des cas d'usage difficiles ciblés (mise en page artistique multi-zones), avec le même prompt d'extraction JSON structuré pour les deux providers testés.
  - Documentation officielle de tarification du fournisseur cloud Vision (grille de prix par million de tokens input/output du modèle Flash-Lite).
  - Documentation officielle des quotas et rate-limits du free tier de ce fournisseur.
  - Fiche interne `parsing-quality-baseline` documentant les résultats bruts du test comparatif (photo source, sortie JSON de chaque provider, scoring par champ).

- **Conséquences** :
  - Nouvelle User Story [US-A2-10](../documentation/Backlog_V1-Alpha2.md) ajoutée à l'Alpha.2 : implémentation du provider Vision dans le Factory existant + adaptation du pipeline scan pour passer l'image directement au provider Vision (au lieu de la faire transiter par l'OCR local).
  - [US-A2-03](../documentation/Backlog_V1-Alpha2.md) (baseline complète 5-10 scans du pipeline précédent) devient obsolète et est marquée SUSPENDUE — un scan comparatif unique a suffi à trancher, une baseline formelle du chemin abandonné n'a plus de valeur produit.
  - [US-A2-04](../documentation/Backlog_V1-Alpha2.md) (sécurisation LLM — [BACK-083](../documentation/BACKLOG.md#back-083)) doit être adaptée : les patterns de prompt-injection et de rate-limiting restent identiques, mais l'audit trail et le comptage de tokens ciblent le format de réponse du provider Vision.
  - [US-A2-05](../documentation/Backlog_V1-Alpha2.md) (alertes coûts — [BACK-083](../documentation/BACKLOG.md#back-083) §3) : seuils d'alerte à recalibrer selon la grille tarifaire du provider Vision (ordre de grandeur ~$0.0004 par scan Flash-Lite).
  - [US-A2-07](../documentation/Backlog_V1-Alpha2.md) (optimisation prompt) : le prompt de référence devient la version enrichie testée sur AI Studio (schéma JSON complet 8 champs, règles anti-hallucination, instruction de titre complet, préservation de l'ordre des étapes).
  - [US-A2-08](../documentation/Backlog_V1-Alpha2.md) (initialement "Support WebP" — [BACK-039](../documentation/BACKLOG.md#back-039)) : SUSPENDUE le 21/08/2026 fusionnée avec [BACK-105](../documentation/BACKLOG.md#back-105) post V1 (scope élargi "Support formats étendus uploads WebP + PDF + HEIC + refacto utilitaire `IFileUploadValidator`"). Le support WebP est acquis nativement au niveau du modèle Vision (Mistral, Gemini) mais reste bloqué au niveau contrôleur API tant que BACK-105 n'est pas implémenté (cf. mise à jour post-vérification de [DEC-025](#dec-025)). Décision de suspension motivée par (a) WebP marginal sans MAUI mobile natif (photos importées galerie téléphone = JPEG à ~99%), (b) cohérence refacto groupé plutôt qu'ajouts épars.
  - Note de confidentialité utilisateur : le contenu OCR (privacy Section 9) reste factuellement exact mais la description du fournisseur IA doit être mise à jour lors du déploiement (transfert désormais chez le nouveau fournisseur au lieu du précédent, conditions contractuelles à re-vérifier avant activation prod).
  - Décision produit associée : les recettes créées via le scan sont forcées en visibilité privée par défaut (droits d'auteur potentiels sur les recettes reproduites depuis livres/magazines/blogs) — à implémenter dans [US-A2-06](../documentation/Backlog_V1-Alpha2.md) et à documenter dans la clause de responsabilité utilisateur des mentions légales.

- **Conditions qui invalideraient ce choix** :
  - **Changement défavorable de la politique commerciale du fournisseur** (retrait du free tier, hausse tarifaire massive, restriction géographique) → bascule vers le provider fallback via le Factory Pattern, sans changement architectural. Coût de bascule = quelques minutes de configuration.
  - **Dégradation mesurée de la qualité du modèle Flash-Lite** (hallucinations accrues, régression sur cas simples) → basculer vers le modèle Flash standard (5× plus cher mais dans les limites du budget cap) ou vers le provider fallback.
  - **Enjeu réglementaire spécifique** (souveraineté des données imposée, exigence RGPD stricte incompatible avec un transfert hors UE) → réévaluer avec un provider Vision UE ou un modèle auto-hébergé.

- **État** : DÉCIDÉ le 09/08/2026. Setup du compte cloud, activation de l'API, budget cap et clé API validés le même jour. Implémentation code (US-A2-10) à réaliser à la prochaine session.

---

### DEC-044 : Extension de `AI_PROVIDER` avec la valeur `"GeminiVision"` (plutôt qu'une seconde variable d'environnement dédiée au mode de scan)

- **Statut** : ✅ ACTIVE, avec extension formalisée par [DEC-045](#dec-045). L'approche "une seule variable d'environnement pour porter le mode de scan" reste retenue, et le mécanisme s'est étendu d'une cinquième à une sixième valeur : `MistralVision` a été ajoutée dans la même logique de switch case (voir DEC-045). Aujourd'hui `AI_PROVIDER` accepte 6 valeurs : `Fake` (dev uniquement), `Mistral`, `Gemini`, `Groq`, `MistralVision`, `GeminiVision`.
- **Date** : 10 août 2026

- **Choix** :
  1. L'implémentation Vision ([US-A2-10](../documentation/Backlog_V1-Alpha2.md)) est sélectionnée via l'ajout d'une **cinquième valeur `"GeminiVision"`** à la variable d'environnement existante `AI_PROVIDER` (à côté de `Fake`, `Mistral`, `Gemini`, `Groq`).
  2. Cette valeur déclenche simultanément l'enregistrement du client `IVisionCompletionClient` (branche Gemini Vision multimodal) et la substitution du pipeline `RecipePipeline` (chemin OCR + text LLM) par `VisionRecipePipeline` (chemin direct image → LLM multimodal).
  3. Aucune nouvelle variable d'environnement n'est introduite.

- **Pourquoi ces choix** :
  - **YAGNI** : un seul provider Vision est retenu pour V1 (cf. [DEC-043](#dec-043)). Découpler la sélection du "provider" et du "mode de scan" via deux variables orthogonales n'apporte aucune valeur tant qu'il n'existe qu'un seul provider Vision.
  - **Cohérence avec le pattern existant** : la sélection par `AI_PROVIDER` + `switch` sur la valeur est déjà en place pour les quatre providers text-only (cf. [DEC-035](#dec-035)). Ajouter un cinquième case préserve l'homogénéité opérationnelle et documentaire.
  - **Simplicité opérationnelle** : une seule variable à documenter dans les runbooks de déploiement, une seule valeur à modifier lors d'un basculement de fournisseur.

- **Alternatives écartées** :
  - **Deux variables orthogonales** (`AI_PROVIDER` + `SCAN_MODE=OcrText|Vision`) : écartée pour V1 — introduit une combinatoire de configurations (N providers × 2 modes, dont plusieurs combinaisons invalides à documenter et à valider) sans bénéfice fonctionnel tant qu'il n'existe qu'un seul provider Vision. La lisibilité opérationnelle (une variable = un mode complet) prime.

- **Réversibilité** : refactor trivial vers deux variables orthogonales si un second provider Vision est introduit (par exemple, un autre acteur multimodal cloud). Le `switch case` s'éclate alors en deux résolutions successives, sans changement du contrat externe côté consommateurs de la fonction serverless.

- **Impact** : `AI_PROVIDER` accepte désormais **cinq valeurs** : `Fake` (dev uniquement), `Mistral`, `Gemini`, `Groq`, `GeminiVision`. Message d'erreur du `default` case mis à jour en conséquence. Documentation opérationnelle (`.env.example`, `DEPLOYMENT.md`) à mettre à jour lors du prochain déploiement.

- **État** : DÉCIDÉ le 10/08/2026, implémenté dans le cadre de [US-A2-10](../documentation/Backlog_V1-Alpha2.md).

---

### DEC-045 : Plan test Mistral Vision (priorité) + Groq Vision (fallback conditionnel) — activation runtime Vision sans coût

- **Statut** : ✅ ACTIVE. Mistral Vision retenu comme provider Vision par défaut V1 (test qualité 11/08/2026 sur photo magazine ~85%, au-dessus du seuil 60% déclencheur du fallback). L'US-A2-13 (implémentation GroqVisionCompletionClient de fallback conditionnel) n'a donc **pas été déclenchée**, aucun `GroqVisionCompletionClient` n'existe dans le codebase à ce jour.
- **Date** : 10 août 2026

- **Choix** :
  1. Le provider Vision par défaut visé pour l'Alpha.2 devient **Mistral Vision** (modèles Small / Medium / Large avec vision native intégrée, Experiment tier gratuit sans carte bancaire, hébergement UE, RGPD-natif, EU AI Act compliant).
  2. Le provider **Groq Vision** (`qwen/qwen3.6-27b`, free tier 30 RPM / 1000 RPD, sans carte bancaire) est retenu comme **fallback conditionnel** — implémenté et adopté uniquement si Mistral Vision se révèle insuffisant sur la qualité empirique mesurée OU sur la contrainte de débit (Mistral Experiment tier = 2 RPM, potentiellement limitant en beta multi-utilisateurs concurrents).
  3. Le provider **Gemini Vision** (implémentation code réalisée en [US-A2-10](../documentation/Backlog_V1-Alpha2.md)) est **conservé dans le codebase** en tant que provider optionnel via le pattern Factory existant, mais **retiré du chemin runtime par défaut** — l'authentification actuelle passe par le système AI Studio prepayment qui requiert un mode payant avec crédits prépayés, incompatible avec la contrainte opérationnelle "zéro coût fournisseur cloud" du projet en phase Alpha/Beta.

- **Pourquoi ces choix** :
  - **Souveraineté européenne** : Mistral AI (siège Paris, hébergement UE, RGPD-natif) est le seul acteur Vision multimodal européen offrant un free tier accessible sans carte bancaire en 2026. Réduit la surface RGPD (pas de transfert hors UE) et constitue un argument différenciateur produit (SaaS respectueux des données européennes).
  - **Contrainte zéro coût opérationnel** : le provider Gemini nécessite un compte AI Studio en mode payant avec crédits prépayés — incompatible avec la posture "aucun paiement fournisseur cloud avant beta publique validée" du projet. Ce point a été découvert lors du test runtime post-implémentation code (erreur `429 RESOURCE_EXHAUSTED / prepayment credits depleted` retournée par l'API Vision quel que soit le volume de requêtes).
  - **Cohérence architecture** : le pattern `IVisionCompletionClient` (Strategy) déjà en place permet l'ajout de Mistral Vision et Groq Vision côte à côte avec zéro modification structurelle — juste un nouveau case dans le switch factory (cf. [DEC-044](#dec-044) pour la logique d'extension d'`AI_PROVIDER`).
  - **Prudence sur qualité empirique** : la qualité du modèle Mistral Vision sur des photos représentatives (mise en page magazine, recette manuscrite, capture de blog) n'est pas encore mesurée. Le fallback Groq Vision est prévu et budgeté si Mistral n'atteint pas le seuil de qualité utilisable.

- **Alternatives écartées** :
  - **Activer le prépaiement Gemini AI Studio (~$5 initial)** : écarté pour Alpha.2 — contrevient à la posture "zéro coût" active. Reste envisageable en V1 stable si les alternatives européenne et Groq échouent conjointement sur la qualité.
  - **Rester exclusivement sur le chemin OCR + LLM text-only** : écarté — la baseline comparative du 09/08 (cf. [DEC-043](#dec-043)) démontre un écart qualitatif de ~4× (Vision ~95% vs OCR+text ~25%) sur les photos à mise en page complexe qui constituent une part significative des cas d'usage cibles.
  - **Auto-hébergement d'un modèle Vision open-weight local** (LLaVA / Qwen VL / Pixtral open) : écarté pour V1 — nécessite GPU dédié + expertise d'exploitation disproportionnée à l'échelle actuelle.
  - **Autre provider Vision commercial** (Anthropic Claude Vision, OpenAI GPT-4 Vision, etc.) : écartés — tous demandent un compte payant avec carte bancaire, aucun ne propose de free tier Vision comparable à Mistral ou Groq.

- **Réversibilité** : refactor trivial. Le pattern `IVisionCompletionClient` permet d'ajouter/retirer/changer le provider par défaut via une simple modification du `switch case` dans `Program.cs` et de la variable d'environnement `AI_PROVIDER`, sans changement de contrat externe ni de logique métier.

- **Impact** :
  - Nouvelle **US-A2-12** ajoutée à l'Alpha.2 : implémentation `MistralVisionCompletionClient` + wire DI + test qualité + décision provider par défaut.
  - Nouvelle **US-A2-13** (conditionnelle) ajoutée à l'Alpha.2 : implémentation `GroqVisionCompletionClient` en fallback si Mistral qualité insuffisante.
  - [DEC-043](#dec-043) conserve sa validité conceptuelle (pivot Vision LLM vs OCR + text-LLM démontré empiriquement) mais son choix de provider spécifique (Gemini Flash-Lite) sera **révisé** après le test qualité empirique en US-A2-12.
  - [DEC-044](#dec-044) (extension `AI_PROVIDER` avec `"GeminiVision"`) reste valide — les valeurs `"MistralVision"` et éventuellement `"GroqVision"` seront ajoutées dans la même logique de switch.

- **Conditions qui invalideraient ce choix** :
  - **Mistral Vision qualité < 60%** sur les photos test représentatives → bascule sur Groq Vision (US-A2-13 déclenchée).
  - **Rate limit Mistral Experiment tier trop restrictif** sur la charge beta réelle (utilisateurs concurrents) → bascule sur Groq Vision (30 RPM vs 2 RPM).
  - **Groq Vision qualité également insuffisante** → réévaluation de la posture "zéro coût" (activation du prépaiement Gemini OU décalage de l'activation Vision en V1.1 avec chemin fallback OCR+text-LLM en couverture).

- **État** : DÉCIDÉ le 10/08/2026. Implémentation Mistral Vision (US-A2-12) prévue pour la session du 11/08/2026.

---

### DEC-046 : Architecture pipeline Vision (deux ports parallèles `IChatCompletionClient` et `IVisionCompletionClient`)

- **Statut** : ✅ ACTIVE
- **Date** : 10 août 2026 (initial via US-A2-10 puis stabilisation via US-A2-12 le 11/08/2026)

- **Choix** :
  1. Ajouter un nouveau port `IVisionCompletionClient` dans `memoRecipe-ia/Application/Interfaces/` avec la méthode `Task<LlmCompletionResult> CompleteWithImageAsync(string prompt, byte[] imageData, string mimeType)`. Ce port coexiste avec le port existant `IChatCompletionClient` qui reste inchangé (méthode `CompleteAsync(string prompt)` pour les providers text-only).
  2. Créer un nouveau pipeline `VisionRecipePipeline` dans `memoRecipe-ia/Application/Pipeline/` qui implémente `IRecipePipeline` (abstraction partagée avec `RecipePipeline` classique). Le VisionRecipePipeline lit l'image directement depuis le stream, la passe au modèle Vision, parse la réponse JSON, et retourne le `RecipeDto`. Aucun OCR intermédiaire.
  3. Sélection du pipeline conditionnelle dans `Program.cs` du worker IA, selon la valeur de la variable d'environnement `AI_PROVIDER`. Si la valeur est `MistralVision` ou `GeminiVision`, le worker enregistre `VisionRecipePipeline`. Sinon (Fake, Mistral, Gemini, Groq), il enregistre l'ancien `RecipePipeline` (OCR + text-only LLM).
  4. Les deux pipelines cohabitent dans le codebase, aucun n'est retiré. Le switch entre modes text-only et Vision reste trivial via une seule variable d'environnement.

- **Pourquoi ces choix** :
  - **Interface Segregation Principle (SOLID)** : les deux modes d'appel LLM sont fondamentalement différents (prompt string seul vs prompt + image binaire). Deux abstractions distinctes évitent qu'un provider text-only doive implémenter (ou throw NotSupportedException sur) une méthode qui n'a pas de sens pour lui.
  - **Compatibilité ascendante** : les 4 providers text-only existants (`FakeChatCompletionClient`, `MistralChatCompletionClient`, `GeminiChatCompletionClient`, `GroqChatCompletionClient`) restent inchangés. Aucune régression risquée sur l'existant.
  - **Extensibilité** : ajouter un nouveau provider Vision revient à créer une nouvelle classe qui implémente `IVisionCompletionClient` et ajouter un case dans le switch factory. Zéro impact sur les providers existants (Open/Closed Principle).
  - **Séparation OCR vs Vision** : le path OCR + text-only fait de la sanitization anti prompt-injection sur le texte extrait (`PromptSanitizer.Sanitize`), étape qui n'a pas de sens sur le path Vision (aucun texte utilisateur intermédiaire, l'image est l'input direct). Deux pipelines séparés permettent d'exprimer proprement cette différence sans branches conditionnelles dans un pipeline unique.
  - **Clarté DI et injection** : le worker inject soit `IChatCompletionClient` soit `IVisionCompletionClient` selon le provider actif, jamais les deux. Cohérent avec le pattern Strategy déjà en place ([DEC-035](#dec-035)).

- **Alternatives écartées** :
  - **Étendre `IChatCompletionClient` avec un paramètre image optionnel** : casse le principe SRP, force les 4 providers text-only à implémenter ou à ignorer un paramètre qui n'a pas de sens pour eux. Génère du code défensif inutile (throw NotSupportedException, etc.).
  - **Un seul pipeline avec branches conditionnelles** : mélange la logique OCR + PromptSanitizer + text-LLM avec la logique image + Vision-LLM dans un même fichier. Mauvais SRP, tests plus difficiles, refactor pénible.
  - **Union type ou variant C#** : pas idiomatique en .NET, aurait ajouté de la complexité pour zéro bénéfice.

- **Sources** :
  - SOLID Interface Segregation Principle
  - Ports and Adapters (Cockburn), pattern déjà utilisé dans le projet pour `INotificationChannel` (voir [DEC-039](#dec-039)) et `IChatCompletionClient` (voir [DEC-035](#dec-035))

- **Conséquences** :
  - Nouveau fichier `Application/Interfaces/IVisionCompletionClient.cs`
  - Nouveau fichier `Application/Pipeline/VisionRecipePipeline.cs`
  - Nouvelles implémentations `Infrastructure/AI/GeminiVisionCompletionClient.cs` (US-A2-10) et `Infrastructure/AI/MistralVisionCompletionClient.cs` (US-A2-12)
  - Sélection pipeline conditionnelle dans `Program.cs` du worker IA (2 cas Vision et 4 cas text-only)
  - Aucun impact sur l'API MemoRecipe (le worker reste appelé de la même façon via HTTP par `OcrScanService`)
  - Aucun impact sur le frontend (transparent)

- **Conditions qui invalideraient ce choix** :
  - Émergence d'un provider unifié text + vision natif dans lequel les deux modes d'appel seraient exposés par une interface commune officielle, cela rendrait l'abstraction séparée redondante.
  - Besoin d'un mode hybride Vision + OCR combinés (par exemple, extraire du texte via OCR pour l'analyse structurée en parallèle du Vision pour la mise en page). Ce cas nécessiterait un troisième pipeline ou une composition.
  - Refactor futur vers un mécanisme de plugins générique qui uniformiserait tous les modes d'appel LLM.

- **État** : APPLIQUÉ. Implémenté dans US-A2-10 (10/08/2026) avec `GeminiVisionCompletionClient` + `VisionRecipePipeline`, étendu dans US-A2-12 (11/08/2026) avec `MistralVisionCompletionClient` (retenu comme provider Vision par défaut V1, voir [DEC-045](#dec-045)). Test qualité photo magazine ~85% validé le 11/08/2026, au-dessus du seuil 60% déclencheur du fallback GroqVision. L'US-A2-13 (GroqVisionCompletionClient) n'a donc pas été déclenchée.

---

### DEC-047 : Sécurisation IA multi-couches (anti prompt-injection, rate limiter LLM, audit trail structuré, propagation tokens)

- **Statut** : ✅ ACTIVE
- **Date** : Fin juillet et début août 2026 (US-A2-04 en 3 sous-livraisons a/b/c)

- **Choix** :
  Le scan IA en production est protégé par 4 couches de défense en profondeur, chacune ciblant un risque OWASP LLM Top 10 distinct.

  1. **PromptSanitizer** (worker IA, `memoRecipe-ia/Application/Security/PromptSanitizer.cs`) : catalogue de 10 patterns regex OWASP LLM01 (jailbreak, role hijack, safety bypass, reveal prompt, etc.) appliqués sur le texte extrait par OCR avant envoi au LLM text-only. Un match throw `PromptInjectionDetectedException`, l'appel LLM est interrompu.
  2. **AiRateLimiter** (API, `MemoRecipe.Application/Services/AISecurity/AiRateLimiter.cs`) : rate limiter LLM-level à 4 tiers cumulatifs (per-user-hour, per-user-day, per-ip-hour, global-minute). Configuration via `AiRateLimitOptions` (POCO Options, tiers ajustables par environnement). Two-phase check pour fair enforcement (vérifie tous les tiers sans muter, incrémente seulement si tous passent). Throw `AiRateLimitExceededException` capté par le middleware d'exceptions qui répond 429 avec header `Retry-After`.
  3. **AiAuditLogger** (API, `MemoRecipe.Application/Services/AISecurity/AiAuditLogger.cs`) : logger structuré Serilog qui trace 3 événements de scan IA (`AiScanSuccess`, `AiScanBlocked`, `AiScanError`). Champs propagés : `UserId`, `Provider`, `TokensIn`, `TokensOut`, `DurationMs`, `InputHash`, `ErrorCode`. Aucune PII (pas d'image, pas de prompt, pas d'email). Sur success, appel automatique à `IAiCostCounter.IncrementAsync` pour propagation cost tracking (voir [DEC-048](#dec-048)).
  4. **LlmCompletionResult record** (worker IA) : type retour uniformisé pour tous les providers (`text`, `promptTokens`, `completionTokens`). Chaque provider parse la structure OpenAI-compatible `usage.prompt_tokens` et `usage.completion_tokens` de la réponse HTTP. Permet un tracking tokens uniforme cross-provider (text-only et Vision), consommé par l'audit logger et le cost counter.
  5. **AiInputHasher** (API, `MemoRecipe.Application/Services/AISecurity/AiInputHasher.cs`) : hash SHA256 déterministe calculé sur `userId + fileName + fileSize`. Permet la corrélation entre logs d'un même upload sans exposer l'image ni les données personnelles.

- **Pourquoi ces choix** :
  - **Défense en profondeur (Defense in Depth)** : chaque couche cible un risque OWASP LLM Top 10 distinct. LLM01 prompt injection (couche 1 PromptSanitizer), LLM04 model denial of service et LLM10 model theft via cost (couche 2 rate limiter), LLM06 sensitive information disclosure et LLM08 excessive agency observability (couche 3 audit + couche 5 input hashing). Si une couche est contournée, les suivantes offrent une protection résiduelle.
  - **RGPD Art. 5.1.c minimization** : aucun contenu utilisateur (image, texte OCR, prompt) n'est stocké dans les logs. Seul un hash SHA256 permet la corrélation entre événements. Le lecteur ops peut diagnostiquer un incident sans jamais accéder aux données personnelles.
  - **Fair enforcement du rate limiter** : le two-phase check évite qu'un tier soit incrémenté injustement quand un autre tier bloque déjà. Un utilisateur qui atteint son quota per-hour ne consomme pas aussi son quota per-day, ce qui préserve son budget sur les autres fenêtres.
  - **Uniformité cross-provider via `LlmCompletionResult`** : permet de comparer les coûts entre providers (Mistral, Gemini, Groq, versions Vision) et de déclencher des alertes cost avec la même structure de données, quel que soit le provider actif.
  - **Placement des couches par responsabilité** : le PromptSanitizer vit dans le worker IA (le seul qui manipule du texte OCR à envoyer au LLM). Le rate limiter et l'audit logger vivent dans l'API (le point d'entrée client HTTP qui connaît le contexte utilisateur et IP). Cohérent avec la Clean Architecture ([DEC-002](#dec-002)).

- **Alternatives écartées** :
  - **Une seule couche de sanitization stricte au niveau input** : ne protège pas contre les autres attaques (rate limiting, cost abuse, observabilité). La défense en profondeur reste supérieure.
  - **Rate limiter délégué au provider LLM tiers** : les providers gèrent leur propre rate limit mais avec des règles opaques et non alignées sur les besoins produit. Un rate limiter applicatif permet des règles métier (par-utilisateur, par-IP) que le provider ne connaît pas.
  - **Logs LLM complets (prompt et réponse) pour debug** : viole RGPD Art. 5.1.c. Le hash SHA256 offre un compromis utile et sûr.
  - **Bloquer l'IP après N échecs sanitization** : trop agressif, une seule frappe suspecte pourrait bloquer un utilisateur légitime. Le rate limiter tier per-ip-hour joue déjà ce rôle statistique.

- **Sources** :
  - OWASP Top 10 for LLM Applications (2024)
  - RGPD Article 5.1.c Data Minimization
  - Structured logging with Serilog (BACK-010)

- **Conséquences** :
  - Nouveau namespace `MemoRecipe.Application.Services.AISecurity` (7 classes et interfaces)
  - Nouveau namespace `MemoRecipeIA.Application.Security` avec le `PromptSanitizer` côté worker
  - `AiRateLimiter` enregistré en Singleton dans `Program.cs` API (MemoryCache partagé)
  - `AiAuditLogger` enregistré en Scoped (dépend de `IAiCostCounter` scoped)
  - Configuration `AiRateLimitOptions` dans `appsettings.json` section `AiRateLimiting`
  - Wire dans `RecipeController.CreateScannedRecipe` : rate limiter check avant appel worker, audit logger sur success/error
  - Wire dans `RecipePipeline` (worker) : PromptSanitizer step 2 avant appel LLM
  - Le path Vision (`VisionRecipePipeline`) n'utilise pas PromptSanitizer, pas de texte utilisateur intermédiaire à sanitizer

- **Conditions qui invalideraient ce choix** :
  - Émergence d'un standard industriel (par exemple librairie officielle Microsoft ou OWASP) qui couvrirait les 4 couches de manière plus mature. Migration à évaluer selon la couverture.
  - Refonte majeure du modèle de threat OWASP LLM qui rendrait les 10 patterns actuels obsolètes ou insuffisants. Le catalogue serait alors à réévaluer.
  - Passage à un provider LLM qui offrirait nativement toutes ces protections, rendant les couches applicatives redondantes.

- **État** : APPLIQUÉ. Livré en 3 sous-livraisons US-A2-04a/b/c fin juillet et début août 2026. Toutes les couches sont wire dans le pipeline scan, testées unitairement et en intégration.

---

### DEC-048 : Alerting coûts LLM par provider (compteur en cache + alertes Telegram déboncées)

- **Statut** : ✅ ACTIVE
- **Date** : Début août 2026 (US-A2-05)

- **Choix** :
  1. Nouveau service `AiCostCounter : IAiCostCounter` (Scoped) dans `MemoRecipe.Application/Services/Monitoring/`. Compte les tokens consommés par provider, sur deux fenêtres temporelles distinctes : quotidienne (reset à minuit UTC) et hebdomadaire (reset dimanche 23:59:59 UTC). Compteurs stockés dans `IMemoryCache` avec clés datées (`cost:daily:{provider}:{yyyy-MM-dd}` et `cost:weekly:{provider}:{yyyy-Www}`).
  2. Nouveau POCO `AiCostAlertingOptions` avec un `Dictionary<string, AiCostProviderThresholds>` (seuils Daily et Weekly tokens configurables par provider). Configuration dans `appsettings.json` section `AiCostAlerting.PerProvider` avec un bloc par provider actif (Mistral, MistralVision, Groq, Gemini, GeminiVision).
  3. Déclenchement automatique via `AiAuditLogger.LogScanSuccessAsync` qui appelle `IAiCostCounter.IncrementAsync(provider, tokensIn + tokensOut)` après chaque scan réussi. Zéro instrumentation manuelle dans le code métier, le tracking coût suit naturellement l'audit trail.
  4. Alertes Telegram déclenchées à l'atteinte de chaque seuil via `IAlertingService.NotifyAiCostDailyAsync` et `NotifyAiCostWeeklyAsync` (canal `INotificationChannel` de [DEC-039](#dec-039)). Debounce via un flag `notified` dans le cache pour éviter le spam (une seule alerte par jour et par semaine par provider une fois le seuil atteint).
  5. `TimeProvider` injecté (pas `DateTime.UtcNow` direct) pour permettre les tests time-drift-free avec `FakeTimeProvider`.

- **Pourquoi ces choix** :
  - **Per-provider plutôt que global** : les grilles tarifaires diffèrent significativement entre providers (par exemple Gemini beaucoup moins cher que Groq au token, mais throughput plus limité). Un seuil global ferait exploser les alertes sur les providers chers avant même que les providers économiques ne soient stressés. Un seuil par provider permet de calibrer selon le coût unitaire et le volume attendu.
  - **Two windows daily et weekly** : le daily capture les pics d'usage abusifs (un utilisateur ou un attaquant qui spam les scans). Le weekly capture les dérives de fond (un provider qui devient progressivement plus utilisé sans que personne ne s'en rende compte).
  - **Debounce via flag notified** : sans debounce, chaque scan suivant le franchissement du seuil enverrait une nouvelle alerte, spammant Telegram. Le flag garantit une seule notification par fenêtre.
  - **In-memory cache volatile** : compteurs perdus au restart, acceptable pour un signal best-effort. L'audit trail Serilog persiste dans les logs sur disque, il peut servir de source de vérité en cas de besoin d'audit post-incident.
  - **Wire automatique via AiAuditLogger** : couplage propre par Dependency Inversion. Le cost counter est un consommateur de l'audit trail, pas une préoccupation croisée à instrumenter partout.
  - **`TimeProvider` abstract** : tests unitaires avec `FakeTimeProvider` peuvent avancer le temps arbitrairement pour tester le franchissement des seuils journaliers et hebdomadaires sans attendre en temps réel.

- **Alternatives écartées** :
  - **Alerting global sur budget total en euros** : nécessite une conversion tokens vers euros par provider avec des tarifs qui changent régulièrement. Complexité de maintenance vs valeur ajoutée faible (les tokens sont un proxy raisonnable du coût, l'opérateur sait faire la conversion mentale).
  - **Persistance BDD des compteurs** : ajoute une écriture BDD par scan, latence non négligeable. In-memory volatile suffit pour l'usage cible (observabilité best-effort).
  - **Alerting synchrone bloquant en cas d'échec Telegram** : ferait planter le scan utilisateur si Telegram est down. Alerte fire-and-forget avec log en cas d'échec est plus robuste.
  - **Compteur par utilisateur et pas par provider** : le rate limiter ([DEC-047](#dec-047)) couvre déjà l'usage par utilisateur. Le cost counter opère à un niveau différent, la santé économique du provider, complémentaire.

- **Sources** :
  - Pattern debounce pour alerting (SRE Book, Google)
  - `TimeProvider` .NET 8+ abstract clock pour testabilité

- **Conséquences** :
  - Nouveau namespace `MemoRecipe.Application.Services.Monitoring` (3 fichiers)
  - Configuration `AiCostAlerting.PerProvider` dans `appsettings.json` (5 providers avec seuils Daily et Weekly tokens)
  - `AiCostCounter` enregistré en Scoped dans `Program.cs` API
  - 2 nouvelles méthodes sur `IAlertingService` : `NotifyAiCostDailyAsync` et `NotifyAiCostWeeklyAsync`
  - Aucun impact direct sur le pipeline scan (wire transparent via `AiAuditLogger`)
  - Coût observable même quand aucune alerte n'est levée (logs Serilog `AiScanSuccess` avec `TokensIn` et `TokensOut` disponibles pour extraction post-hoc)

- **Conditions qui invalideraient ce choix** :
  - Passage à un modèle de tarification LLM basé sur autre chose que les tokens (par exemple facturation à la requête ou au workflow). Le counter devrait être adapté à la nouvelle métrique.
  - Émergence d'un dashboard cost natif chez tous les providers (Grafana Cloud LLM Cost, Datadog LLM Observability, etc.) qui rendrait l'instrumentation applicative redondante.
  - Volume LLM tel qu'il justifierait une persistance BDD pour audit fine-grained (par exemple facturation par-utilisateur ou compliance strict).

- **État** : APPLIQUÉ. Livré dans US-A2-05 début août 2026. 5 providers configurés dans `appsettings.json` avec seuils calibrés sur les grilles tarifaires courantes. Testé unitairement avec `FakeTimeProvider` (bug time-drift `MemoryCache` corrigé mi-août dans les tests `AiCostCounterTests`).

---

### DEC-049 : Quota recettes par utilisateur avec check pre-LLM (fail-fast économique)

- **Statut** : ✅ ACTIVE
- **Date** : 17-18 août 2026 (US-A2-06 pour le quota + check pre-LLM ajouté 20/08 via US-A2-15)

- **Choix** :
  1. Nouveau POCO `RecipeLimitsOptions` (SectionName `RecipeLimits`, `MaxPerUser: 200` par défaut) dans `MemoRecipe.Application/Configuration/`. Enregistré via `builder.Services.Configure<RecipeLimitsOptions>` dans `Program.cs`, lu via `IOptions<RecipeLimitsOptions>` dans les services.
  2. Nouvelle exception custom `RecipeLimitReachedException(int Limit)` dans `MemoRecipe.Application/Exceptions/`.
  3. Nouvelle méthode helper `Task EnsureQuotaAvailableAsync(Guid userId)` sur `IRecipeService`, implémentée dans `RecipeService`. Elle appelle `_repository.CountByUserAsync(userId)` (méthode existante depuis le Dashboard) et throw `RecipeLimitReachedException` si le count courant atteint ou dépasse le `MaxPerUser` configuré. Nommage aligné avec `EnsureAccountActiveAsync` existant (convention `EnsureXxxAsync` pour assertion positive).
  4. Wire dans `RecipeService.CreateAsync` (path create manuel) : appel `EnsureQuotaAvailableAsync` en début de méthode, avant la persistance BDD.
  5. Wire dans `RecipeController.CreateScannedRecipe` (path scan IA) : appel `EnsureQuotaAvailableAsync` immédiatement après les validations input (MIME, magic bytes) et AVANT `_aiRateLimiter.CheckAndThrow` + appel worker IA. Objectif fail-fast économique : ne pas consommer un slot rate limit LLM ni des tokens LLM pour un utilisateur qui est déjà au quota (US-A2-15).
  6. `ExceptionMiddleware` catch `RecipeLimitReachedException` et renvoie HTTP 403 avec body JSON `{ status, error: "recipe_limit_reached", title, limit }`. Le champ `error` sert de discriminant machine-readable pour distinguer ce 403 des autres 403 possibles (par exemple `AccountMarkedForDeletionException`).
  7. Frontend Blazor : nouvelle exception `RecipeLimitException(int Limit)` dans `App/MemoRecipe.Web/Exceptions/`. `RecipeService.CreateRecipeAsync` et `RecipeService.ScanImageAsync` détectent le 403 avec discriminant `error == "recipe_limit_reached"` avant `EnsureSuccessStatusCode` (pattern peek + branch or throw). Message utilisateur FR contextualisé : "Vous avez atteint la limite de {limit} recettes. Supprimez-en pour en créer de nouvelles.".

- **Pourquoi ces choix** :
  - **Quota BDD plutôt que rate limit temporel** : le rate limiter LLM ([DEC-047](#dec-047)) protège contre les rafales dans le temps. Le quota BDD protège contre une accumulation lente et durable qui saturerait le stockage sans jamais déclencher le rate limit. Deux préoccupations différentes, deux mécanismes complémentaires.
  - **Fail-fast économique avec check pre-LLM (US-A2-15)** : sans ce check, un utilisateur au quota qui scanne consommait tokens LLM + slot rate limit avant de recevoir un 403 au save. Coût direct sur le budget LLM sans valeur produit. Avec le check pre-LLM, le 403 arrive en environ 130 ms au lieu de 8 à 10 s d'attente LLM. Bénéfice UX et coût.
  - **Ordre du pipeline scan** : input validation, puis quota check, puis rate limit, puis LLM. Du moins coûteux au plus coûteux. Chaque étape peut couper la suivante.
  - **Réutilisation `CountByUserAsync`** : la méthode repository existe déjà pour le Dashboard (compteur "X recettes"). Zéro nouvelle méthode repository, zéro refactor cascade.
  - **Nommage `EnsureQuotaAvailableAsync`** : convention `EnsureXxxAsync` (positive assertion, throw sinon) alignée avec `EnsureAccountActiveAsync` déjà présent dans `RecipeService`. Cohérence intra-service prime sur la variance globale du projet (par exemple `AiRateLimiter.CheckAndThrow` utilise l'autre convention).
  - **Discriminant `error: "recipe_limit_reached"`** : évite les faux positifs avec les autres 403 (par exemple `AccountMarkedForDeletionException`). Pattern d'erreur typé et machine-readable, décrit dans [DEC-012](#dec-012) actualisé.
  - **Message FR avec interpolation `{ex.Limit}`** : si `MaxPerUser` évolue (500 pour un tier premium, par exemple), le message frontend s'adapte automatiquement sans modif de code.

- **Alternatives écartées** :
  - **Quota au niveau infrastructure (BDD constraint)** : trigger Postgres qui rejette l'INSERT si le count utilisateur atteint le seuil. Plus difficile à tester, message d'erreur PG opaque à traduire côté API, pas de flexibilité pour tier premium et free différencié.
  - **Quota côté frontend uniquement** : contournable via appel API direct, contraire au principe de defense in depth.
  - **Rate limit temporel plutôt que quota BDD** : ne protège pas contre l'accumulation lente et durable.
  - **Deux tiers (Free 200 et Premium 500) implémentés dès V1** : YAGNI tant qu'il n'y a pas de système d'abonnement payant. Le POCO Options `MaxPerUser` unique convient pour Alpha.2 et Beta.1. Refactor par-tier viendra si et quand monétisation.
  - **Check quota uniquement au save (sans pre-LLM)** : c'est ce qui a été livré initialement en US-A2-06. La dette économique a été révélée immédiatement (tokens LLM gaspillés) et corrigée sous 3 jours via US-A2-15.

- **Sources** :
  - Options pattern .NET (`Configure<T>` et `IOptions<T>`)
  - Fail-fast principle (Martin Fowler)
  - Pattern d'exceptions métier typées (extension de [DEC-012](#dec-012))

- **Conséquences** :
  - Nouveau fichier `MemoRecipe.Application/Configuration/RecipeLimitsOptions.cs`
  - Nouveau fichier `MemoRecipe.Application/Exceptions/RecipeLimitReachedException.cs`
  - Nouvelle méthode `EnsureQuotaAvailableAsync` sur `IRecipeService` et `RecipeService`
  - Nouveau catch dans `ExceptionMiddleware` (RecipeLimitReachedException, 403)
  - Nouveau fichier `App/MemoRecipe.Web/Exceptions/RecipeLimitException.cs`
  - Détection du 403 discriminant dans `RecipeService.CreateRecipeAsync` (Web) et `RecipeService.ScanImageAsync` (Web) avec pattern peek + branch or throw
  - Catches spécifiques dans `CreateRecipe.razor.cs` et `ScanRecipe.razor.cs` (ordre spécifique vers générique : `AiRateLimitException`, puis `RecipeLimitException`, puis `Exception` filet de sécurité)
  - Configuration `RecipeLimits.MaxPerUser: 200` dans `appsettings.json` prod
  - Tests d'intégration `RecipeQuotaTests` avec factory dédiée `LowQuotaWebApplicationFactory` (override `MaxPerUser=2`) pour tester les deux paths (create manuel et scan)

- **Conditions qui invalideraient ce choix** :
  - Introduction d'un système d'abonnement payant avec tiers différenciés (Free 200, Premium 500, Enterprise unlimited). Le POCO Options deviendra un mapping par-tier ou un service qui lit le tier utilisateur en BDD. Refactor gérable, base structurelle en place.
  - Volume utilisateur tel que `CountByUserAsync` deviendrait un bottleneck (par exemple 1M recettes par utilisateur). Passage à un compteur maintenu en cache ou en colonne dénormalisée serait alors pertinent.
  - Changement de modèle métier (par exemple recettes partagées entre utilisateurs, quota par workspace au lieu par utilisateur). Le concept `MaxPerUser` deviendra `MaxPerWorkspace` ou équivalent.

- **État** : APPLIQUÉ. Livré en 2 temps : quota BDD dans US-A2-06 (17-18/08/2026, PR mergée main) + check pre-LLM dans US-A2-15 (20/08/2026, PR #69 mergée squash `84c4b82`). Tests d'intégration verts (2 tests quota create + 1 test quota scan), test manuel E2E validé sur les deux paths (create manuel et scan) avec `MaxPerUser=2` en dev.

---

### DEC-050 : Ingrédients structurés name + quantity + unit end-to-end (worker IA, API, frontend Blazor, BDD)

- **Statut** : ✅ ACTIVE
- **Date** : 16 août 2026 (US-A2-14)

- **Choix** :
  Représenter chaque ingrédient comme un objet structuré à 3 champs (`Name` string, `Quantity` decimal?, `Unit` string?) plutôt que comme une chaîne libre à parser (par exemple "200g de farine"). Cette structure est propagée end-to-end à travers les 4 couches du système.

  1. **Worker IA** (`memoRecipe-ia/Application/Dtos/ParsedIngredientDto.cs`) : le LLM (text-only ou Vision) retourne un JSON avec un tableau `ingredients` d'objets structurés. Le prompt (`RecipePromptBuilder`) demande explicitement le schéma structuré et donne des exemples. Aucun parsing regex côté application, le LLM fait le travail de séparation.
  2. **API** (`MemoRecipe.Application/DTOs/Ingredients/IngredientDto.cs`) : DTO exposé sur les endpoints REST avec les mêmes 3 champs plus `Id` (Guid persisté en BDD).
  3. **Frontend Blazor** (`App/MemoRecipe.Web/Models/IngredientFormModel.cs`) : modèle de formulaire découplé du DTO API (voir [DEC-018](#dec-018)). 3 champs sans `Id` car géré côté API à la création.
  4. **BDD** (`Ingredient` entity + colonnes Postgres `Name text`, `Quantity decimal?`, `Unit text?`). Schéma déjà en place depuis les premières migrations, aucune nouvelle migration n'a été nécessaire pour US-A2-14, le changement est purement DTO et propagation.

- **Pourquoi ces choix** :
  - **Structuration au plus tôt** : le LLM sait faire l'extraction de manière fiable si on le lui demande explicitement dans le schéma JSON. Attendre le frontend ou une couche métier pour parser une chaîne libre serait fragile (regex qui ne couvrent pas tous les cas, ambiguïtés d'unités, gestion des fractions et des ranges).
  - **Ouvre la voie à des features produit** : conversion de portions (multiplier ou diviser les quantités pour un nombre de convives différent), conversion d'unités (métrique vers impérial), génération de liste de courses agrégée entre plusieurs recettes (somme des quantités par ingrédient). Aucune de ces features n'est possible avec des ingrédients en chaîne libre.
  - **Cohérence de contrat cross-project** : les 3 projets (worker IA, API, frontend) partagent le même schéma logique. Zéro conversion ou parsing entre les couches, uniquement du mapping mécanique via Mapperly.
  - **Nullable Quantity et Unit** : certaines recettes ont des ingrédients sans quantité explicite ("sel et poivre à goût", "quelques feuilles de basilic"). Nullable permet de représenter cette réalité sans forcer une valeur artificielle.
  - **`decimal?` plutôt que `float?` ou `string`** : `decimal` évite les erreurs d'arrondi en base 2 sur des quantités affichées à l'utilisateur (par exemple 0.1 kg ne devient jamais 0.09999...). `string` empêcherait le calcul (conversion, agrégation).
  - **Schéma BDD déjà en place** : les colonnes `Quantity` et `Unit` existaient depuis les premières migrations (voir migration `MakeIngredientQuantityAndUnitNullable` de mars 2026). US-A2-14 exploite un schéma déjà cohérent, aucune migration nécessaire, ce qui a limité la surface du changement à la propagation DTO.

- **Alternatives écartées** :
  - **Chaîne libre `Ingredient : string`** : simple à afficher, impossible à exploiter (calcul, conversion, agrégation).
  - **Parser côté frontend une chaîne libre retournée par le LLM** : fragile (regex jamais complètement correctes), duplique la logique si mobile MAUI arrive plus tard.
  - **Structure encore plus fine (par exemple `Quantity` + `Unit` + `Preparation` "haché, émincé")** : YAGNI pour V1, ajoute une complexité pour un besoin non exprimé. Extensible plus tard (nouveau champ nullable dans le DTO, migration additive).
  - **Ingrédients normalisés avec référentiel** (par exemple table `IngredientReference` avec IDs stables comme "farine_ble" et labels multilingues) : hors scope V1, énorme chantier de normalisation qui n'apporte pas de valeur immédiate.

- **Sources** :
  - Structured Output pattern LLM (par exemple JSON schema constrained decoding)
  - Data Transfer Object pattern (Fowler)
  - Cohérence avec [DEC-018](#dec-018) `RecipeFormModel` séparé des DTOs API

- **Conséquences** :
  - Nouveau DTO worker IA `ParsedIngredientDto`
  - `IngredientDto` API existant enrichi (les champs `Quantity` et `Unit` étaient déjà là mais pas propagés depuis le LLM)
  - Nouveau `IngredientFormModel` frontend Blazor (extraction depuis `RecipeFormModel`)
  - Refactor cascade dans les mappers (worker vers API, API, Web)
  - Prompt LLM (`RecipePromptBuilder`) mis à jour pour demander explicitement le schéma structuré avec exemples
  - Bug latent DTO nullable réparé pendant l'US (le pipeline retournait des `Ingredient : string` aplati qui masquait la structure BDD sous-jacente)
  - Test manuel Mistral Vision validé sur recette réelle (extraction structurée conforme au schéma demandé)
  - Aucune migration BDD nécessaire, schéma déjà cohérent depuis mars 2026

- **Conditions qui invalideraient ce choix** :
  - Émergence d'un besoin de granularité supérieure (par exemple extraction séparée du mode de préparation "haché, émincé, râpé"). Ajout d'un champ nullable dans le DTO, refactor cascade mais gérable.
  - Passage à un référentiel normalisé d'ingrédients (voir alternative écartée). Refactor structurel majeur, à repenser globalement.
  - Retour à un modèle chaîne libre imposé par une contrainte externe (par exemple format d'import ou export tiers). Compromis à réévaluer avec sérialisation dédiée pour l'interop.

- **État** : APPLIQUÉ. Livré dans US-A2-14 (16/08/2026, PR #65 mergée main). 3 commits atomiques cascade IA vers API vers Web. Bug latent DTO nullable réparé au passage. Test manuel Mistral Vision validé sur recette réelle. Aucune régression détectée en tests d'intégration.

---

### DEC-051 : Serilog structured logging + masquage PII (Serilog, sinks Console + File, `EmailMasker`, `ValidationErrorSanitizer`)

- **Statut** : ✅ ACTIVE
- **Date** : 18 juillet 2026 (BACK-010)

- **Choix** :
  1. Adopter Serilog comme framework de logging structuré pour l'API, préféré au logger natif `ILogger<T>` seul pour la richesse de son écosystème (sinks, enrichers, filters).
  2. Configuration dans `appsettings.json` section `Serilog` avec 2 sinks actifs, `Console` (dev et prod pour agréger avec Docker logs) et `File` (rotation quotidienne, rétention 30 jours, template daté avec timestamp UTC, level, SourceContext, message et properties).
  3. Enrichers actifs : `FromLogContext` (pour les scopes structurés), `WithMachineName` (traçabilité multi-instances future), `WithEnvironmentName` (distinguer dev, prod).
  4. Masquage PII systématique via 2 utilitaires dédiés, `EmailMasker` (masque la partie locale de l'email dans les logs, par exemple `s***@example.com`) et `ValidationErrorSanitizer` (retire les valeurs saisies par l'utilisateur des messages d'erreur FluentValidation avant de logger).
  5. Wire dans `Program.cs` API via `builder.Services.AddSerilog((services, lc) => lc.ReadFrom.Configuration(builder.Configuration).ReadFrom.Services(services).Enrich.FromLogContext())` et `app.UseSerilogRequestLogging()` pour tracer chaque requête HTTP.

- **Pourquoi ces choix** :
  - **Logs structurés** : chaque log est un objet avec des properties nommées (`UserId`, `RecipeId`, `EventType`, etc.) plutôt qu'un string concaténé. Permet des filtres et agrégations puissants avec un outil de log aggregation futur (Grafana Loki, Elasticsearch, etc.), et rend les logs exploitables pour l'audit trail RGPD.
  - **File sink avec rotation** : garde une trace persistante des 30 derniers jours sur le disque du VPS pour investigation post-mortem, même en cas de panne du service d'agrégation externe.
  - **Enrich `FromLogContext`** : permet de propager un contexte (par exemple `LogContext.PushProperty("UserId", userId)`) qui s'attache automatiquement à tous les logs émis pendant l'opération.
  - **Masquage PII systématique** : le RGPD Art. 5.1.c minimization impose de ne pas logger de données personnelles inutilement. Les emails sont masqués partiellement (assez pour corréler, pas assez pour exposer), les valeurs saisies dans les erreurs de validation sont retirées (un password mal formaté ne doit jamais apparaître dans les logs).
  - **Pattern uniforme cross-couche** : le même logger structuré est utilisé dans l'API (`ExceptionMiddleware`, `AiAuditLogger`, `AccountPurgeService`) et pourra l'être dans le worker IA quand nécessaire.

- **Alternatives écartées** :
  - **`ILogger<T>` natif seul** : suffit pour du logging basique mais manque de flexibilité pour les sinks (pas de File avec rotation natif), les enrichers, et le format structuré. Serilog s'intègre proprement au-dessus.
  - **NLog ou log4net** : autres frameworks matures mais moins pratiques pour le logging structuré nativement. Serilog est aujourd'hui la référence dans l'écosystème .NET pour ce besoin.
  - **Log agrégation externe seule (Datadog, Grafana Cloud)** : coût récurrent significatif, dépendance externe, moins de contrôle sur le format. Le File sink local reste la baseline, l'agrégation externe pourra être ajoutée en V2 sans casser la baseline.

- **Sources** :
  - [Serilog official docs](https://serilog.net/)
  - RGPD Art. 5.1.c Data Minimization
  - Structured logging best practices (Nicholas Blumhardt, Serilog creator)

- **Conséquences** :
  - Configuration `Serilog` complète dans `appsettings.json` (Using, MinimumLevel avec Override par namespace, WriteTo, Enrich)
  - 2 utilitaires custom dans `MemoRecipe.Application/Security/` (`EmailMasker`, `ValidationErrorSanitizer`)
  - Wire dans `Program.cs` API L35-38 (`AddSerilog`) et L257 (`UseSerilogRequestLogging`)
  - `AiAuditLogger` (voir [DEC-047](#dec-047)) et `AccountPurgeService` utilisent le logger structuré Serilog par défaut
  - Logs quotidiens dans `logs/memorecipe-{yyyy-MM-dd}.log` avec rotation automatique

- **Conditions qui invalideraient ce choix** :
  - Passage à une plateforme cloud (par exemple Azure App Service) qui offre nativement un service de logging structuré équivalent, rendant Serilog + File sink redondants
  - Émergence d'un standard `Microsoft.Extensions.Logging` avec support natif structured + sinks configurables qui rendrait Serilog superflu
  - Volume de logs tel qu'un service d'agrégation externe deviendrait obligatoire (limite disque VPS atteinte)

- **État** : APPLIQUÉ. Livré dans BACK-010 (mergé 18/07/2026). Wire complet dans l'API, `AiAuditLogger` et `ExceptionMiddleware` utilisent le logger structuré. Prêt pour l'ajout d'un agrégateur externe en V2 sans changement d'API.

---

### DEC-052 : Docker Secrets natif (`secrets:` top-level + `AddKeyPerFile`) pour les variables sensibles en production

- **Statut** : ✅ ACTIVE. Cette décision **supersede** le trade-off "pas de Docker secrets natifs" listé dans les alternatives deferred de [DEC-029](#dec-029).
- **Date** : 25 juillet 2026 (BACK-004, PR #38)

- **Choix** :
  1. Adopter le mécanisme `secrets:` top-level natif de Docker Compose dans `docker-compose.prod.yml` pour tous les secrets sensibles (POSTGRES_PASSWORD, JWT Secret, ConnectionStrings, OcrScan BaseUrl, Telegram BotToken et ChatId).
  2. Bloc `secrets:` top-level qui pointe vers `${SECRETS_PATH}/nom_du_secret`. `SECRETS_PATH` par défaut `/run/secrets` en prod, override possible en test local (`SECRETS_PATH=./secrets-local`).
  3. Consommation dans chaque service via 3 conventions selon le runtime :
     - **Postgres** : variable `POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password` (support natif de l'image officielle Postgres)
     - **API ASP.NET Core** : `builder.Configuration.AddKeyPerFile("/run/secrets", optional: true)` dans `Program.cs`, chaque fichier devient une clé de configuration hiérarchique via le double underscore (`JwtSettings__Secret` devient `JwtSettings:Secret`)
     - **Script backup shell** : lecture directe `if -f /run/secrets/postgres_password` puis `export PGPASSWORD=$(cat ...)`
  4. Documentation opérationnelle dans `DEPLOYMENT.md` avec runbook de génération et rotation des secrets sur le VPS.

- **Pourquoi ces choix** :
  - **Supersede du trade-off DEC-029** : à l'époque de DEC-029 (mai 2026), les env vars ont été jugées suffisantes pour un compose simple. Trois mois d'expérience et la préparation au déploiement prod ont fait évoluer ce choix vers le mécanisme secrets natif, plus sûr et plus propre.
  - **Secrets en fichier plutôt qu'en variable d'environnement** : les env vars sont visibles via `docker inspect`, dans les logs de démarrage, et parfois dans les crash dumps. Les secrets en fichier restent dans le tmpfs `/run/secrets/`, jamais persistés sur disque, jamais listés par `docker inspect env`.
  - **Trois conventions de consommation propres à chaque runtime** : évite les hacks (par exemple sourcer un `.env` avant chaque commande) et utilise les mécanismes natifs de chaque outil.
  - **`SECRETS_PATH` overridable** : permet de tester localement sans monter en tmpfs, tout en gardant le même code de production.
  - **`AddKeyPerFile` ASP.NET Core natif** : zéro dépendance externe, intègre proprement les fichiers secrets dans le système de configuration standard `IConfiguration`. La convention double underscore mappe naturellement vers la structure hiérarchique attendue par le code métier.

- **Alternatives écartées** :
  - **Vault (HashiCorp Vault, Bitnami Sealed Secrets)** : overkill pour un stack single-VPS. Utile en Kubernetes ou multi-cluster, pas pour un compose Docker sur un serveur unique.
  - **Fichier `.env` non-chiffré monté en volume** : accessible en cas d'accès disque au VPS, ne bénéficie pas du tmpfs isolation.
  - **Env vars dans le compose** (statu quo pré-BACK-004) : visible via `docker inspect`, moins sûr, cité comme dette technique tracée.

- **Sources** :
  - [Docker Compose secrets](https://docs.docker.com/compose/compose-file/09-secrets/)
  - [ASP.NET Core `AddKeyPerFile`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.keyperfileconfigurationextensions.addkeyperfile)
  - [Postgres image FILE convention](https://hub.docker.com/_/postgres)

- **Conséquences** :
  - `docker-compose.prod.yml` L119-131 : bloc `secrets:` top-level avec 6 secrets déclarés
  - `Program.cs` API L31-33 : `builder.Configuration.AddKeyPerFile(Environment.GetEnvironmentVariable("SECRETS_PATH") ?? "/run/secrets", optional: true)`
  - `infra/backup/backup.sh` : lecture `POSTGRES_PASSWORD` depuis fichier
  - `DEPLOYMENT.md` : section runbook secrets (génération, rotation, backup)
  - `.env.example` mis à jour pour ne plus lister les secrets (seulement les non-sensitives comme POSTGRES_USER, JWT_ISSUER, JWT_AUDIENCE, GPG_RECIPIENT)
  - Note DEC-029 mise à jour pour refléter le supersede partiel

- **Conditions qui invalideraient ce choix** :
  - Migration vers Kubernetes ou Docker Swarm : le mécanisme `Secret` de l'orchestrateur remplace le `secrets:` compose. Migration mécanique.
  - Adoption d'un vault centralisé (par exemple Hashicorp Vault) pour secrets rotables : le compose `secrets:` restera valable mais alimenté par le vault plutôt que par des fichiers statiques.

- **État** : APPLIQUÉ. Livré dans BACK-004 (25/07/2026, PR #38) puis raffiné dans BACK-007 partie 3 (30/07/2026, PR #42). Test E2E local validé (4 containers healthy, /health Healthy, backup .dump.gpg 4.2KB chiffré).

---

### DEC-053 : Pipeline CI/CD GitHub Actions (7 jobs parallèles + CodeQL SAST + Lighthouse a11y + release conditionnel sur tag)

- **Statut** : ✅ ACTIVE
- **Date** : 01-03 août 2026 (BACK-008, 4 phases mergées en PR successives)

- **Choix** :
  1. Adopter GitHub Actions comme plateforme CI/CD, hébergé sur le même GitHub que le code, avec authentification native via `GITHUB_TOKEN` (pas de clé privée à gérer).
  2. Workflow principal `.github/workflows/ci.yml` avec 7 jobs qui tournent en parallèle sur `ubuntu-latest`, chacun découpé selon la brique concernée pour permettre les feedbacks rapides et le fail-fast :
     - `test-api` : `dotnet test` sur la solution API (177 tests d'intégration + unitaires)
     - `test-ia` : `dotnet test` sur le projet IA `.NET 8` avec `--filter "Category!=Integration"` (skip Tesseract native deps qui ne s'installent pas facilement sur les runners GH)
     - `build-web` : `dotnet build` sur le Frontend Blazor `.NET 10`
     - `vuln-audit` : `dotnet list ... --vulnerable --include-transitive` sur les 4 solutions, `grep -qE '(High|Critical)'` puis `exit 1` avec annotation `::error::` pour fail-fast sur toute CVE High ou Critical
     - `lighthouse-a11y` : audit Lighthouse local Docker (build image Web, run container, `@lhci/cli`, assertions via `.lighthouserc.json` avec seuils Perf, A11y, Best Practices, SEO)
  3. Workflow séparé `.github/workflows/codeql.yml` pour l'analyse statique SAST via `github/codeql-action@v4`, matrix `languages: [csharp, actions]`, `build-mode: none`, avec cron hebdomadaire (jeudi) plus déclenchement sur push et PR main.
  4. Job conditionnel `build-and-push` déclenché uniquement sur push d'un tag `v*` (release), qui build et push les images Docker sur GHCR (API via `dotnet publish /t:PublishContainer`, Web via `docker build` + `docker push`).
  5. `permissions: contents: read` least-privilege déclaré au niveau workflow, chaque job élève les permissions au minimum nécessaire (par exemple `packages: write` pour le job release).

- **Pourquoi ces choix** :
  - **GitHub Actions plutôt qu'alternative** : intégré au repo, gratuit pour les repos publics, authentification native via `GITHUB_TOKEN`, écosystème d'actions marketplace mature. Alternatives (GitLab CI, CircleCI, Jenkins self-hosted) demandent soit de migrer l'hébergement code, soit de gérer des secrets d'authentification cross-service.
  - **Fail-fast sur High/Critical CVE** : la sécurité de la supply chain est critique (OWASP A03:2025 Software and Data Integrity Failures). Bloquer le merge sur toute vulnérabilité importante évite de propager de la dette sécu en prod. Les Low et Medium restent visibles dans les logs mais ne bloquent pas.
  - **CodeQL en workflow séparé** : le SAST est asynchrone (les résultats arrivent quelques minutes après le push), pas besoin de bloquer le merge dessus. Le cron hebdomadaire garantit un scan régulier même sans commit récent.
  - **Lighthouse a11y en local Docker** : évite la friction de déploiement d'un environnement de test réel juste pour l'audit. Le container Docker suffit à valider les seuils accessibility, performance et best practices.
  - **Release conditionnelle sur tag `v*`** : le pattern SemVer `v1.0.0`, `v1.0.0-alpha.2` déclenche automatiquement le push d'image tagué avec la version. Zéro action manuelle après création du tag.
  - **Least-privilege `permissions:` explicites** : réduit le blast radius en cas de compromission d'un workflow (par exemple via une action marketplace malicieuse).

- **Alternatives écartées** :
  - **GitLab CI en self-hosted** : nécessite de migrer le code sur GitLab OU d'utiliser un runner externe. Complexité et coût sans bénéfice pour un projet solo.
  - **CircleCI free tier** : limité en minutes gratuites, moins bien intégré à GitHub.
  - **Jenkins self-hosted** : maintenance lourde, sécurité à gérer, disproportionné pour un projet portfolio.
  - **CI/CD "artisanal" via scripts déclenchés manuellement** : anti-pattern, pas de reproductibilité ni de feedback rapide sur les PR.

- **Sources** :
  - [GitHub Actions docs](https://docs.github.com/en/actions)
  - [github/codeql-action](https://github.com/github/codeql-action)
  - [Lighthouse CI](https://github.com/GoogleChrome/lighthouse-ci)
  - OWASP A03:2025 Software and Data Integrity Failures
  - Least-privilege principle (SRE Book)

- **Conséquences** :
  - Fichier `.github/workflows/ci.yml` avec 7 jobs (~200 lignes)
  - Fichier `.github/workflows/codeql.yml` (~30 lignes)
  - Fichier `.lighthouserc.json` à la racine du projet Web avec seuils calibrés
  - Fichier `documentation/RELEASING.md` avec runbook de release (versionning SemVer, procédure tag + push, rollback)
  - Badges CI et CodeQL affichés dans le README
  - Retrait du package `Microsoft.EntityFrameworkCore.Sqlite v10.0.5` (dead code) suite au fix cascade CVE-2025-6965 SQLite HIGH

- **Conditions qui invalideraient ce choix** :
  - Migration hors GitHub comme plateforme principale du projet (peu probable)
  - Émergence d'un besoin de tests E2E avec vraie infra (Playwright, Selenium contre un env de staging réel) qui nécessiterait d'ajouter un cluster de runners self-hosted
  - Volume de tests tel que les runners GitHub-hosted gratuits deviendraient un bottleneck

- **État** : APPLIQUÉ. Livré en 4 phases (PR #44 le 01/08 workflow principal, PR #45 vuln-audit 01/08, PR #46 CodeQL 02/08, PR #49 Lighthouse 02/08, feature/BACK-008-releasing-doc 03/08 phase release). Baseline CodeQL day 1 = 14 findings tous triés (4 fix workflow permissions, 10 false positives dismissed avec justification). Cascade fix CVE-2025-6965 SQLite HIGH résolue en retirant le package inutilisé.

---

### DEC-054 : `Recipe.IsPublic = false` par défaut (privacy-by-design RGPD Art. 25)

- **Statut** : ✅ ACTIVE
- **Date** : 22 juin 2026 (BACK-076, migration `20260622065006_SetRecipeIsPublicDefaultFalse`)

- **Choix** :
  Changer la valeur par défaut de la colonne `Recipe.IsPublic` de `true` (implicite BDD initial) à `false` via une migration EF Core dédiée. Toute nouvelle recette créée sans spécifier explicitement `IsPublic = true` est par défaut privée, visible uniquement par son propriétaire.

- **Pourquoi ces choix** :
  - **Privacy by Design (RGPD Art. 25)** : le principe légal impose de configurer les paramètres par défaut de manière à protéger au maximum la vie privée. Un utilisateur qui crée une recette sans se poser la question doit obtenir un état privé par défaut, pas public.
  - **Alignement avec l'attente utilisateur commune** : la plupart des utilisateurs qui scannent leurs recettes personnelles ne s'attendent PAS à ce qu'elles soient visibles par d'autres utilisateurs de la plateforme. Le défaut public rompt cette attente et créait un risque de partage involontaire.
  - **Impact minimal sur le code applicatif** : la migration change juste la valeur par défaut Postgres, le code applicatif qui set explicitement `IsPublic = true` (par exemple future feature de partage) continue de fonctionner.
  - **Cohérence avec la note produit DEC-043** : les recettes créées via scan sont particulièrement sensibles (risque droit d'auteur sur des recettes reproduites depuis livres, magazines, blogs). Le défaut privé les protège à la source.

- **Alternatives écartées** :
  - **Garder IsPublic = true par défaut** : contraire au principe Privacy by Design, expose les utilisateurs à un risque de partage involontaire.
  - **Ajouter une popup "voulez-vous rendre publique ?"** au moment de la création : friction UX, contredit la fluidité du parcours scan et save. Le défaut privé résout le problème en amont.
  - **Retirer complètement le champ IsPublic** : ferme la porte à toute feature de partage future. Le champ nullable/false-par-défaut préserve l'option.

- **Sources** :
  - RGPD Article 25 Data Protection by Design and by Default
  - OWASP A04:2021 Insecure Design
  - Principle of least privilege appliqué à la visibilité des données

- **Conséquences** :
  - Nouvelle migration `20260622065006_SetRecipeIsPublicDefaultFalse` (colonne `IsPublic bool DEFAULT false` sur la table `Recipe`)
  - Aucun changement code applicatif nécessaire (le default est appliqué au niveau BDD)
  - Documentation `Privacy.razor` mise à jour pour refléter le nouveau comportement par défaut
  - Tests d'intégration existants continuent de passer (aucun n'assumait `IsPublic = true` par défaut sans le spécifier)

- **Conditions qui invalideraient ce choix** :
  - Repositionnement produit vers "réseau social de recettes publiques" (peu probable) qui rendrait le défaut privé contreproductif
  - Ajout d'une feature "quick share" qui exigerait un défaut public dans un contexte spécifique (impact isolé, pas de retour au défaut public global)

- **État** : APPLIQUÉ. Migration mergée le 22/06/2026 dans BACK-076. Vérifié en tests d'intégration, aucune régression.

---

### DEC-055 : ForwardedHeaders middleware en tête de pipeline pour supporter le reverse proxy edge

- **Statut** : ✅ ACTIVE
- **Date** : 26 juillet 2026 (BACK-061, PR #39)

- **Choix** :
  1. Ajouter `ForwardedHeaders` middleware en tête du pipeline `Program.cs` API, configuré avec `XForwardedFor | XForwardedProto` pour propager l'IP réelle du client et le schéma (http/https) depuis le reverse proxy edge vers l'API.
  2. Appeler `KnownIPNetworks.Clear()` et `KnownProxies.Clear()` pour désactiver la whitelist par défaut d'ASP.NET Core (qui accepte seulement les IPs de loopback en dev). Safe car l'API est isolée dans un réseau Docker interne, aucun accès direct depuis l'extérieur.
  3. Skipper `app.UseHttpsRedirection()` en production, car le TLS termination se fait au reverse proxy edge (Apache ou nginx sur le host), pas au niveau du container API.

- **Pourquoi ces choix** :
  - **Prérequis pour le déploiement prod via reverse proxy edge** : sans `ForwardedHeaders`, l'API voit toutes les requêtes venir de l'IP du reverse proxy (par exemple `172.17.0.1`) au lieu de l'IP réelle du client. Le rate limiter par IP ([DEC-022](#dec-022), [DEC-047](#dec-047)) devient inutile, tout le trafic est comptabilisé sur une seule IP.
  - **`KnownIPNetworks.Clear()` en tête de pipeline** : le middleware doit être enregistré AVANT tout autre middleware qui lit `HttpContext.Connection.RemoteIpAddress` (rate limiter, CORS, logging). Sans le clear, ASP.NET Core rejette les headers en environnement Docker car il ne reconnaît pas le proxy Docker comme "trusted".
  - **Skip HttpsRedirection en prod** : le reverse proxy edge fait déjà la redirection HTTP vers HTTPS. Refaire cette redirection depuis l'API génère un warning et casse le flow des requêtes derrière un proxy qui termine le TLS.
  - **Safe car réseau isolé** : l'API n'est jamais exposée directement sur Internet en prod ([DEC-028](#dec-028), Option B reverse proxy nginx). Le clear des KnownNetworks est acceptable dans ce contexte d'isolation réseau stricte.

- **Alternatives écartées** :
  - **Ne pas configurer ForwardedHeaders** : l'API loggue des IPs Docker internes au lieu des IPs client, casse le rate limiter par IP, brise l'audit trail.
  - **Whitelister explicitement le proxy Docker dans `KnownProxies`** : nécessite de maintenir une liste d'IPs qui change entre environnements. Le clear est plus simple dans notre contexte réseau isolé.
  - **Terminer le TLS au niveau du container API** : demande un certificat installé dans l'image, complexifie le renouvellement Let's Encrypt, mélange les responsabilités. Le reverse proxy edge est le point d'entrée logique pour le TLS.

- **Sources** :
  - [ASP.NET Core Configure ASP.NET Core to work with proxy servers and load balancers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer)
  - Cohérence avec [DEC-028](#dec-028) reverse proxy nginx Option B

- **Conséquences** :
  - `Program.cs` API L224-232 : configuration `ForwardedHeadersOptions` puis `app.UseForwardedHeaders(...)` en tête de pipeline
  - `Program.cs` API L258-261 : `if (!app.Environment.IsProduction()) { app.UseHttpsRedirection(); }` pour skipper en prod
  - Warning "HTTPS redirection" supprimé au démarrage en prod
  - Rate limiter par IP retrouve son sens (chaque client a sa propre IP visible dans les logs)
  - Prérequis technique couvrant environ 80% du code nécessaire pour BACK-009 (HTTPS forcé prod), le reste étant la config Apache et Let's Encrypt sur le VPS

- **Conditions qui invalideraient ce choix** :
  - Passage à un déploiement sans reverse proxy edge (par exemple exposition directe de l'API sur Internet, ce qui contredit DEC-028)
  - Adoption d'un service PaaS qui gère le TLS termination différemment (par exemple ingress Kubernetes avec annotations)

- **État** : APPLIQUÉ. Livré dans BACK-061 (26/07/2026, PR #39).

---

### DEC-056 : Kestrel hardening (`MaxRequestBodySize` 15 Mo + `AddServerHeader = false`)

- **Statut** : ✅ ACTIVE
- **Date** : Juillet 2026 (BACK-041)

- **Choix** :
  Configurer le web server Kestrel dans `Program.cs` API avec 2 hardenings appliqués via `builder.WebHost.ConfigureKestrel(...)` :
  1. `options.Limits.MaxRequestBodySize = 15 * 1024 * 1024` (15 Mo) pour plafonner la taille des uploads au niveau transport, avant que le contenu n'atteigne le controller.
  2. `options.AddServerHeader = false` pour retirer le header `Server: Kestrel` des réponses HTTP (OWASP anti-fingerprinting).

- **Pourquoi ces choix** :
  - **Défense en profondeur upload** : couche 1 (Kestrel `MaxRequestBodySize`) plafonne au niveau transport, couche 2 (validation multipart dans `RecipeController`) plafonne au niveau applicatif à 10 Mo, couche 3 (whitelist extension + MIME + magic bytes) filtre le contenu. Si un attaquant essaie d'uploader un fichier de 100 Mo, Kestrel refuse immédiatement avec un 413 sans consommer de mémoire applicative.
  - **`AddServerHeader = false` (OWASP anti-fingerprinting)** : les headers `Server: Kestrel/X.Y.Z` révèlent le stack technique et sa version, aidant un attaquant à cibler des CVE spécifiques. Le retrait complet est plus sûr qu'une valeur générique.
  - **15 Mo plafond raisonnable** : couvre confortablement les scans de recettes (JPEG/PNG haute résolution ~5 Mo max en pratique), tout en bloquant les tentatives d'upload de fichiers massifs.

- **Alternatives écartées** :
  - **Plafond upload uniquement au controller** : ne protège pas contre une attaque qui envoie un body énorme sans jamais consommer le stream côté serveur.
  - **Header `Server:` avec valeur générique** ("MemoRecipe") : donne quand même une info sur le stack (custom .NET). Le retrait total est plus sûr.
  - **Utiliser un package tiers pour le hardening** : les 2 options natives Kestrel suffisent, package externe = complexité et surface d'attaque supplémentaire.

- **Sources** :
  - [Kestrel web server options](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options)
  - [OWASP HTTP Headers Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html) anti-fingerprinting

- **Conséquences** :
  - `Program.cs` API L42-46 : `builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 15 * 1024 * 1024; options.AddServerHeader = false; })`
  - Réponses HTTP sans header `Server`
  - Uploads > 15 Mo refusés avec 413 Payload Too Large avant appel controller
  - Cohérence avec nginx `server_tokens off` côté Frontend (voir note dans DEC-027)

- **Conditions qui invalideraient ce choix** :
  - Introduction d'une feature qui légitimement upload des fichiers > 15 Mo (par exemple vidéos, PDF haute résolution). Ajuster le plafond en fonction du besoin.

- **État** : APPLIQUÉ. Livré dans BACK-041 (juillet 2026).

---

### DEC-057 : Upload defense-in-depth (extension + MIME + magic bytes) au niveau contrôleur

- **Statut** : ✅ ACTIVE
- **Date** : Mai-juin 2026 (première itération BACK-051), renforcée juillet 2026

- **Choix** :
  Dans `RecipeController.CreateScannedRecipe`, valider l'upload en 3 couches successives (defense-in-depth OWASP File Upload Cheat Sheet) avant de propager le stream au worker IA :
  1. **Whitelist extension** : `allowedExtensions = { ".jpeg", ".jpg", ".png" }`, comparaison via `Path.GetExtension().ToLowerInvariant()`. Refuse tout autre extension avec 400.
  2. **Whitelist MIME type** : `allowedMimeTypes = { "image/jpeg", "image/png" }`, comparaison sur `imageFile.ContentType`. Refuse tout autre MIME avec 400.
  3. **Magic bytes** : lecture des 8 premiers octets du stream et comparaison avec les signatures JPEG (`0xFF 0xD8 0xFF`) et PNG (`0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A`). Refuse tout fichier dont les magic bytes ne matchent pas avec 400.

- **Pourquoi ces choix** :
  - **Defense-in-depth (OWASP File Upload)** : chaque couche protège contre un vecteur d'attaque différent. L'extension seule est trivialement contournable (renommer `.exe` en `.jpg`). Le MIME seul est trivialement contournable (le client contrôle le header `Content-Type`). Les magic bytes sont la seule couche vraiment fiable, mais lente à évaluer, d'où la vérification en couches croissantes de coût.
  - **Fail-fast économique** : les vérifications simples (extension puis MIME) filtrent d'abord et évitent la lecture du stream pour la majorité des cas invalides. Les magic bytes ne s'exécutent que sur les fichiers qui ont passé les 2 premiers filtres.
  - **JPEG et PNG uniquement** : formats standard pour les photos de recettes, largement supportés par Tesseract OCR ([DEC-025](#dec-025)). WebP retiré pour cause libwebp manquant sur Windows, blocage acté au niveau contrôleur y compris pour le path Vision (ticket post V1 pour l'assouplissement, voir DEC-025).
  - **Complémentarité avec Kestrel `MaxRequestBodySize`** ([DEC-056](#dec-056)) : la couche Kestrel plafonne la taille, les couches contrôleur filtrent le contenu.

- **Alternatives écartées** :
  - **Vérification extension seulement** : trivialement contournable, insuffisante.
  - **Vérification MIME seulement** : le client contrôle le header, insuffisante.
  - **Vérification magic bytes seulement** : coûteuse (lecture stream) exécutée sur toutes les requêtes valides ou pas.
  - **Package tiers de validation d'image (ImageSharp analyse)** : ouvre le fichier avec un décodeur d'image, plus coûteux, expose potentiellement à des exploits sur les décodeurs. Le triptyque extension + MIME + magic bytes offre le meilleur ratio sécurité/coût.

- **Sources** :
  - [OWASP File Upload Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html)
  - JPEG et PNG magic bytes standards

- **Conséquences** :
  - `RecipeController.CreateScannedRecipe` L139-165 : whitelist extension + MIME + magic bytes appliqués en séquence
  - Helper `IsValidImageMagicBytes(byte[])` L218-234 : comparaison des signatures
  - Tests d'intégration `UploadValidationTests` couvrent chaque couche indépendamment
  - Blocage WebP acté (voir [DEC-025](#dec-025))

- **Conditions qui invalideraient ce choix** :
  - Assouplissement du support d'images (WebP end-to-end, HEIC iPhone, PDF) via ticket BACK dédié. Les magic bytes concernés à ajouter.

- **État** : APPLIQUÉ. Livré initialement dans BACK-051 (mai 2026), stabilisé courant juillet 2026.

---

### DEC-058 : Suppression de compte utilisateur — soft-delete via colonne nullable timestamp + cascade delete BDD

- **Statut** : ✅ ACTIVE
- **Date** : 23 juin 2026 (BACK-005, migration `20260623074830_AddDeleteRequestedAtToUser`)

- **Choix** :
  Deux décisions liées formant le socle du RGPD Art. 17 (droit à l'oubli) :
  1. **Soft-delete via colonne nullable timestamp** : ajout de `User.DeleteRequestedAt DateTime?` (nullable), plutôt qu'un flag booléen `IsDeleted` ou une table archive séparée. Une demande de suppression met `DeleteRequestedAt = DateTime.UtcNow`. L'annulation remet la valeur à `null`. La purge définitive (après J+30) supprime physiquement la ligne.
  2. **Cascade delete BDD via `DeleteBehavior.Cascade`** : configuré dans `MemoRecipeDbContext.OnModelCreating` L47. Quand un `User` est supprimé physiquement, toutes ses `Recipe`, `Ingredient`, `Step`, `RecipeCategory`, `Favorite`, `Comment` sont supprimées en cascade automatiquement par Postgres, sans besoin de code applicatif pour parcourir les entités liées.

- **Pourquoi ces choix** :
  - **Colonne nullable timestamp plutôt que flag booléen** : le timestamp encode DEUX informations, le fait que la suppression est demandée ET le moment. Un flag booléen aurait nécessité une seconde colonne `DeletionRequestedAt` pour connaître la date, doublant la surface de schéma. Nullable au lieu de valeur par défaut permet de distinguer clairement les 3 états, actif, suppression demandée, purgé (ligne supprimée).
  - **Cascade delete BDD plutôt que code applicatif** : garantit l'atomicité, aucune donnée orpheline possible même en cas de crash du service pendant la purge. Postgres garantit la cohérence transactionnelle. Le code applicatif se contente d'appeler `_context.Users.Remove(user)` et laisse la cascade opérer.
  - **Compatible RGPD Art. 17 strict** : après purge, aucune donnée résiduelle identifiante ne persiste. Les recettes, ingrédients, favoris, commentaires de l'utilisateur sont physiquement supprimés en cascade, pas archivés.
  - **Le soft-delete offre une réversibilité pendant les 30 jours** : l'utilisateur qui change d'avis se reconnecte et le login-check propose l'annulation (`DeleteRequestedAt = null`), aucune donnée n'a été détruite entre-temps.

- **Alternatives écartées** :
  - **Flag booléen `IsDeleted`** : moins expressif, nécessite une seconde colonne pour le timestamp. Pattern datant d'une époque où les colonnes nullable étaient chères.
  - **Table archive séparée** : deux fois plus de tables, code applicatif complexe pour parcourir les archives lors du login-check. Coût de maintenance disproportionné.
  - **Cascade delete côté code applicatif (Remove sur chaque table)** : risqué (oubli d'une entité, race condition), moins performant (multiples requêtes SQL au lieu d'une seule cascade Postgres), casse l'atomicité en cas de crash.
  - **Soft-delete permanent (jamais de purge physique)** : viole RGPD Art. 17 qui exige une suppression effective.

- **Sources** :
  - RGPD Article 17 Droit à l'effacement
  - [Entity Framework Core cascade delete](https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete)
  - Postgres `ON DELETE CASCADE` documentation

- **Conséquences** :
  - Migration `20260623074830_AddDeleteRequestedAtToUser` (colonne `DeleteRequestedAt timestamp with time zone NULL`)
  - `MemoRecipeDbContext.OnModelCreating` L47 : `.OnDelete(DeleteBehavior.Cascade)` sur les relations Recipe → User et toutes les entités enfant
  - `AuthService` L107-120 : login-check qui déclenche la purge à J+30 ou annule la demande si l'utilisateur revient
  - `AccountPurgeService` (BackgroundService) qui balaie les comptes marqués et purge ceux dépassant J+30 (voir [DEC-037](#dec-037))
  - Cohérence RGPD 100% (login-check pour les revenants + cron pour les fantômes)

- **Conditions qui invalideraient ce choix** :
  - Contrainte réglementaire imposant une archive légale des données supprimées (par exemple audit fiscal 7 ans) qui rendrait la cascade delete impossible. Le soft-delete resterait valide, la purge deviendrait une anonymisation.
  - Refactor vers un modèle multi-tenant où les données de plusieurs utilisateurs partagent des lignes (par exemple recettes collaboratives) qui rendrait la cascade dangereuse.

- **État** : APPLIQUÉ. Migration mergée dans BACK-005 (29/06/2026, PR #25). Cascade delete testée en intégration (BACK-080, 28/07/2026). RGPD Art. 17 100% couvert avec le cron ajouté dans BACK-077 (voir [DEC-037](#dec-037)).

---

### DEC-059 : Projet `MemoRecipe.Tests.Shared` + factories test spécialisées par héritage

- **Statut** : ✅ ACTIVE
- **Date** : Fin juillet à mi-août 2026 (BACK-080 + US-A2-06)

- **Choix** :
  Deux patterns tests complémentaires formalisés autour de la même intention "isolation des configurations spécifiques de test sans polluer la factory de base" :
  1. **Nouveau projet `MemoRecipe.Tests.Shared`** dans `tests/` avec csproj minimal qui référence uniquement `MemoRecipe.Application` (pas d'API, pas d'Infrastructure). Contient les fakes transverses utilisés par plusieurs projets de tests (aujourd'hui `FakeAlertingService` dans le namespace `MemoRecipe.Tests.Shared.Fakes`).
  2. **Factories test spécialisées par héritage** : classes qui étendent `CustomWebApplicationFactory<Program>` pour modifier UNE dimension de config, sans polluer la factory de base. Aujourd'hui 2 factories spécialisées en place :
     - `NoRateLimitApplicationFactory` (BACK-080) : bascule sur `Environment=Testing-NoRateLimit`, le `Program.cs` API skippe `UseRateLimiter()` dans cet environnement (utilisé par les tests Login qui hameraient le policy `auth` 10 req/min).
     - `LowQuotaWebApplicationFactory` (US-A2-06) : override `RecipeLimits:MaxPerUser = 2` via `AddInMemoryCollection` sur `ConfigureAppConfiguration`, permet de tester le comportement au quota sans devoir seed 200 recettes.

- **Pourquoi ces choix** :
  - **Rule of three (Fowler) pour Tests.Shared** : le premier fake réutilisé entre 2 projets tests peut vivre en local. Dès qu'un troisième projet a besoin du même fake, on extrait dans un projet transverse dédié. `FakeAlertingService` a atteint ce seuil.
  - **Politique d'admission stricte** : seuls les fakes utilisés par 3+ projets et sans dépendance API ou Infrastructure entrent dans `Tests.Shared`. Évite qu'il devienne un fourre-tout.
  - **Factories par héritage plutôt que flags dans la factory de base** : la factory `CustomWebApplicationFactory` reste focalisée sur "lancer un Postgres TestContainers et un serveur API standard". Chaque scénario spécifique (no rate limit, low quota, futur) hérite et override juste ce qu'il faut, sans polluer les 90% des tests qui utilisent la factory de base.
  - **Pattern reproductible** : ajouter une nouvelle dimension de config = 20 lignes de code (un nouveau fichier `.cs` qui hérite et override). Zéro modification de la factory de base ni du code applicatif.
  - **Isolation propre** : chaque test utilise `IClassFixture<FactorySpecifique>` et obtient un environnement configuré exactement pour son scénario, sans effet de bord sur les autres classes de tests.

- **Alternatives écartées** :
  - **Flags booléens dans `CustomWebApplicationFactory`** (par exemple `bool RateLimitEnabled = true`) : pollue la factory de base, force chaque test à savoir quels flags positionner, multiplie les branches conditionnelles.
  - **Constructeurs paramétrés sur la factory de base** : idem, complexifie l'usage courant pour un besoin marginal.
  - **Répliquer la logique dans chaque test** : anti-DRY, la config Postgres TestContainers est complexe à reproduire.
  - **Fakes réutilisés dans un dossier `Shared/` local à chaque projet tests** : fonctionne pour 2 projets mais casse dès qu'un 3ème arrive (double copie).

- **Sources** :
  - Rule of three (Martin Fowler)
  - Fixture pattern xUnit (`IClassFixture`)
  - Cohérence avec [DEC-009](#dec-009) et [DEC-033](#dec-033)

- **Conséquences** :
  - Nouveau projet `tests/MemoRecipe.Tests.Shared/` avec csproj minimal et namespace `MemoRecipe.Tests.Shared.Fakes`
  - `FakeAlertingService` extrait vers ce projet (utilisé par plusieurs projets tests)
  - 2 factories spécialisées dans `tests/MemoRecipe.Api.Tests/Helpers/` (`NoRateLimitApplicationFactory`, `LowQuotaWebApplicationFactory`)
  - Documentation implicite via le nom des classes (auto-descriptif)
  - Chaque test peut choisir sa factory via `IClassFixture<FactoryXxx>` sans effort de configuration

- **Conditions qui invalideraient ce choix** :
  - Volume de factories tel que la maintenance devient lourde (par exemple 15+ factories différentes) qui suggérerait un pattern de composition (par exemple decorators) plutôt que d'héritage.
  - Émergence d'un besoin cross-project pour une fixture partagée (au-delà de fakes) qui étendrait le scope de `Tests.Shared` au-delà de son intention initiale.

- **État** : APPLIQUÉ. `Tests.Shared` créé dans BACK-080 (28/07/2026, PR #40). `NoRateLimitApplicationFactory` livré dans BACK-080. `LowQuotaWebApplicationFactory` livré dans US-A2-06 (17-18/08/2026).

---

### DEC-060 : Pattern exceptions métier typées (extension de DEC-012)

- **Statut** : ✅ ACTIVE
- **Date** : Fin juillet à août 2026 (formalisation d'un pattern émergent sur plusieurs US)

- **Choix** :
  Formaliser le pattern d'utilisation d'exceptions custom typées pour signaler les erreurs métier depuis les services vers le middleware d'exceptions, plutôt que des codes de retour ou des flags booléens. Chaque erreur métier a sa propre classe d'exception dérivée de `Exception`, avec les données contextuelles nécessaires (par exemple `int Limit` pour `RecipeLimitReachedException`, `int RetryAfterSeconds` pour `AiRateLimitExceededException`). Le `ExceptionMiddleware` centralise le mapping exception → réponse HTTP avec un catch spécifique par type, garantissant une réponse cohérente au client (status code + body JSON structuré).

- **Pourquoi ces choix** :
  - **Extension du pattern DEC-012** : le principe global du middleware d'exceptions est ACTIVE depuis mars 2026. Ce qui est nouveau, c'est la formalisation qu'on **crée systématiquement une exception typée** pour chaque erreur métier plutôt que d'utiliser des exceptions génériques (`InvalidOperationException`) qui obligeraient à parser le message.
  - **Exceptions typées facilitent le catch discriminant côté frontend** : le frontend Web catch `AiRateLimitException`, `RecipeLimitException`, etc. avec un traitement spécifique par type (message FR contextualisé, UX différenciée). Sans exceptions typées, le frontend devrait parser le message d'erreur (fragile).
  - **Body JSON avec discriminant `error: "recipe_limit_reached"`** : permet au frontend de distinguer 2 exceptions qui produisent le même status code (par exemple 2 sources différentes de 403) via un champ machine-readable, en plus du status HTTP.
  - **Placement métier dans `Application/Exceptions/`** : cohérent avec la Clean Architecture ([DEC-002](#dec-002)), les exceptions métier vivent dans la couche Application, pas dans l'API.
  - **Wire dans `ExceptionMiddleware`** ([DEC-012](#dec-012)) : catches spécifiques ordonnés du plus spécifique au plus général, un catch final `Exception` capture tout le reste avec un 500 générique + alerte Telegram.

- **Exceptions actuellement en place** :
  - `AccountMarkedForDeletionException` (BACK-005) : 403 sur toute opération d'écriture après demande de suppression
  - `AiRateLimitExceededException(string tier, int retryAfterSeconds)` (US-A2-04, [DEC-047](#dec-047)) : 429 avec header `Retry-After`
  - `RecipeLimitReachedException(int limit)` (US-A2-06, [DEC-049](#dec-049)) : 403 avec discriminant `error: "recipe_limit_reached"`
  - `PromptInjectionDetectedException(string pattern)` (worker IA, [DEC-047](#dec-047)) : gérée dans le worker, remonte 500 à l'API si non catch, à traiter en amélioration future

- **Alternatives écartées** :
  - **Codes de retour (Result<T> pattern)** : plus verbeux dans le code métier, chaque appelant doit checker le succès manuellement. Les exceptions typées propagent naturellement jusqu'au middleware sans code intermédiaire.
  - **Une seule exception générique `BusinessException(int statusCode, string errorCode)`** : moins discriminant à l'usage, force le middleware à checker `ex.ErrorCode` pour décider quoi faire. Plusieurs classes typées avec catches spécifiques est plus C#-idiomatique.
  - **Retourner un objet réponse HTTP directement depuis le service** : couple la couche Application à HTTP, casse Clean Architecture.

- **Sources** :
  - Cohérence avec [DEC-002](#dec-002) Clean Architecture
  - Extension de [DEC-012](#dec-012) middleware global d'exceptions

- **Conséquences** :
  - Nouveau dossier `MemoRecipe.Application/Exceptions/` qui regroupe les exceptions métier
  - `ExceptionMiddleware` enrichi de catches spécifiques (voir DEC-012 note d'évolution)
  - Frontend Blazor a un dossier symétrique `App/MemoRecipe.Web/Exceptions/` avec les mêmes types (par exemple `RecipeLimitException`, `AiRateLimitException`) pour un catch côté client
  - Pattern reproductible : ajouter une nouvelle erreur métier = nouvelle classe d'exception + nouveau catch dans le middleware + éventuellement nouveau type côté frontend

- **Conditions qui invalideraient ce choix** :
  - Migration vers un pattern `Result<T>` généralisé (par exemple avec LanguageExt) qui remplacerait les exceptions par des types union. Refactor structurel majeur.
  - Framework qui offrirait un mapping automatique exception → réponse HTTP standardisé (par exemple RFC 7807 Problem Details généralisé), rendant le middleware custom redondant.

- **État** : APPLIQUÉ. Pattern émergent formalisé rétrospectivement le 20/08/2026 lors de la refonte doc US-A2-11. Les 3 exceptions API + 1 exception worker sont en place et wire. Ce pattern est utilisé pour toutes les nouvelles erreurs métier depuis fin juillet 2026.

---

### DEC-061 : Environnement dev containerisé via DevContainer + service IA dans Compose dev (dev/prod parity)

- **Statut** : ✅ ACTIVE
- **Date** : 21/08/2026 (décision produit prise en fin de session Alpha.2), formalisation ADR le 22/08/2026 suite intégration audit externe pré-beta.1

- **Choix** :
  Trois volets combinés. Un DevContainer VSCode avec image Docker Linux (`mcr.microsoft.com/devcontainers/dotnet:10.0`) plus install natif Tesseract, libwebp et libheif via `apt-get`. VSCode se connecte via extension Remote Containers, le développeur code DANS le container avec hot reload natif via `dotnet watch`. Le worker IA (Azure Function .NET 8 avec Tesseract) tourne en container Linux dans le compose dev enrichi, comme en production, au lieu de tourner sur `func start` sur la machine hôte. Une fiche pédagogique `documentation/fiches/DEVCONTAINER-CHEATSHEET.md` couvre l'onboarding rapide (setup, workflow quotidien, rebuild, troubleshooting, migration future vers cluster K8s ou cloud managé).

- **Pourquoi ces choix** :
  - **12-Factor App Factor X (Dev/Prod parity)** : principe standard industrie qui exige de garder dev, staging et prod aussi similaires que possible pour éliminer les surprises "ça marche chez moi mais pète en prod".
  - **Divergence silencieuse observée en Alpha.2** : Tesseract sans libwebp en local Windows contre Tesseract avec libwebp en prod Linux Alpine, faisant crasher le path OCR fallback sur les images WebP en dev mais fonctionnant en prod. Ce type de gap va s'amplifier avec chaque nouvelle dépendance native ajoutée (libheif pour HEIC, ImageMagick pour conversion).
  - **DevContainer VSCode est un standard 2026** : spécification Microsoft, adoption massive dans l'industrie, signal recruteur reconnu (maturité DevOps).
  - **Worker IA en Compose dev plutôt qu'en local `func start`** : c'est le composant qui a le plus de dépendances natives (Tesseract, libwebp, libheif), donc celui qui bénéficie le plus de la containerisation côté dev.

- **Alternatives écartées** :
  - **Compose dev sans DevContainer** : l'IDE reste sur l'OS local du contributeur, gap partiellement résolu (les libs natives restent OS-spécifiques à installer manuellement). Signal recruteur moindre.
  - **DevContainer sans service IA dans Compose dev** : le worker IA continue à tourner sur `func start` local. L'API et le frontend sont en parity mais l'IA (composant qui a le plus de dépendances natives) reste divergent, ce qui manque le principal risque à couvrir.
  - **Statu quo (tout local)** : chaque contributeur doit installer Tesseract, libwebp, libheif et Azure Functions Core Tools manuellement. Onboarding lent, gap dev/prod persistant, risque d'accumulation de divergences.

- **Sources** :
  - 12-Factor App methodology, Factor X (Dev/Prod parity) : https://12factor.net/dev-prod-parity
  - VSCode DevContainer specification : https://containers.dev/

- **Conséquences** :
  - Dev/prod parity garantie sur toute la stack (API, frontend, worker IA, dépendances natives)
  - Onboarding contributeur en environ 10 minutes via "Reopen in Container" au lieu de plusieurs heures d'install manuelle
  - Débloque le test manuel WebP en local sur le path OCR fallback (prérequis BACK-105 Support formats étendus)
  - Débloque les tests E2E Playwright cross-OS via un environnement Linux uniforme (cf. US-21)
  - Coût : premier build container environ 5 à 10 minutes, rebuild uniquement à chaque modification du Dockerfile, du SDK ou de l'OS de base
  - Prérequis : Docker Desktop installé sur la machine du contributeur

- **Conditions qui invalideraient ce choix** :
  - Migration de tout le stack (API, frontend, IA) vers un environnement managé cloud (par exemple GitHub Codespaces natif ou Gitpod) qui rendrait le DevContainer local redondant.
  - Décision de retirer toutes les dépendances natives (Tesseract, libheif, libwebp) en migrant vers 100 pourcents Vision LLM (path OCR fallback supprimé), qui réduirait le gap dev/prod natif à un niveau où le DevContainer ne serait plus justifié.

- **État** : 🔵 PLANIFIÉE. À appliquer en US-B1-20 (première US du sprint Alpha.3, prévue démarrage 22-24/08/2026).

---

### DEC-062 : Branch Protection Rule stricte sur `main` (Classic, 8 required checks, no bypass)

- **Statut** : ✅ ACTIVE
- **Date** : 26/08/2026 (setup découvert manquant au moment de merger US-22 P0-2 sur `main`)

- **Choix** :
  Activation d'une Branch Protection Rule Classic sur la branche `main` du repo GitHub, avec 8 status checks required (`test-api`, `test-ia`, `build-web`, `vuln-audit`, `lighthouse-a11y` issus du workflow `.github/workflows/ci.yml` + `Code scanning results / CodeQL` agrégat GitHub Advanced Security + `CodeQL Advanced / Analyze (actions)` + `CodeQL Advanced / Analyze (csharp)` issus du workflow `.github/workflows/codeql.yml`). Toutes les PRs doivent être à jour avec `main` avant merge (`Require branches to be up to date`). Case `Do not allow bypassing the above settings` cochée : même l'owner du repo ne peut pas bypasser. Force push et deletion de `main` bloqués (`Allow force pushes` et `Allow deletions` décochés). `Require approvals` volontairement DÉCOCHÉE (contrainte solo dev, GitHub interdit self-approval sur ses propres PRs). Job conditionnel `build-and-push` (tag `v*`) volontairement exclu des required checks (skipped sur PRs normales, le mettre en required bloquerait toutes les PRs).

- **Pourquoi ces choix** :
  - **Trou de sécurité gouvernance identifié tardivement** : depuis la création du repo aucune Branch Protection Rule active → merge autorisé sans CI verte, sans review, force push possible. Découverte au moment de merger P0-2 le 26/08 (GitHub proposait le bouton merge alors que la CI n'avait pas encore tourné à cause de l'incident Actions concomitant). Correction immédiate.
  - **Belt-and-suspenders sur CodeQL (3 checks au lieu d'1)** : le check aggregé `Code scanning results / CodeQL` (badge GitHub Advanced Security) bloque si le SCAN trouve une alerte critique, mais si le WORKFLOW YAML lui-même plante (erreur infra, quota, syntax) le scan ne s'exécute pas et l'aggregé ne bloque pas. Les 2 jobs Actions `Analyze (actions)` + `Analyze (csharp)` couvrent ce cas défensivement.
  - **Do not allow bypassing** : sans cette case, l'owner du repo voit systématiquement un bouton "Merge without waiting for requirements" → la protection ne sert à rien en pratique. Case cochée = seule vraie sécurité, aucune exception urgence.
  - **Require approvals décoché** : contrainte technique GitHub (self-approval interdit sur ses propres PRs). Sur un projet solo dev en Alpha, activer cette option = plus aucune PR jamais mergeable. À réactiver en V2 quand review par IA (ex : CodeRabbit) ou team review disponible.
  - **Require branches to be up to date activé** : force `rebase`/`merge main` avant merge → la CI re-tourne sur l'état RÉEL de `main`. Sans ça, scenario cassé : PR verte mergée alors que `main` a évolué entretemps → régression silencieuse possible.
  - **Classic plutôt que Rulesets** : Rulesets = système moderne GitHub 2024+ plus flexible mais plus complexe à setup et à comprendre. Choix pragmatique pour aller vite sur ce trou de sécurité identifié. Migration Rulesets envisageable en V2 si besoin de règles multi-branches ou d'exceptions bypass sur liste d'users.

- **Alternatives écartées** :
  - **Pas de Branch Protection** (statu quo) : maintien du trou de sécurité, aucune garantie sur ce qui merge sur `main`. Refusé.
  - **Rulesets moderne** : plus flexible mais setup plus long, pas de bénéfice concret pour un repo solo dev V1. Reporté à V2 si migration nécessaire.
  - **Require approvals + bypass exception sur owner** : Rulesets uniquement (pas dispo en Classic). Contournerait la limite self-approval mais nécessiterait migration Rulesets + coût cognitif pour maintenir la liste bypass à jour.
  - **Require signed commits (GPG)** : DÉCOCHÉ. Nécessite setup GPG local complexe pour un bénéfice sécu marginal en solo dev. Envisageable en V2 si contribution externe ou audit sécu formel.
  - **Require linear history (interdit merge commits)** : DÉCOCHÉ. Contrainte non-nécessaire, le squash-merge (comportement par défaut choisi sur les PRs) donne déjà un historique propre linéaire sur `main`.

- **Sources** :
  - GitHub Docs — About protected branches : https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches
  - GitHub Docs — About rulesets : https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets
  - OWASP DevSecOps guidelines — Branch protection

- **Conséquences** :
  - Zéro merge sur `main` sans les 8 checks CI verts (fonctionnels + sécurité + a11y + SAST)
  - Zéro force push, zéro deletion accidentelle de `main`
  - Owner du repo inclus dans la protection = pas d'exception urgence "je merge quand même"
  - En cas d'incident CI GitHub (comme celui du 26/08), attente obligatoire de la reprise CI avant merge (workaround = push commit vide pour retrigger workflow)
  - Chaque nouveau check CI ajouté au projet doit être manuellement ajouté à la liste required checks de la protection rule (sinon il tourne mais ne bloque pas)

- **Conditions qui invalideraient ce choix** :
  - Passage à une team ≥ 2 devs : réactiver `Require approvals: 1` (self-approval GitHub débloquée quand un autre dev peut review)
  - Migration vers Rulesets pour setup multi-repo ou règles conditionnelles (ex : différentes protections selon environnement/branche)
  - Ajout d'une review IA automatisée (ex : CodeRabbit) qui pourrait remplir le rôle d'approbation reviewer

- **État** : ✅ ACTIVE. Setup GitHub Settings → Branches → Classic branch protection rule sur pattern `main`, 26/08/2026.

---

### DEC-063 : Amendement DEC-021 — `'unsafe-inline'` dans `script-src` CSP pour compatibilité Blazor WASM importmap

- **Statut** : ✅ ACTIVE (amende [DEC-021](#dec-021))
- **Date** : 27/08/2026 (découvert lors du test browser P0-5)

- **Choix** :
  Amendement de la Content-Security-Policy définie dans DEC-021 : ajout du token `'unsafe-inline'` dans la directive `script-src`. Valeur finale de la CSP :
  ```
  default-src 'self'; script-src 'self' 'wasm-unsafe-eval' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'
  ```
  Appliquée UNIFORMÉMENT côté API middleware (`SecurityHeadersMiddleware.cs`) ET côté nginx front (`App/MemoRecipe.Web/nginx.conf` bloc `server`), pour cohérence front/API et éviter les gaps sécu subtils.

- **Pourquoi ces choix** :
  - **Blazor WASM impose un inline script dans `index.html`** : la balise `<script type="importmap"></script>` (ligne 27 du template Blazor WebAssembly) est le mécanisme standard Blazor .NET 8+ pour le fingerprinting des modules `dotnet.js` / `dotnet.wasm` à runtime. Elle est OBLIGATOIREMENT inline (spec importmap HTML). Impossible de la déplacer dans un fichier `.js` externe.
  - **Le test browser P0-5 a révélé la limitation** : la CSP de DEC-021 n'avait jamais été fonctionnellement testée contre une page HTML Blazor (elle était appliquée uniquement aux réponses JSON `/api/*` côté API middleware, où aucun script inline n'existe). L'application de la même CSP côté nginx (P0-5) sur les réponses HTML statiques a causé un blocage immédiat de Blazor par la console DevTools : `Executing inline script violates the following Content Security Policy directive`. Blazor ne bootait pas, page blanche.
  - **Cohérence front/API obligatoire** : ajouter `'unsafe-inline'` uniquement côté nginx et pas côté API middleware créerait une désynchronisation subtile. Un attaquant exploitant une XSS via une réponse API (même si ces réponses sont normalement JSON, un cas d'erreur mal traité pourrait renvoyer du HTML) pourrait profiter du CSP moins strict côté nginx en injectant un script inline. Sync obligatoire pour éviter les surfaces d'attaque asymétriques.
  - **Compromis sécu accepté et documenté** : `'unsafe-inline'` sur `script-src` réduit la protection contre les XSS injectés (un attaquant peut exécuter `<script>` inline). Mais MemoRecipe dispose de plusieurs autres couches de défense qui rendent ce compromis acceptable : validation FluentValidation sur tous les inputs utilisateur, PasswordHasher PBKDF2 (DEC-020), Serilog structured logging avec `EmailMasker` + `ValidationErrorSanitizer` (DEC-051), cookies `HttpOnly + Secure + SameSite=Strict` (DEC-014), rate limiting double couche IP + email (DEC-022), upload defense-in-depth extension + MIME + magic bytes (DEC-057), CSP `frame-ancestors 'none'` + `X-Frame-Options DENY` anti-clickjacking, CSP `object-src 'none'` anti-plugins legacy, CSP `base-uri 'self'` anti-injection `<base>`, CSP `form-action 'self'` anti-vol formulaire.
  - **Alignement avec `'unsafe-inline'` déjà présent côté `style-src`** : la CSP DEC-021 originale acceptait déjà `'unsafe-inline'` pour les styles (imposé par MudBlazor qui injecte des styles inline à runtime). Ajouter `'unsafe-inline'` pour scripts complète la cohérence de traitement des ressources inline. Les 2 tokens sont dictés par les frameworks utilisés.
  - **Documentation Microsoft** : Blazor WASM + CSP est une limitation documentée sur Microsoft Learn. `'unsafe-inline'` est la solution recommandée V1 avant l'adoption de patterns plus sophistiqués (nonce, strict-dynamic).

- **Alternatives écartées** :
  - **Hash SHA256 de l'importmap** (`'sha256-...'`) : plus strict, autorise UNIQUEMENT le contenu exact de l'importmap actuel. Rejeté car FRAGILE : Blazor génère un nouveau fingerprint à chaque `dotnet publish`, donc le hash change à chaque build → CSP fail → app cassée en prod. Non déterministe cross-build.
  - **CSP nonce** : générer un nonce cryptographique unique par requête, injecter dans le HTML (`<script nonce="...">`) et dans le CSP header (`script-src 'nonce-...'`). Approche la plus sécurisée mais complexité setup lourde (middleware ASP.NET custom, template Razor modifié à runtime, désynchronisation possible avec le contenu nginx statique). Reporté V2.
  - **`'strict-dynamic'`** : autorise les scripts chargés dynamiquement par un script trusté (via nonce ou hash). Puissant mais nécessite quand même un mécanisme de trust initial (nonce ou hash). Écarté pour complexité + fragilité identique au hash.
  - **Rester sur DEC-021 stricte** : refusé car Blazor WASM inchargeable → app cassée en prod.
  - **CSP différenciée par path** (`/api/*` strict, `/` avec `unsafe-inline`) : casserait le principe d'uniformité front/API et créerait une surface d'attaque asymétrique.

- **Sources** :
  - Microsoft Docs — ASP.NET Core Blazor WebAssembly with content security policy : https://learn.microsoft.com/en-us/aspnet/core/blazor/security/content-security-policy
  - MDN — Content Security Policy directives `script-src` : https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/script-src
  - OWASP CSP Cheat Sheet : https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html
  - Spec importmap HTML : https://html.spec.whatwg.org/multipage/webappapis.html#import-maps

- **Conséquences** :
  - Blazor WASM se charge correctement en production (Frontend fonctionnel)
  - Uniformité CSP nginx front + API middleware garantie (audit-trail cohérent)
  - Slight regression sécu : un XSS injecté pourrait exécuter des scripts inline (mais couvert par les autres couches défense listées ci-dessus)
  - Le fichier de test `SecurityHeadersMiddlewareTests.cs` reflète la nouvelle valeur (mis à jour dans P0-5, InlineData ligne 23)
  - Documentation `HTTP-SECURITY-HEADERS.md` et `SECURITY-BASELINE-API-NGINX.md` à jour (déjà fait)
  - Le workaround est visible dans le body PR P0-5 pour audit-trail rapide

- **Conditions qui invalideraient ce choix** :
  - Migration Blazor vers un rendering server-side (Blazor Server / Blazor SSR) qui n'utilise plus l'importmap client-side → possible retour à CSP strict sans `'unsafe-inline'`
  - Adoption CSP nonce-based via middleware ASP.NET custom (envisagé V2) → retour au CSP strict pour scripts, avec injection nonce dans HTML servi par nginx
  - Retrait complet de MudBlazor + Blazor WASM (peu probable V1) → retour possible à CSP strict

- **État** : ✅ ACTIVE. Appliqué en P0-5 (27/08/2026) simultanément côté nginx (`App/MemoRecipe.Web/nginx.conf`) et côté API middleware (`SecurityHeadersMiddleware.cs`) via commits atomiques séparés sur la branche `fix/US-22-P0-5-nginx-security-headers`.

---

## Investigations en cours

Cette section liste les points identifiés qui méritent une évaluation mais qui ne sont pas critiques et n'ont pas encore été tranchés en décision formelle.

### INV-001 : Appel `api/auth/me` retourne 401 sur les pages publiques

- **Constat** : Le `CookieAuthStateProvider` appelle systématiquement `api/auth/me` au chargement de l'app, même sur `/login` et `/register`. Retourne 401 si pas de cookie, visible en console DevTools (erreur rouge cosmétique).
- **Impact** : Aucun impact fonctionnel. Cosmétique uniquement.
- **Options à évaluer** :
  1. Ignorer, pattern standard des SPAs, pas visible par l'utilisateur final.
  2. Flag `localStorage` non-sensible (`isLoggedIn` true/false) pour éviter l'appel quand pas connecté.
- **État** : À ÉVALUER

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
