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

### DEC-017 : Frontend → API → Azure Function IA (pas d'appel direct)
- **Date** : Mars 2026
- **Choix** : Le frontend envoie l'image à l'API, qui appelle l'Azure Function IA. Le frontend ne communique jamais directement avec l'Azure Function.
- **Pourquoi** : Un seul point d'entrée sécurisé (cookies HttpOnly déjà en place). L'Azure Function peut rester privée/interne. Meilleur contrôle RGPD (traçabilité, audit, suppression des images). Compatible MAUI (même endpoint API). L'utilisateur n'a pas besoin de connaître l'existence du service IA.
- **Conséquence** : Nouveau service `IOcrScanService` (Application) / `OcrScanService` (Infrastructure) pour l'appel HTTP. Endpoint `POST api/recipe/scan` dans `RecipeController`. URL Azure Function configurable dans `appsettings.json`.
- **Etat** : EN COURS — endpoint créé, frontend connecté, preview fonctionnel. Reste : validation formulaire et sauvegarde en BDD.

### DEC-018 : RecipeFormModel séparé des DTOs API + composant RecipeForm réutilisable
- **Date** : Mars 2026
- **Choix** : Le formulaire de recette utilise un `RecipeFormModel` dédié (pas un DTO API) et vit dans un composant `RecipeForm.razor` réutilisable. Chaque page parente mappe vers le DTO approprié (`RecipeCreateDto` ou `RecipeUpdateDto`) avant d'appeler l'API.
- **Pourquoi** : Single Responsibility — le formulaire ne doit pas dépendre d'un contrat API. `RecipeFormModel` = ce que l'utilisateur voit et édite. Le même composant est réutilisé dans 3 contextes : scan (pré-rempli par l'IA), création manuelle (vide), modification (pré-rempli depuis l'API). Le parent décide du verbe HTTP (POST vs PUT), pas le formulaire.
- **Conséquence** : `RecipeFormModel` dans `Models/`, `RecipeForm.razor` dans `Components/`. Le composant expose un `[Parameter] RecipeFormModel` et un `[Parameter] EventCallback<RecipeFormModel>` pour notifier le parent au clic "Sauvegarder".
- **Etat** : DONE — composant intégré dans Scan, Edit et création manuelle (future).

### DEC-019 : Code-behind pattern + `= default!;` pour les pages Blazor
- **Date** : Mars 2026
- **Choix** : Séparer chaque page en `.razor` (template) + `.razor.cs` (code C#). Utiliser `= default!;` sur les propriétés `[Inject]` pour supprimer les warnings nullable.
- **Pourquoi** : Séparation des responsabilités (SRP) — le template ne contient que du HTML/Razor, le C# est dans une classe `partial`. `= default!;` est le pattern recommandé par Microsoft pour les injections Blazor ([doc officielle](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection)).
- **Conséquence** : Les `@inject` du `.razor` deviennent `[Inject]` dans le `.razor.cs` avec `{ get; set; } = default!;`. Les `using` doivent être ajoutés manuellement dans le `.razor.cs` (pas d'accès aux `@using` de `_Imports.razor`).
- **Etat** : DONE — appliqué sur RecipeDetail, Recipes, ScanRecipe, EditRecipe.

### DEC-020 : Migration du hashing des mots de passe — HMAC-SHA512 → PBKDF2 (PasswordHasher\<T\>)
- **Date** : Avril 2026
- **Choix** : Remplacer le hashing custom `HMACSHA512` par `PasswordHasher<User>` de `Microsoft.AspNetCore.Identity` (PBKDF2, 100 000 itérations, salt intégré).
- **Pourquoi** : `HMACSHA512` est un algorithme rapide (milliards de hash/seconde) — vulnérable au brute force si la BDD est compromise. `PasswordHasher<T>` utilise PBKDF2 avec un work factor élevé, rendant le brute force impraticable. C'est le standard recommandé par Microsoft ([doc officielle](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0)).
- **Migration douce** : Les utilisateurs existants (hashés avec l'ancien algo) sont migrés automatiquement à la prochaine connexion — le login vérifie l'ancien hash, re-hash avec PBKDF2, vide le `PasswordSalt`, et sauvegarde. La méthode `VerifyLegacy()` est conservée temporairement pour la rétrocompatibilité.
- **Conséquence** : `PasswordHasher` n'est plus `static`, injecté via DI. Le champ `PasswordSalt` reste en BDD (pour vérifier les anciens hash) mais est vide pour les nouveaux users. `IUserRepository` a une nouvelle méthode `Update()`. À terme : supprimer `VerifyLegacy()` et le champ `PasswordSalt` quand tous les users auront migré.
- **Etat** : DONE — migration douce en place, testée avec comptes existants.

### DEC-021 : SecurityHeadersMiddleware custom plutot que packages tiers
- **Date** : Avril 2026
- **Choix** : Middleware custom dans `MemoRecipe.Api/Middlewares/SecurityHeadersMiddleware.cs` qui ajoute 6 headers de securite (X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, CSP, HSTS) sur chaque reponse.
- **Pourquoi** : Les headers sont statiques et peu nombreux — un middleware custom de ~20 lignes est plus simple et transparent qu'un package tiers (NWebsec, etc.). On garde le controle total sur les valeurs. CSP adapte a Blazor WASM (`wasm-unsafe-eval`) + MudBlazor (`unsafe-inline` pour style-src) + Google Fonts.
- **HSTS conditionnel** : `Strict-Transport-Security` ajoute uniquement en production (`!IsDevelopment()`), car HSTS casserait le dev local en HTTP/certificats auto-signes.
- **X-XSS-Protection volontairement omis** : Header deprecie (MDN 2025), peut creer des failles XSS. CSP le remplace entierement.
- **Sources** : [OWASP HTTP Headers Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html), [MDN Security Headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers), [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/).
- **Etat** : DONE — BACK-001, 7 tests d'integration.

### DEC-022 : Rate limiting double couche — IP natif + per-account custom
- **Date** : Avril 2026
- **Choix** : Deux niveaux de rate limiting complementaires. Niveau 1 : `AddRateLimiter()` natif ASP.NET Core avec Fixed Window par IP (global 100/min, auth 10/min, scan 5/min). Niveau 2 : compteur custom par email dans `AuthService` avec `IMemoryCache` (5 echecs → blocage 15 min).
- **Pourquoi** : Le rate limiting par IP ne suffit pas contre le credential stuffing (botnets avec milliers d'IP). Le rate limiting par compte via `IMemoryCache` bloque AVANT la verification du mot de passe (evite le timing attack). Le rate limiter natif `AddPolicy()` avec partition par `httpContext.User` ne fonctionne PAS pour le login car `UseRateLimiter` s'execute avant `UseAuthentication`.
- **LoginResult pattern** : `LoginAsync` retourne un objet `LoginResult` (Token + IsLockedOut) au lieu de `string?` pour permettre au controller de distinguer 401 (mauvais identifiants) de 429 (compte bloque).
- **Retry-After** : Header ajoute via `OnRejected` callback (valeur fixe 60s). Le `RejectionStatusCode` par defaut est 503, pas 429 — doit etre configure explicitement ou gere dans `OnRejected`.
- **Sources** : [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit), [OWASP Credential Stuffing Prevention](https://cheatsheetseries.owasp.org/cheatsheets/Credential_Stuffing_Prevention_Cheat_Sheet.html).
- **Etat** : DONE — BACK-002, 3 tests d'integration. Logs des tentatives bloquees reportes a BACK-010 (Serilog).

### DEC-023 : CORS dynamique via appsettings + fail fast au demarrage
- **Date** : Avril 2026
- **Choix** : Externaliser les origines CORS dans `appsettings.json` (`Cors:AllowedOrigins` array) au lieu d'un string hard-code. Resserrer les permissions : `WithHeaders("Content-Type")` au lieu de `AllowAnyHeader()`, `WithMethods("GET", "POST", "PUT", "DELETE")` au lieu de `AllowAnyMethod()`. Validation au demarrage qui leve une exception si la config est manquante.
- **Pourquoi** : En production, le frontend sera sur un autre domaine que `localhost:5110`. Le hard-coding empechait tout deploiement. L'array permet plusieurs origines (ex: `https://memorecipe.com` + `https://www.memorecipe.com`). Resserrer les methods/headers reduit la surface d'attaque (principe du moindre privilege).
- **`Authorization` non whiteliste** : L'authentification passe par le cookie `authCookie` (envoye automatiquement via `AllowCredentials()`), pas par un header `Authorization: Bearer`. Pas besoin de l'autoriser explicitement.
- **`OPTIONS` non liste dans `WithMethods`** : Les requetes preflight sont gerees automatiquement par le middleware CORS — l'ajouter manuellement est redondant et peut causer des conflits (doc Microsoft).
- **Fail fast** : Si `Cors:AllowedOrigins` est absent ou vide au demarrage → `InvalidOperationException`. Mieux vaut crasher avec un message clair que tourner avec un CORS mal configure.
- **Sources** : [ASP.NET Core CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors), [MDN CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CORS).
- **Etat** : DONE — BACK-003, 3 tests d'integration.

### DEC-024 : Pas de token anti-CSRF (protection par SameSite=Strict + CORS)

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

### DEC-026 : Migration AutoMapper → Mapperly (source generator, OSS MIT, mappers statiques)

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
- **État** : DÉCIDÉ le 23/05/2026 — implémentation en cours sur la branche `feature/BACK-046-migrate-to-mapperly`. Sera marqué DONE quand BACK-046 sera complètement clôturé.


### DEC-027 : nginx:alpine pour servir le Blazor WASM (au lieu d'aspnet runtime)

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
- **État** : DÉCIDÉ et appliqué le 28/05/2026 (BACK-007 partie 2, PR #13).


### DEC-028 : Frontend ↔ API via reverse proxy nginx (Option B), pas de CORS exposé

- **Date** : 29 mai 2026
- **Choix** : Pour la composition prod (BACK-007 partie 3), le nginx du container Frontend **proxifie `/api/*`** vers le container API en interne au réseau Docker (`proxy_pass http://api:8080/api/`). L'API n'est **pas exposée** publiquement. Le bundle Blazor WASM utilise une **URL relative `/api/...`** (même origine), donc **zéro CORS** en prod.
- **Pourquoi** :
  - **Surface d'attaque réduite** : l'API n'écoute qu'en interne au réseau Docker, jamais joignable depuis Internet. Vis-à-vis OWASP A05:2025 (Security Misconfiguration), c'est la posture la plus restrictive.
  - **Simplicité TLS** : 1 seul certificat HTTPS pour le sous-domaine `app.memorecipe.com` (Apache du host + Let's Encrypt via BACK-009), au lieu de 2 certificats pour 2 sous-domaines (`api.` + `app.`).
  - **Same-origin** : `SameSite=Strict` sur les cookies HttpOnly (DEC-024 CSRF) fonctionne parfaitement parce que le Frontend et l'API partagent l'origine. Pas de bidouille `credentials: include` cross-origin.
  - **Bundle WASM universel** : un seul build `dotnet publish -c Release` fonctionne en dev local (avec override `appsettings.Development.json`) ET en prod (URL relative via `HostEnvironment.BaseAddress`). Pas de rebuild par environnement.
  - **Pattern standard prod** : architecture SPA + API derrière un même reverse proxy = pratique recommandée chez la majorité des déploiements modernes (Caddy, Traefik, nginx).
- **Alternative considérée — Option A (Frontend appelle API en cross-origin)** :
  - L'API serait exposée sur `api.memorecipe.com` avec son propre certificat
  - CORS à configurer (déjà partiellement fait dans BACK-002 + BACK-023)
  - Cookies HttpOnly cross-origin = trade-off `SameSite=None; Secure` + `credentials: include` partout
  - Rebuild WASM par environnement (URL `api.memorecipe.com` dans le bundle compilé)
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
  - **L'API devient consommée par d'autres clients que le Frontend Blazor** (ex: mobile MAUI futur appelant directement, partenaires externes, microservices) → là `api.memorecipe.com` sous-domaine séparé + CORS strict devient pertinent. Mais le Frontend Web pourrait continuer en Option B en parallèle.
  - **Découplage Frontend / API souhaité** pour les déployer séparément (versions différentes, ratios de scaling différents) → 2 containers ≠ même origine.
- **État** : **DÉCIDÉ le 29/05/2026 et APPLIQUÉ le 01/06/2026** (BACK-007 partie 3, PR #14). Validé en E2E local : bundle Blazor WASM appelle `/api/*` en same-origin via nginx reverse proxy, **zéro CORS error** dans la console DevTools.


### DEC-029 : Compose security baseline — phasage volontaire du hardening avancé

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
- **État** : DÉCIDÉ le 04/06/2026 (visio mentor 04/06 + analyse comparative post-visio en solo). À implémenter dans **BACK-064** (étape 1B).


### DEC-032 : .NET Aspire (Option B) pour orchestration du stack dev + prod

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
- **État** : DÉCIDÉ le 04/06/2026 (visio mentor). À implémenter dans **BACK-062**.


### DEC-034 : Report du fix collation Postgres dev lors du passage Debian -> Alpine (warning accepte temporairement)

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
