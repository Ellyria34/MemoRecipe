# Convention User Story Backlog

## Objectif
Specs techniques exécutables. Un dev doit savoir en 5 min : contexte, quoi faire, comment valider, dépendances.
Chronologie et commits → journal-sprint. Backlog = spécification, pas historique.

## Sources de vérité (à partir d'Alpha.3)

| Élément | Emplacement | Rôle |
|---|---|---|
| Contenu des US | **Issues GitHub** du repo `Ellyria34/MemoRecipe` | Spec technique de chaque US (source unique) |
| Dashboard sprint | **Project GitHub `MemoRecipe Roadmap`** (onglet Projects du repo) | Board Kanban, custom fields, filtres, milestones |
| Template d'Issue | `.github/ISSUE_TEMPLATE/user-story.yml` | Formulaire auto proposé au clic "New issue" |
| Convention structurelle | Ce document | Règles rédactionnelles + critères tests + workflow |
| Fichier `documentation/Backlog_V1-<Sprint>.md` | Optionnel | Index compact référençant les issues du sprint (avec liens vers `#NN`) |

Custom fields du Project à renseigner sur chaque Issue ajoutée :
- **Sprint** : Backlog / Alpha.3 / Alpha.4 / Beta.1 / Beta.2 / V1-Stable / V1.1
- **Priority** : P0 / P1 / P2 / P3
- **Nature** : Feature / Bug fix / Refacto / Chore / Doc
- **Estimation (h)** : temps prévu, TDD strict inclus
- **Real (h)** : temps réel passé, rempli au merge PR
- **Milestone** (built-in) : `v1.0.0-<sprint>` (permet tracking auto X/Y closed)

## Nomenclature US (à partir du 25/08/2026)

**Format** : `US-<N>: [Verbe d'action + objet]` où `<N>` est un compteur séquentiel MemoRecipe, sans préfixe sprint.

**Numérotation** :
- Compteur unique séquentiel `US-01`, `US-02`, ... incrémenté à la création
- Aucun préfixe sprint (`A1`, `A2`, `B1`, `V1`) dans le nom
- Le sprint est tracké UNIQUEMENT via le custom field `Sprint` du Project + le Milestone GitHub
- Le numéro `US-<N>` peut être différent du numéro GitHub Issue (`#<M>`) : deux identifiants différents pour deux rôles différents

**Historique** :
- US antérieures au 25/08/2026 avec préfixe `US-A1-XX`, `US-A2-XX`, `US-B1-XX` : conservées telles quelles (déjà closed / mergées avant le changement de convention)
- Exception : US-B1-20 (Issue #71) reste `US-B1-20` (closed le 24/08/2026 juste avant le changement de convention)
- US ouvertes à la date du changement : renommées vers le nouveau format (drop du préfixe sprint)

**Cross-références** :
- Dans les Issues, PRs et commits : préférer le numéro GitHub `#<M>` (auto-linking cliquable, évite le désync)
- Dans la documentation technique (ADR, README, fiches) : utiliser le nom US complet `US-<N>` pour la lisibilité

## Séparation métadonnées vs contenu (single source of truth)

**Métadonnées** (Sprint, Priority, Nature, Estimation (h), Real (h), Status, Milestone) → **uniquement dans les Custom Fields du Project MemoRecipe Roadmap**. Jamais dupliqué dans le body de l'Issue (risque de désync).

**Contenu spécifique de la spec** (Contexte, Description, Tests reproduction, Tâches, Critères validation, Dépendances, Notes) → **uniquement dans le body de l'Issue** via le formulaire du template YAML.

**Statut** de l'US → géré côté project via la colonne (Backlog / To Do / In Progress / Review / Done). Le body de l'Issue ne mentionne PAS de statut.

## Template US (contenu de l'Issue uniquement)

Le formulaire GitHub (`.github/ISSUE_TEMPLATE/user-story.yml`) applique automatiquement cette structure. Le template ci-dessous est la référence rédactionnelle équivalente pour un rendu markdown (index optionnel `Backlog_V1-<Sprint>.md`).

```markdown
### US-XX : [Verbe d'action + objet]

**Contexte** :
[POURQUOI — 3-5 phrases. Problème, valeur métier, risque évité. Pas de solution ici.]

**Description** :
[QUOI — 3-6 phrases. Approche, composants touchés, alternatives écartées. Renvoi ADR si structurante.]

**Tests de reproduction** (Bug fix uniquement, sinon omis) :
1. [Étape fonctionnelle pour observer le bug]
2. [Résultat attendu vs comportement observé]
3. [Source signalement : audit, feedback beta, incident]

**Tâches** :
- [ ] [Verbe + composant + contexte court si non-évident]

**Critères de validation** :

*Fonctionnels* :
- [ ] [Comportement observable côté user]
- [ ] [Comportement API observable via curl/Postman]

*Tests automatisés* (viser tous niveaux applicables) :
- [ ] Unit : nominal + invalide + limite (3 cas min)
- [ ] Intégration TestContainers : end-to-end BDD réelle + assertions état persisté
- [ ] E2E Playwright : parcours UI complet + 1 cas d'erreur user-visible

*Sécurité* :
- [ ] Aucun secret ni PII exposé dans logs/API responses/messages d'erreur
- [ ] Aucune régression protections existantes (rate limit, ownership, CSRF, XSS)

*Documentation* :
- [ ] Doc mise à jour si applicable : ADR / DECISIONS / README / DEPLOYMENT / cheatsheet

**Dépendances** :
- [US-XX DONE] ou [#<GitHub#> DONE] ou [ressource externe : infra, feature flag, secret]
```

## Règles

- Estimation > 4h + sous-problèmes indépendants → décomposer en sous-items P0-1, P0-2, etc.
- 1 branche + 1 test qui reproduit + 1 fix + 1 test qui passe + 1 commit atomique + 1 PR par item (workflow TDD strict)
- TDD strict OBLIGATOIRE pour Bug fix, RECOMMANDÉ pour Feature
- Case cochée = tâche/critère effectivement satisfait (pas "en cours")
- Statut 🟢 DONE uniquement quand toutes cases cochées + US mergée sur `main`
- Estimation qui dérape > 30% → ajouter ligne `**Estimation réelle** : Xh (+Yh vs prévu) — [cause 1 phrase]`
- Update à chaque fin de session : cases + statut + estimation réelle si dérape
- Zéro chronologie ni ref commit dans le backlog (ces infos vivent dans le journal-sprint)

## Critères de déclenchement des tests

| Niveau | Déclenchement |
|---|---|
| Unit | Logique métier, algo, transformation, validation, mapping |
| Intégration TestContainers | Repository, Service qui persiste/récupère, endpoint impactant BDD |
| E2E Playwright | Parcours UI (formulaire, navigation, dialogue, upload) |
| Sécurité | Toute US (vérif logs sans PII + no secret leak + no regression) |

## Sécurité (repo potentiellement public)

| Interdit | OK |
|---|---|
| Mot de passe, token, clé API, IP interne, port service | Nom d'endpoint public (`/api/health`) |
| Nom user réel, email, contact recruteur, identité testeur | Alias, rôle projet |
| Chemin absolu de secret | Chemin repo (`src/...`) |
| Payload d'attaque copy-paste dans tests de reproduction | Description fonctionnelle du bug |
| Chiffre financier perso | Ordre de grandeur |
| Fournisseur tiers non publié dans README/ADR | Fournisseur déjà cité |

## Anti-patterns

- US titre + statut sans contexte ni critères (non exécutable)
- US géante > 8h non décomposée avec sous-problèmes indépendants
- Critères vagues ("ça marche", "OK visuellement")
- Bug fix sans test de reproduction
- Feature sans aucun test automatisé listé
- Statut 🟢 DONE avec cases décochées (désync)
- Chronologie / ref commit dans le backlog (violation séparation avec journal-sprint)
- Duplication entre plusieurs US (extraire dans US commune ou ADR)
