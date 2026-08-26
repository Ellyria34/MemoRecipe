# Décisions Techniques (vue thématique)

> Vue synthétique par thème des choix techniques structurants de MemoRecipe. Pour l'historique complet et détaillé de chaque décision, avec contexte, alternatives écartées, sources et conséquences, voir [`ADR.md`](ADR.md).

## Comment lire ce document

Chaque section correspond à un thème. Pour chaque thème vous trouvez d'abord ce qui est appliqué aujourd'hui, puis un rappel synthétique de l'historique des décisions qui ont conduit à cet état. Les liens `[DEC-XXX](ADR.md#dec-xxx)` renvoient vers l'ADR complet pour aller en profondeur.

## Convention de mise à jour

Toute évolution technique structurelle suit un workflow en 2 temps pour maintenir les deux documents cohérents.

**1. Enregistrer dans ADR.md** : ajouter une nouvelle `DEC-XXX` (format ADR standard : Statut, Date, Choix, Pourquoi, Alternatives écartées, Sources, Conséquences, Conditions qui invalideraient ce choix, État) OU mettre à jour le champ `Statut` d'une DEC existante en cas de supersede, d'extension ou de nuance. Si la nouvelle DEC supersede une DEC précédente, enrichir la nouvelle pour qu'elle soit auto-suffisante en lecture.

**2. Répercuter dans DECISIONS.md** : localiser la section thématique impactée (parmi les 8), mettre à jour le paragraphe `État actuel` si l'évolution change ce qui est en place aujourd'hui, ajouter une entrée dans `Historique des décisions` en respectant l'ordre chronologique, et ajouter le lien `[DEC-XXX](ADR.md#dec-xxx)` dans la ligne `DEC détaillées` en fin de section.

**Principe** : ADR.md est la source de vérité chronologique et détaillée. DECISIONS.md est la vitrine thématique lisible qui pointe vers ADR.md. Les deux doivent rester synchronisés.

## Sommaire

1. [Architecture et fondations](#architecture-et-fondations)
2. [Frontend et UI](#frontend-et-ui)
3. [Authentification, sécurité et RGPD](#authentification-sécurité-et-rgpd)
4. [Base de données et backup](#base-de-données-et-backup)
5. [IA, LLM et scan](#ia-llm-et-scan)
6. [Infrastructure, Docker et déploiement](#infrastructure-docker-et-déploiement)
7. [Tests](#tests)
8. [Framework et patterns backend](#framework-et-patterns-backend)

---

## Architecture et fondations

**État actuel**

MemoRecipe est un monorepo Git qui abrite 3 briques indépendantes (IA, API, Frontend Web), chacune avec sa propre solution .NET et sa propre version de framework. Le Frontend ne communique jamais directement avec le service IA, tous les appels passent par l'API centrale.

L'API suit une Clean Architecture stricte en 4 couches (`MemoRecipe.Api`, `MemoRecipe.Application`, `MemoRecipe.Domain`, `MemoRecipe.Infrastructure`) avec le Repository Pattern pour tous les agrégats persistés (`Recipe`, `User`). Le principe fondateur "l'IA propose, le code décide" garantit qu'un changement de modèle IA ne casse pas le comportement métier (post-processing déterministe systématique sur toutes les sorties LLM).

**Historique des décisions**

La séparation en 3 briques indépendantes a été actée dès novembre 2025 ([DEC-001](ADR.md#dec-001)) pour permettre de faire évoluer chaque brique à son rythme (versions .NET différentes possibles, cycles de release découplés). Dans la foulée, l'API a adopté la Clean Architecture 4 couches ([DEC-002](ADR.md#dec-002)) et le principe "IA comme source de données, pas de vérité" ([DEC-003](ADR.md#dec-003)) qui reste un pilier du produit.

En mars 2026, deux décisions cadres ont été prises. D'abord, ne pas restructurer les dossiers du monorepo malgré une redondance cosmétique connue ([DEC-006](ADR.md#dec-006)), pour éviter de casser les chemins dans les .sln, .csproj, migrations et compose sans bénéfice réel. Ensuite, généraliser le Repository Pattern à tous les agrégats persistés ([DEC-007](ADR.md#dec-007)) via des interfaces dans `Application/Repositories/` et des implémentations dans `Infrastructure/Repositories/`, ce qui a corrigé une référence circulaire entre couches et permet les tests unitaires avec des fakes en mémoire.

**DEC détaillées** : [DEC-001](ADR.md#dec-001), [DEC-002](ADR.md#dec-002), [DEC-003](ADR.md#dec-003), [DEC-006](ADR.md#dec-006), [DEC-007](ADR.md#dec-007).

---

## Frontend et UI

**État actuel**

Le Frontend est un Blazor WebAssembly qui utilise MudBlazor comme librairie de composants UI. Le layout est adaptatif, sur desktop une sidebar gauche pour la navigation avec top bar pour les actions user, sur mobile une bottom bar qui remplace la sidebar (pattern pouce facile à atteindre, standard des apps mobiles modernes). Un layout dédié `AuthLayout` sert les pages non authentifiées (`/login`, `/register`) sans NavBar.

Toutes les pages et composants suivent le pattern code-behind (`.razor` pour le template, `.razor.cs` en partial class pour le C#), avec injection via `[Inject]` et `= default!;` pour supprimer les warnings nullable (46 occurrences dans 17 fichiers). Le formulaire de recette utilise un `RecipeFormModel` dédié (découplé des DTOs API) et le composant `RecipeForm` est réutilisé dans 3 contextes (scan, création manuelle, édition).

**Historique des décisions**

MudBlazor a été retenu en mars 2026 ([DEC-010](ADR.md#dec-010)) comme librairie UI native Blazor, préférée à Bootstrap ou Tailwind car elle propose des composants en C# pur (zéro JS à écrire) avec un thème centralisé et une gestion responsive intégrée. Le layout adaptatif sidebar desktop + bottom bar mobile a été décidé dans la foulée ([DEC-016](ADR.md#dec-016)) pour offrir une UX mobile-first sans sacrifier l'espace desktop.

Le découplage `RecipeFormModel` séparé des DTOs API ([DEC-018](ADR.md#dec-018)) est venu du besoin de réutiliser le même composant `RecipeForm` dans 3 pages avec des DTOs différents (`RecipeCreateDto` pour scan et création manuelle, `RecipeUpdateDto` pour l'édition). Le parent décide du verbe HTTP, pas le formulaire. En parallèle, le code-behind pattern ([DEC-019](ADR.md#dec-019)) a été appliqué à toutes les pages et composants, avec un layout dédié `AuthLayout` extrait pour les pages non authentifiées.

En juillet 2026 ([DEC-041](ADR.md#dec-041)), un compromis pragmatique a été acté sur `MainLayout.razor`. Le code C# a été extrait en code-behind comme le reste, mais le bloc `<style>` inline a été conservé (au lieu d'un `.razor.css` scoped). Raison, la classe CSS cible un sous-composant MudBlazor (`MudMainContent`) que le scoping Blazor ne peut pas atteindre sans `::deep`, dont le coût cognitif dépasse le bénéfice pour ce cas isolé.

**DEC détaillées** : [DEC-010](ADR.md#dec-010), [DEC-016](ADR.md#dec-016), [DEC-018](ADR.md#dec-018), [DEC-019](ADR.md#dec-019), [DEC-041](ADR.md#dec-041).

---

## Authentification, sécurité et RGPD

**État actuel**

L'authentification utilise un token JWT stateless (aucune session côté serveur, cible client web Blazor et mobile MAUI) transporté dans un cookie `HttpOnly + Secure + SameSite=Strict`. Le mot de passe est hashé avec `PasswordHasher<T>` de Microsoft.AspNetCore.Identity (PBKDF2 avec 100 000 itérations et salt intégré), avec migration douce automatique depuis l'ancien hash HMAC-SHA512 au premier login des utilisateurs existants.

Le serveur applique 6 headers de sécurité via un `SecurityHeadersMiddleware` custom (X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, CSP adapté à Blazor WASM et MudBlazor, HSTS conditionnel prod), plus une politique CORS externalisée dans `appsettings.json` avec fail-fast au démarrage si absente. Kestrel est hardened au démarrage (`MaxRequestBodySize = 15 Mo` pour plafonner les uploads au niveau transport, `AddServerHeader = false` pour l'anti-fingerprinting OWASP). Un `ForwardedHeaders` middleware en tête de pipeline propage l'IP réelle du client et le schéma HTTPS depuis le reverse proxy edge, ce qui permet au rate limiter par IP de fonctionner correctement.

Le rate limiting est double couche, natif ASP.NET Core par IP (100 par minute global, 10 pour auth, 5 pour scan) plus un compteur custom par email dans `AuthService` (blocage 15 minutes après 5 échecs). Pas de token anti-CSRF, protection assurée par la combinaison `SameSite=Strict` et CORS whitelist stricte.

Côté RGPD, la suppression de compte utilisateur (Art. 17) fonctionne en soft delete avec 30 jours de grâce, via une colonne nullable timestamp `User.DeleteRequestedAt`. Deux mécanismes de purge coexistent, un check au login (si l'utilisateur revient après J+30, purge immédiate) et un cron `BackgroundService` en arrière-plan qui purge toutes les 24h les comptes expirés. La purge physique déclenche une cascade delete Postgres qui supprime en une transaction toutes les entités liées (recettes, ingrédients, étapes, favoris, commentaires), garantissant une conformité Art. 17 stricte sans données orphelines.

**Historique des décisions**

Le principe JWT stateless a été acté dès novembre 2025 ([DEC-005](ADR.md#dec-005), aujourd'hui superseded par DEC-014 sur le transport). En mars 2026, l'accès protégé aux ressources a été renforcé, `GetByIdAsync` vérifie systématiquement `IsPublic + UserId` ([DEC-008](ADR.md#dec-008)) pour empêcher qu'un utilisateur voie la recette privée d'un autre. Puis le stockage du JWT est passé de `localStorage` (vulnérable XSS) à un cookie HttpOnly + Secure + SameSite=Strict ([DEC-014](ADR.md#dec-014)), avec côté frontend un `CookieAuthStateProvider` qui interroge `api/auth/me` pour connaître l'état d'auth ([DEC-015](ADR.md#dec-015)) puisque le token n'est plus lisible par le JavaScript.

En avril 2026, le hashing des mots de passe a été migré de HMAC-SHA512 vers PBKDF2 ([DEC-020](ADR.md#dec-020)) avec migration douce automatique. Dans le même mois, trois blocs de sécurité ont été livrés ensemble, `SecurityHeadersMiddleware` custom ([DEC-021](ADR.md#dec-021)) préféré à un package tiers (contrôle total, ~20 lignes), rate limiting double couche IP et per-account ([DEC-022](ADR.md#dec-022)) pour bloquer aussi bien les floods d'IP que le credential stuffing distribué, et CORS dynamique via appsettings avec fail-fast ([DEC-023](ADR.md#dec-023)).

En mai 2026, une décision consciente d'absence a été formalisée, pas de token anti-CSRF ([DEC-024](ADR.md#dec-024)), car `SameSite=Strict` sur le cookie plus CORS whitelist strict couvrent l'attaque par deux barrières indépendantes.

En juin 2026, la conformité RGPD Art. 17 a été mise en place ([DEC-037](ADR.md#dec-037)) en 2 temps. Phase 1 (BACK-005) livre le soft delete avec délai de grâce 30 jours et login-check (couverture 80% des cas), reposant sur un schéma de soft-delete par colonne nullable timestamp `User.DeleteRequestedAt` couplé à une cascade delete Postgres ([DEC-058](ADR.md#dec-058)) qui garantit l'atomicité de la purge physique. Phase 2 (BACK-077, fin juillet 2026) ajoute le cron auto `AccountPurgeService` pour couvrir les 20% restants (les comptes fantômes qui ne reviennent jamais). Le cron a été déployé APRÈS avoir les 3 filets de sécurité en place (backup Postgres BACK-078, Serilog structuré BACK-010, alerting Telegram BACK-079), selon le principe SRE "Observability before features". En parallèle en juin 2026, la valeur par défaut de `Recipe.IsPublic` a été passée de `true` à `false` ([DEC-054](ADR.md#dec-054)) pour appliquer Privacy by Design (RGPD Art. 25), toute nouvelle recette est privée par défaut.

Le triptyque `Upload defense-in-depth` (extension + MIME + magic bytes) au niveau contrôleur ([DEC-057](ADR.md#dec-057)) protège l'endpoint scan contre les uploads malicieux, complété par le hardening Kestrel ([DEC-056](ADR.md#dec-056)) qui plafonne les uploads au niveau transport (15 Mo) et retire le header `Server` pour anti-fingerprinting.

En juillet 2026, le pipeline API a été enrichi pour supporter le déploiement production derrière un reverse proxy edge, `ForwardedHeaders` middleware en tête ([DEC-055](ADR.md#dec-055)) pour propager l'IP client réelle et le schéma HTTPS.

**DEC détaillées** : [DEC-005](ADR.md#dec-005), [DEC-008](ADR.md#dec-008), [DEC-014](ADR.md#dec-014), [DEC-015](ADR.md#dec-015), [DEC-020](ADR.md#dec-020), [DEC-021](ADR.md#dec-021), [DEC-022](ADR.md#dec-022), [DEC-023](ADR.md#dec-023), [DEC-024](ADR.md#dec-024), [DEC-037](ADR.md#dec-037), [DEC-054](ADR.md#dec-054), [DEC-055](ADR.md#dec-055), [DEC-056](ADR.md#dec-056), [DEC-057](ADR.md#dec-057), [DEC-058](ADR.md#dec-058).

---

## Base de données et backup

**État actuel**

La base de données de production est PostgreSQL 16 (image `postgres:16-alpine`), avec un mix de colonnes relationnelles classiques et de colonnes JSONB pour les données semi-structurées (`IngredientNutrition.AllergensJson`, `OCRExtraction.JsonData`, `RecipeSource.MetadataJson`). Un warning cosmétique de collation apparaît en dev depuis le passage à Alpine, tracé comme dette technique mais sans impact fonctionnel (aucun tri textuel sensible à la locale dans le code actuel).

Le schéma applique deux choix RGPD structurants côté données, la colonne `Recipe.IsPublic` avec un défaut à `false` (privacy by design RGPD Art. 25), et un pattern de suppression de compte utilisateur en 2 temps, soft-delete via colonne nullable timestamp puis purge physique cascade delete pilotée par le contrat FK Postgres. Ces choix sont détaillés dans la section Authentification, sécurité et RGPD.

Le backup PostgreSQL est chiffré avec GPG asymétrique (clé publique sur le VPS, clé privée hors serveur) et suit la règle 3-2-1 (3 copies, 2 supports, 1 hors-site). Un container dédié `backup` dans `docker-compose.prod.yml` exécute quotidiennement pendant les heures creuses un `pg_dump` custom qui produit un fichier `.dump.gpg` chiffré, conservé 30 jours en local avec rotation automatique. La procédure de restore complète est documentée et testée end-to-end.

**Historique des décisions**

Le choix Postgres avec colonnes JSONB a été acté dès novembre 2025 ([DEC-004](ADR.md#dec-004)) pour éviter de créer des tables dédiées à des données très variables (nutrition, sortie OCR brute, metadata source recette). Postgres gère nativement le JSON avec indexation, ce qui simplifie le schéma sans sacrifier la performance.

En juin 2026, lors du passage de l'image dev Postgres de Debian à Alpine ([DEC-034](ADR.md#dec-034)), un warning de collation est apparu (changement de provider glibc vers musl). Le fix propre nécessite une procédure `pg_dump + dropdb + createdb + pg_restore`, disproportionnée pour un warning cosmétique. Décision de reporter le fix dans un ticket dédié (BACK-068) à traiter quand la première feature exploitant un tri textuel arrivera (BACK-029 recherche et filtres). Le même mois, deux évolutions RGPD ont impacté le schéma, le default `Recipe.IsPublic = false` ([DEC-054](ADR.md#dec-054)) via migration `SetRecipeIsPublicDefaultFalse` et le pattern soft-delete + cascade delete ([DEC-058](ADR.md#dec-058)) via migration `AddDeleteRequestedAtToUser`.

En juillet 2026, la stratégie de backup PostgreSQL a été formalisée ([DEC-038](ADR.md#dec-038)) autour de 4 axes, format `pg_dump` custom (compression native, restore sélectif), chiffrement GPG asymétrique (évite le paradoxe "clé co-localisée avec le backup"), règle 3-2-1 pour la résilience physique, et découpage BACK-078 en 2 parties. La partie 1 (backup local chiffré + cron + rétention 30 jours) a été livrée immédiatement. La partie 2 (automatisation off-site) a été repriorisée fin juillet, pour V1 l'off-site est opérateur-managed sur médium physique séparé (satisfait la règle 3-2-1 et RGPD Art. 32 pour le volume V1), l'automatisation via object storage est tracée pour V1.1.

**DEC détaillées** : [DEC-004](ADR.md#dec-004), [DEC-034](ADR.md#dec-034), [DEC-038](ADR.md#dec-038), [DEC-054](ADR.md#dec-054), [DEC-058](ADR.md#dec-058).

---

## IA, LLM et scan

**État actuel**

Le provider IA par défaut pour le scan de recettes est **Mistral Vision** (hébergement UE, RGPD-natif, Experiment tier gratuit sans carte bancaire), sélectionné via la variable d'environnement `AI_PROVIDER=MistralVision`. Le modèle multimodal analyse directement l'image envoyée par l'utilisateur, sans étape OCR intermédiaire, et retourne un JSON structuré (titre, description, portions, temps de préparation et cuisson, difficulté, ingrédients avec name+quantity+unit, étapes ordonnées).

L'architecture repose sur un Factory Pattern qui permet de switcher entre 6 providers (Fake, Mistral, Gemini, Groq text-only, MistralVision, GeminiVision) en changeant une seule variable d'environnement. Deux pipelines cohabitent, `VisionRecipePipeline` pour les providers multimodaux et `RecipePipeline` classique (OCR Tesseract + text-only LLM) pour les providers text-only. Cette double architecture permet de basculer rapidement en cas de panne ou de contrainte réglementaire.

L'endpoint scan est protégé par un triptyque de validation upload au niveau contrôleur (whitelist extension, MIME type, magic bytes JPEG et PNG) qui bloque les fichiers non conformes avant tout appel LLM. Le scan lui-même est ensuite protégé par 4 couches de défense en profondeur (défense OWASP LLM Top 10), un `PromptSanitizer` avec 10 patterns regex sur le texte OCR avant envoi au LLM (path text-only), un `AiRateLimiter` LLM-level à 4 tiers cumulatifs (per-user-hour et day, per-ip-hour, global-minute), un `AiAuditLogger` structuré Serilog sans PII avec input hash SHA256, et une propagation uniforme des tokens consommés cross-provider via un record `LlmCompletionResult`. Un `AiCostCounter` par provider trace les tokens consommés sur deux fenêtres (daily reset UTC minuit, weekly reset dimanche 23:59 UTC) et déclenche des alertes Telegram déboncées à l'atteinte des seuils configurés dans `appsettings.json`.

Un quota BDD limite les recettes à 200 par utilisateur (`RecipeLimits.MaxPerUser`). Le check quota est effectué AVANT l'appel LLM sur le path scan (fail-fast économique, réponse en environ 130 ms au lieu de 8 à 10 secondes d'attente LLM inutile pour un utilisateur déjà au quota). Le format WebP est bloqué au niveau du contrôleur API dans tous les cas, y compris sur le path Vision qui l'accepterait techniquement, un ticket post V1 est prévu pour l'assouplissement end-to-end.

**Historique des décisions**

Dès mars 2026 ([DEC-017](ADR.md#dec-017)), il a été acté que le Frontend passe systématiquement par l'API (jamais d'appel direct au service IA), pour centraliser la sécurité, l'audit et la traçabilité RGPD. En mai 2026 ([DEC-025](ADR.md#dec-025)), le support WebP a été retiré (Tesseract Windows sans libwebp), décision qui reste active aujourd'hui pour le path OCR fallback comme pour le path Vision (blocage au contrôleur API).

En mai puis juin 2026, le triptyque de validation upload extension + MIME + magic bytes ([DEC-057](ADR.md#dec-057)) a été mis en place au niveau contrôleur pour protéger l'endpoint scan contre les fichiers non conformes avant tout appel LLM.

En juin 2026, le pattern Factory Pattern via `AI_PROVIDER` env var a été mis en place ([DEC-035](ADR.md#dec-035)), et Groq (Llama 3.3 70B) a été retenu comme provider text-only par défaut pour la stratégie freemium ([DEC-036](ADR.md#dec-036), aujourd'hui partiellement superseded).

En juillet et août 2026, plusieurs pivots successifs ont fait converger l'architecture vers son état actuel :

- **Fin juillet 2026** : sécurisation IA multi-couches livrée en 3 sous-livraisons US-A2-04 a/b/c ([DEC-047](ADR.md#dec-047)), avec le catalogue OWASP LLM01 PromptSanitizer, le rate limiter 4 tiers, l'audit trail structuré sans PII, et la propagation tokens cross-provider.
- **Début août 2026** : alerting coûts LLM per-provider ([DEC-048](ADR.md#dec-048)) via `AiCostCounter` avec debounce.
- **09 août 2026** : pivot majeur du provider par défaut ([DEC-043](ADR.md#dec-043)) vers un Vision LLM multimodal (Google Gemini initialement), suite à un test qualité qui a révélé un écart d'environ 4× entre le pipeline OCR + text-only (~25% de qualité) et un Vision LLM direct (~95%) sur les photos à mise en page complexe. Cette décision supersede DEC-040 (MVP V1 sans scan IA, décidé le 19/07) qui devient caduque.
- **10 août 2026** : extension de `AI_PROVIDER` avec `GeminiVision` comme cinquième valeur ([DEC-044](ADR.md#dec-044)), plutôt que d'introduire une seconde variable d'environnement dédiée.
- **10 août 2026** : re-pivot ([DEC-045](ADR.md#dec-045)) vers Mistral Vision au lieu de Gemini, suite à la découverte que Gemini AI Studio impose un prepayment incompatible avec la posture "zéro coût fournisseur cloud" du projet. Mistral Vision offre les mêmes bénéfices multimodaux avec en plus l'hébergement UE et un free tier sans carte bancaire.
- **11 août 2026** : implémentation `MistralVisionCompletionClient` (US-A2-12) avec test qualité photo magazine ~85% validé (au-dessus du seuil 60% déclencheur du fallback GroqVision, donc US-A2-13 non-déclenchée, `GroqVisionCompletionClient` n'existe pas dans le codebase).
- **10-11 août 2026** : formalisation de l'architecture pipeline Vision ([DEC-046](ADR.md#dec-046)) avec deux ports parallèles `IChatCompletionClient` et `IVisionCompletionClient`.
- **16 août 2026** : structuration end-to-end des ingrédients ([DEC-050](ADR.md#dec-050)) en 3 champs `Name` + `Quantity` + `Unit` (au lieu d'une chaîne libre à parser), propagée à travers worker IA, API, frontend Blazor et BDD. Ouvre la voie à des features produit comme la conversion de portions ou la génération de listes de courses agrégées.
- **17-20 août 2026** : quota recettes par utilisateur ([DEC-049](ADR.md#dec-049)) livré en 2 temps, quota BDD (US-A2-06) puis check pre-LLM (US-A2-15) pour éviter le gaspillage de tokens LLM sur des utilisateurs déjà au quota. Discriminant machine-readable `error: "recipe_limit_reached"` dans le body 403 pour distinguer ce cas des autres 403.

**DEC détaillées** : [DEC-017](ADR.md#dec-017), [DEC-025](ADR.md#dec-025), [DEC-035](ADR.md#dec-035), [DEC-036](ADR.md#dec-036), [DEC-040](ADR.md#dec-040), [DEC-043](ADR.md#dec-043), [DEC-044](ADR.md#dec-044), [DEC-045](ADR.md#dec-045), [DEC-046](ADR.md#dec-046), [DEC-047](ADR.md#dec-047), [DEC-048](ADR.md#dec-048), [DEC-049](ADR.md#dec-049), [DEC-050](ADR.md#dec-050), [DEC-057](ADR.md#dec-057).

---

## Infrastructure, Docker et déploiement

**État actuel**

Le déploiement production cible un VPS Linux dédié (offre d'entrée de gamme d'un hébergeur européen) avec Docker installé manuellement (accès root SSH standard). Le stack tourne via `docker-compose.prod.yml` qui orchestre 4 services isolés dans un réseau interne, PostgreSQL, l'API, le Frontend nginx, et un container de backup. Aucun service applicatif n'est exposé publiquement en dehors du nginx qui sert le Frontend sur le port 8080 (à mapper derrière un reverse proxy edge Apache ou nginx du host avec HTTPS).

L'image API est générée via le Container Support natif intégré au SDK .NET (properties MSBuild dans le `.csproj`, plus de Dockerfile manuel), poussée sur GitHub Container Registry (GHCR) taguée en version sémantique. L'image Frontend utilise un Dockerfile custom avec `nginx:alpine` (~40 MB au lieu de ~150 MB avec un runtime aspnet), et nginx fait office de reverse proxy vers l'API en interne au réseau Docker (Option B, zéro CORS exposé publiquement).

La sécurité compose est baseline solide (isolation réseau, `security_opt: no-new-privileges`, `mem_limit` et `cpus`, healthchecks avec `depends_on: service_healthy`), enrichie fin juillet 2026 par l'adoption du mécanisme Docker Secrets natif (`secrets:` top-level dans le compose, `AddKeyPerFile("/run/secrets")` côté API) qui remplace le pattern env vars pour les variables sensibles (POSTGRES_PASSWORD, JWT Secret, ConnectionString, OcrScan BaseUrl, Telegram BotToken et ChatId). Le composant IA (Azure Function .NET 8 avec dépendances natives Tesseract) reste un service externe hébergé sur une plateforme serverless facturée à l'usage, pas conteneurisé sur le VPS.

Le pipeline CI/CD GitHub Actions (workflow `.github/workflows/ci.yml`) exécute 7 jobs en parallèle sur push et PR (build+tests API, IA et Web, vuln-audit avec fail-fast sur High et Critical, CodeQL SAST sur C# et actions, Lighthouse a11y audit local Docker) plus un job conditionnel `build-and-push` sur tag `v*` qui pousse les images sur GHCR.

La branche `main` est protégée par une Branch Protection Rule Classic stricte (activée le 26/08/2026) : 8 status checks required (`test-api`, `test-ia`, `build-web`, `vuln-audit`, `lighthouse-a11y`, `Code scanning results / CodeQL`, `Analyze (actions)`, `Analyze (csharp)`) + `Require branches to be up to date` (force rebase avant merge) + `Do not allow bypassing` activé (même l'owner du repo ne peut pas bypasser) + force push et deletion bloqués. `Require approvals` volontairement décoché (solo dev = GitHub interdit self-approval, à réactiver en team ou avec review IA).

**Historique des décisions**

En mai 2026, le Frontend Blazor WASM a adopté `nginx:alpine` ([DEC-027](ADR.md#dec-027)) au lieu d'un runtime aspnet (économie ~110 MB par image, plus pertinent pour du statique WASM), avec dans la foulée le choix d'un reverse proxy nginx Option B ([DEC-028](ADR.md#dec-028)) où le nginx du Frontend proxifie `/api/*` vers l'API en interne, plutôt que d'exposer l'API sur un sous-domaine avec CORS. Zéro CORS en prod, un seul certificat HTTPS à gérer, same-origin natif pour le cookie SameSite=Strict.

Fin mai 2026 ([DEC-029](ADR.md#dec-029)), une baseline security compose a été formalisée avec phasage volontaire du hardening avancé, la baseline (isolation réseau, no-new-privileges, mem_limit, healthchecks) était livrable immédiatement, les mesures avancées (read_only filesystems, cap_drop, user non-root explicite) tracées dans BACK-056 pour itération. Le trade-off "pas de Docker secrets natifs" acté à cette époque a été superseded fin juillet 2026 par l'adoption du mécanisme `secrets:` natif ([DEC-052](ADR.md#dec-052)) via BACK-004.

En juin 2026, trois décisions ont modernisé le pipeline de build et distribution :
- Container Support natif SDK .NET pour l'API ([DEC-030](ADR.md#dec-030)) au lieu d'un Dockerfile manuel, suggéré par le mentor, ~5 lignes XML dans le .csproj remplacent ~30 lignes de Dockerfile multi-stage.
- Distribution des images via GHCR ([DEC-031](ADR.md#dec-031)), build sur le poste dev, push tagué en version sémantique, pull depuis le VPS. Rollback en 30 secondes via `docker compose pull` sur un tag précédent.
- Adoption de .NET Aspire ([DEC-032](ADR.md#dec-032)) pour l'orchestration du stack dev + prod, décidée en visio mentor le 04/06 mais **jamais implémentée** (spike BACK-065 non déclenché sur Alpha.1 ni Alpha.2 pour cause de priorisation produit sur d'autres tickets P0). Statut aujourd'hui, envisagé V2.

En août 2026, deux décisions structurantes ont finalisé l'infrastructure de déploiement, l'hébergement production sur VPS Linux dédié européen ([DEC-042](ADR.md#dec-042)) et le pipeline CI/CD GitHub Actions ([DEC-053](ADR.md#dec-053)) en 7 jobs parallèles avec CodeQL SAST et Lighthouse a11y, plus un job de release conditionnel sur tag `v*` pour publier sur GHCR.

Fin août 2026, la décision d'un environnement dev containerisé via DevContainer VSCode plus service IA dans le compose dev ([DEC-061](ADR.md#dec-061)) a été formalisée pour éliminer les divergences dev/prod silencieuses observées en Alpha.2 (typiquement Tesseract avec libwebp en prod contre sans libwebp en local Windows, faisant crasher le path OCR fallback sur les images WebP). Application prévue en US-B1-20, première tâche du sprint Alpha.3.

Le 26/08/2026 ([DEC-062](ADR.md#dec-062)), suite à la découverte d'un trou de sécurité gouvernance (aucune Branch Protection Rule active sur `main` depuis la création du repo → merge sans CI verte possible), activation d'une Branch Protection Rule Classic stricte : 8 status checks required incluant les 5 jobs `ci.yml` + les 3 jobs CodeQL (belt-and-suspenders : check aggregé Advanced Security + les 2 workflow jobs `Analyze` pour couvrir le cas où le workflow YAML lui-même plante), `Do not allow bypassing` activé sur l'owner, `Require approvals` décoché par contrainte solo dev (GitHub interdit self-approval sur ses propres PRs). Classic préféré à Rulesets pour la simplicité d'un setup V1, migration Rulesets envisageable en V2.

**DEC détaillées** : [DEC-027](ADR.md#dec-027), [DEC-028](ADR.md#dec-028), [DEC-029](ADR.md#dec-029), [DEC-030](ADR.md#dec-030), [DEC-031](ADR.md#dec-031), [DEC-032](ADR.md#dec-032), [DEC-042](ADR.md#dec-042), [DEC-052](ADR.md#dec-052), [DEC-053](ADR.md#dec-053), [DEC-061](ADR.md#dec-061), [DEC-062](ADR.md#dec-062).

---

## Tests

**État actuel**

La stratégie de test repose sur deux niveaux complémentaires. Les tests unitaires utilisent xUnit avec des `Fake*Repository` implémentés avec `List<T>` en mémoire (déterministes, rapides sous 1 seconde, sans base de données ni Docker). Les tests d'intégration passent par un `CustomWebApplicationFactory` qui lance un container `postgres:16-alpine` réel via TestContainers, avec application des migrations EF Core via `Database.MigrateAsync()` (pas `EnsureCreated()`, pour valider le vrai chemin de migration prod).

Le pattern d'isolation retenu est "un container par classe de tests" via `IClassFixture<CustomWebApplicationFactory<Program>>` (16 classes de tests d'intégration branchées). Des factories spécialisées héritent de la factory de base quand il faut modifier une seule dimension de config (`NoRateLimitApplicationFactory` pour désactiver le rate limiter, `LowQuotaWebApplicationFactory` pour override `MaxPerUser=2`), sans polluer la factory principale. Les fakes réutilisés entre plusieurs projets tests vivent dans un projet transverse dédié `MemoRecipe.Tests.Shared`.

**Historique des décisions**

Dès mars 2026 ([DEC-009](ADR.md#dec-009)), le pattern FakeRepository a été retenu pour les tests unitaires, cohérent avec la philosophie "tests déterministes rapides sans dépendance externe" du projet. Le choix xUnit comme framework de test est implicite depuis le début du projet.

En juin 2026, un audit a révélé deux divergences silencieuses entre les tests d'intégration SQLite in-memory et Postgres prod, JSONB traduit en TEXT (bloquant dès la première query `@>`, `?`, `->>`) et TIMESTAMPTZ perdu (précision, DateTime.Kind). La migration vers TestContainers ([DEC-033](ADR.md#dec-033)) a été actée avec le mentor et implémentée dans BACK-062 (13/06/2026). Aujourd'hui 18 tests d'intégration passent contre un vrai Postgres, aucune divergence schéma silencieuse.

Fin juillet et mi-août 2026, deux patterns tests complémentaires ont été formalisés ([DEC-059](ADR.md#dec-059)), le projet transverse `MemoRecipe.Tests.Shared` pour les fakes réutilisés entre projets (rule of three), et le pattern de factories test spécialisées par héritage (`NoRateLimitApplicationFactory` livré en BACK-080, `LowQuotaWebApplicationFactory` livré en US-A2-06) pour isoler les configurations spécifiques sans polluer la factory de base.

**DEC détaillées** : [DEC-009](ADR.md#dec-009), [DEC-033](ADR.md#dec-033), [DEC-059](ADR.md#dec-059).

---

## Framework et patterns backend

**État actuel**

L'API valide les DTOs entrants avec FluentValidation (5 validators actifs, `RecipeCreateDto`, `RecipeUpdateDto`, `RegisterDto`, `LoginDto`, `DeleteAccountDto`), les règles vivent dans une classe séparée du DTO (SRP) et sont testables unitairement. La gestion d'erreur passe par un `ExceptionMiddleware` global qui garantit qu'aucune stack trace ne fuite en production, avec des catches spécifiques pour les exceptions métier typées (`AccountMarkedForDeletionException` renvoie 403, `AiRateLimitExceededException` renvoie 429 avec header `Retry-After`, `RecipeLimitReachedException` renvoie 403 avec discriminant JSON `error: "recipe_limit_reached"`). Le pattern d'exceptions métier typées est appliqué systématiquement pour toute nouvelle erreur métier, avec des classes symétriques côté frontend Blazor pour un catch discriminant côté client.

Le mapping DTO ↔ entités utilise Mapperly (source generator, OSS MIT), avec 5 mappers statiques (`UserMapper`, `RecipeMapper`, `IngredientMapper`, `StepMapper`, `CategoryMapper`) définis en `static partial class` avec attribut `[Mapper]`. Zéro reflection runtime, erreurs de typo détectées à la compilation, perf 30 à 50 fois supérieure à AutoMapper.

Le logging est structuré via Serilog (sinks Console et File avec rotation quotidienne et rétention 30 jours), avec masquage systématique des données personnelles dans les logs (`EmailMasker`, `ValidationErrorSanitizer`) pour respecter RGPD Art. 5.1.c. L'alerting critique (opérations destructives massives, erreurs 500, dépassements de seuils coûts LLM, échecs backup) passe par une abstraction `INotificationChannel` implémentée par un `TelegramNotificationChannel`. Le service métier `AlertingService` reste agnostique du canal réel, ce qui permet de swapper Telegram vers Slack, Discord ou email en changeant une seule ligne dans `Program.cs`.

Un `FakeAuthService` reste présent dans le codebase Frontend comme option dev offline, permettant de développer et tester l'UX sans dépendre du backend (swap d'une ligne dans `Program.cs`). Le wire par défaut reste le vrai `AuthService` HTTP.

**Historique des décisions**

Trois décisions cadres ont été prises en mars 2026, FluentValidation ([DEC-011](ADR.md#dec-011)) préféré à Data Annotations (règles dans classes séparées, testables via `TestValidate`), un `ExceptionMiddleware` global custom ([DEC-012](ADR.md#dec-012)) préféré au handler par défaut d'ASP.NET (contrôle total sur la réponse d'erreur), et un `FakeAuthService` pour développer le Frontend sans API ([DEC-013](ADR.md#dec-013)). Ce fake est toujours maintenu au contrat `IAuthService` (dernière méthode ajoutée `RequestAccountDeletionAsync` post BACK-005 juin 2026).

En mai 2026, AutoMapper a été remplacé par Mapperly ([DEC-026](ADR.md#dec-026)) suite au passage d'AutoMapper sous licence commerciale Lucky Penny. Migration mécanique (5 profiles convertis, 2 services simplifiés), gains significatifs (warning licence supprimé, perf 30 à 50 fois supérieure, erreurs typo détectées au build au lieu du runtime, découverte des source generators .NET).

En juillet 2026, l'alerting critique a été formalisé ([DEC-039](ADR.md#dec-039)) autour de Telegram Bot API comme canal instantané par défaut (setup ~5 min via `@BotFather`, aucun SDK, gratuit sans quota mensuel, notification push mobile) et d'une abstraction `INotificationChannel` (pattern Ports/Adapters) pour permettre de swap vers un autre canal sans toucher au code métier. Dans le même mois, le logging structuré Serilog a été mis en place ([DEC-051](ADR.md#dec-051)) avec sinks Console et File, masquage PII systématique via `EmailMasker` et `ValidationErrorSanitizer`, enrichers `FromLogContext` et `WithMachineName`.

Depuis l'été 2026, l'`ExceptionMiddleware` a été enrichi de 3 catches spécifiques (au fil de l'ajout des exceptions métier typées) et branché à `IAlertingService.NotifyServerErrorAsync()` pour alerter Telegram sur les 500 non captés (skip `/health`). Le pattern d'exceptions métier typées a été formalisé rétrospectivement ([DEC-060](ADR.md#dec-060)) comme extension de DEC-012, chaque erreur métier a sa propre classe d'exception avec les données contextuelles nécessaires, et un catch spécifique dans le middleware garantit une réponse HTTP cohérente au client.

**DEC détaillées** : [DEC-011](ADR.md#dec-011), [DEC-012](ADR.md#dec-012), [DEC-013](ADR.md#dec-013), [DEC-026](ADR.md#dec-026), [DEC-039](ADR.md#dec-039), [DEC-051](ADR.md#dec-051), [DEC-060](ADR.md#dec-060).
