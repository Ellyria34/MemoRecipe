# Release Process — MemoRecipe

This document describes the release cycle for the MemoRecipe project. Releases are triggered by Git tags following the [semver](https://semver.org/) specification.

## Version numbering

The project follows semantic versioning `MAJOR.MINOR.PATCH` with pre-release qualifiers:

| Tag pattern | Meaning | When to use | Audience |
|---|---|---|---|
| `v1.0.0-alpha.N` | Development in progress, features being added, breaking changes possible | Internal iteration, CI validation | Maintainer only |
| `v1.0.0-beta.N` | Features complete, seeking bug reports | Beta testing phase | Close testers (5-10 people) |
| `v1.0.0-rc.N` | Release candidate, considered stable, final validation | Optional pre-launch check | Broader testers (rarely used for solo projects) |
| `v1.0.0` | First stable public release (General Availability) | Public announcement (LinkedIn, Discord, etc.) | Public |
| `v1.0.1` | Patch — bugfix on stable release | After public V1.0.0 | Public |
| `v1.1.0` | Minor — new feature added on stable release | After public V1.0.0 | Public |
| `v2.0.0` | Major — breaking change on stable release | Rare, requires migration path | Public |

## How to release

Releases are fully automated via GitHub Actions. To release a new version:

```bash
# 1. Make sure main is clean and CI is green
git checkout main
git pull origin main

# 2. Create the tag locally
git tag v1.0.0-alpha.1

# 3. Push the tag to trigger the build-and-push workflow
git push origin v1.0.0-alpha.1
```

The `on: push: tags: ['v*']` trigger in `.github/workflows/ci.yml` will:

1. Match any tag starting with `v`
2. Execute the `build-and-push` job:
   - Login to GHCR via the native `GITHUB_TOKEN` (no PAT required)
   - Build the API image via `dotnet publish /t:PublishContainer` with `ContainerImageTag=<version>`
   - Build the Web image via `docker build`
   - Push both images to `ghcr.io/<owner>/memorecipe-api:<version>` and `ghcr.io/<owner>/memorecipe-web:<version>`
3. Images become available for pull from any machine authenticated to GHCR (or without auth if the package is Public)

## Pre-release checklist (before tagging `-beta.N` or stable)

Before tagging beta or stable, verify:

- [ ] All tests pass locally: `dotnet test <all solutions>` — 185/185 green expected (177 API + 8 IA)
- [ ] Zero build warnings: `dotnet build --warnaserror` passes on API + Web + IA (cf BACK-015)
- [ ] `dotnet list package --vulnerable --include-transitive` returns "no vulnerable packages" for all 4 targets (API sln, IA sln, Web csproj, IA Tests csproj) — automated via `vuln-audit` CI job
- [ ] CodeQL findings triaged (0 open in Security tab)
- [ ] Lighthouse thresholds met in CI (`lighthouse-a11y` job green)
- [ ] `SECURITY.md` reflects current state
- [ ] `README.md` mentions the new version if it references specific versions

## Post-release checklist (after tagging stable `vX.Y.Z`)

After a stable release:

- [ ] GHCR packages made **Public** (Settings → Package → Change visibility) if not already
- [ ] Deployed to production VPS (`docker compose -f docker-compose.prod.yml pull && up -d`)
- [ ] Smoke tests on production URL (register → login → CRUD recipe → soft delete → cron purge check)
- [ ] Release notes on GitHub Releases page (optional, uses git log since last stable tag)

## Rollback procedure

If a production release causes issues:

```bash
# 1. On the VPS, edit .env to point to the previous stable version
nano .env
# Change API_IMAGE_TAG=v1.0.0 to API_IMAGE_TAG=v0.9.5
# Change WEB_IMAGE_TAG=v1.0.0 to WEB_IMAGE_TAG=v0.9.5

# 2. Pull the older images (still available on GHCR — never delete stable tags)
docker compose -f docker-compose.prod.yml pull

# 3. Restart with old images
docker compose -f docker-compose.prod.yml up -d
```

**Important**: never delete stable tags from GHCR. Keep the last 3-5 stable versions for rollback.

## Complete example — Path from alpha to stable for V1

For the V1.0.0 release cycle:

1. **`v1.0.0-alpha.1`** — First CI/CD validation build (`build-and-push` workflow tested end-to-end)
2. **`v1.0.0-alpha.2`** — After significant feature milestone (e.g., IA activation code merged)
3. **`v1.0.0-beta.1`** — All V1 features complete, deployed to VPS, URL shared with close testers
4. **`v1.0.0-beta.2, .3, ...`** — Iterations after tester feedback
5. **`v1.0.0`** — Public announcement (LinkedIn, Discord)
6. **`v1.0.1, .2, ...`** — Patches after public launch
7. **`v1.1.0`** — First new feature added after public launch
8. **`v2.0.0`** — Only when breaking change requires migration

## References

- [Semantic Versioning 2.0.0](https://semver.org/) — Official spec
- [GitHub Container Registry docs](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry) — GHCR reference
- [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) — Actual CI workflow (see `build-and-push` job)
- [BACKLOG.md → BACK-008](BACKLOG.md#back-008) — CI/CD implementation ticket
