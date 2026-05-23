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
