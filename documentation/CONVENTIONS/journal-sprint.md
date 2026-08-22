# Convention Journal Sprint

## Objectif
Traçabilité chronologique quotidienne. Sert de daily du lendemain + post-mortem sprint.
Détails techniques → backlog. Journal = quoi + quand + qui + décisions + bloqueurs.

## Fichier
`journal/journal-sprint-<sprint>.md` — un seul fichier par sprint, à plat dans `journal/`.
(Les dossiers `journal/Alpha1/` et `journal/Alpha2/` sont conservés pour les journaux hebdomadaires historiques pré-convention.)

## Template

```markdown
# Journal Sprint <Nom> (DD/MM/AAAA → DD/MM/AAAA)

## Contexte
[Objectif produit, livrable cible, dépendance bloquante externe — 3-5 phrases]
Board : [MemoRecipe Roadmap](https://github.com/Ellyria34/MemoRecipe/projects) — milestone `v1.0.0-<sprint>`
Index optionnel : [Backlog_V1-<Sprint>.md](../documentation/Backlog_V1-<Sprint>.md)

---

## JJ/MM/AAAA (Jour)

**Fait** :
- **US-XX-YY** [item] → Issue #NN, branche `type/US-XX-YY-slug`, commit `abc123f`, PR #MM
- **Décision** : [choix]. **Pourquoi** : [1 ligne]. **Comment** : [1 ligne].

**À faire demain** :
- [Item avec ref US]

**Bloqueurs / résolutions** :
- ⚠️ [Bloqueur non-résolu]
- ✅ [Résolution non-triviale du jour]

---

## Checklist sprint

**Progression : X/Y items — Xh / Yh prévus**

- [x] **US-XX-01** [libellé] — 4h prévu / 3h30 réel — fait DD/MM
- [ ] **US-XX-02** [libellé] — 6h prévu
```

## Règles

- 1 section par date calendaire, jamais par session
- Case cochée = US mergée sur `main` (pas "en cours")
- Concis : 5-15 lignes par date, 25 max
- Commit → mentionner branche + hash 7 chars + PR # dès le merge
- Décision structurante → 1 ligne ici + détails dans ADR/DECISIONS
- Bloqueur trivial résolu en séance → ne pas mentionner
- "À faire demain" → écrit fin de dernière session du jour
- Checklist finale → update à chaque fin de session
- Bilan sprint (livrables + écart estimation + points forts/à améliorer) → ajouté au tag Git final

## Sécurité (repo potentiellement public)

| Interdit | OK |
|---|---|
| Mot de passe, token, clé API, IP interne, port service | Nom d'endpoint public (`/api/health`) |
| Nom user réel, email, contact recruteur, testeur | Alias projet, rôle ("dev alpha.4") |
| Chemin absolu de secret | Chemin repo (`documentation/...`) |
| Payload d'attaque exécutable | Description fonctionnelle du bug |
| Chiffre financier perso | Ordre de grandeur ("quelques centaines") |
| Fournisseur tiers non publié dans README/ADR | Fournisseur déjà cité (Mistral, Infomaniak, Groq…) |

Règle simple : si publiable sur LinkedIn portfolio → OK dans le journal.

## Anti-patterns

- Section par session au lieu de par date
- Détails d'implémentation dupliqués depuis le backlog
- Résolution triviale mentionnée en "Bloqueurs" (bruit)
- Checklist finale pas à jour
- Commit sans ref dans la section du jour
- Décision architecturale rédigée ici au lieu d'ADR
