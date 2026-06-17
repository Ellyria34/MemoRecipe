# MemoRecipe Deployment Guide

This document describes how to build, publish, deploy, and rollback the
MemoRecipe stack (API + Frontend) using GitHub Container Registry (GHCR).

It follows DEC-031 (Registry GHCR over on-VPS build) and DEC-027
(Frontend served via nginx custom Dockerfile).

Placeholders used in this guide:
- `<owner>` -> the GitHub user or organization that owns the repo
- `<github-username>` -> your GitHub login (for the dev machine)
- `<vps-path>` -> the directory where the repo is cloned on the VPS

---

## Overview

A full deployment is a 7-step chronological flow split into two phases:
a publish phase on the dev machine, and a deploy phase on the VPS.

```
================================ DEV SIDE =================================

 +---------------------------------------------------------------------+
 | (1) Edit code / compose / .env.example                              |
 | (2) Build image                                                     |
 |       - API      : dotnet publish /t:PublishContainer               |
 |       - Frontend : docker build                                     |
 | (3) Push image    ----------------------->  [ GHCR ]                |
 |                                             (Docker images stored)  |
 | (4) Push code     ----------------------->  [ GitHub repo ]         |
 |                                             (compose + .env.example)|
 +---------------------------------------------------------------------+

                                  |
                                  v   (later, when deploying)

================================ PROD SIDE ================================

 +---------------------------------------------------------------------+
 | (5) git pull origin main          <-----   [ GitHub repo ]          |
 |       (refreshes compose + .env.example if their structure changed) |
 |                                                                     |
 | (5b) Edit .env to set the new image tags                            |
 |       -> API_IMAGE_TAG=v1.0.1                                       |
 |       -> WEB_IMAGE_TAG=v1.0.1                                       |
 |                                                                     |
 | (6) docker compose pull           <-----   [ GHCR ]                 |
 |       (downloads the new image versions specified in .env)          |
 |                                                                     |
 | (7) docker compose up -d                                            |
 |       (recreates containers using the new images)                   |
 +---------------------------------------------------------------------+
```

### Two artefacts flow through the pipeline

| Artefact                          | Source        | Pulled on VPS via       |
|-----------------------------------|---------------|-------------------------|
| Code + compose + .env.example     | GitHub repo   | `git pull origin main`  |
| Docker images (API + Frontend)    | GHCR          | `docker compose pull`   |

The GitHub repo and GHCR are two separate services that both live under
the same GitHub account, but store different things (source code vs
built container images).

### Versioning & rollback

Every image is tagged with semver (`v1.0.0`, `v1.0.1`, ...). The compose
file does NOT reference a version directly — it references env variables
`${API_IMAGE_TAG}` and `${WEB_IMAGE_TAG}` defined in `.env`.

Consequence: to deploy a new version OR to rollback to a previous one,
the only thing to change is `.env`. The compose file itself stays
untouched. Rollback = put the previous tag in `.env`, then re-run
`docker compose pull && up -d`. Estimated downtime ~30s.

---

## Prerequisites

### Dev machine (build + push)

- Docker Desktop (or Docker Engine + buildx) running.
- .NET SDK 10 installed (for API build via `dotnet publish`).
- A GitHub Personal Access Token (PAT) with `write:packages` scope,
  stored in a password manager. Never commit it.
- `docker login ghcr.io` executed once with the PAT (credentials are
  cached in the OS keyring afterwards).

### Production VPS (pull + run)

- Docker Engine installed (>= 24.x recommended).
- A GitHub PAT with `read:packages` scope only (least privilege).
- `docker login ghcr.io` executed once with that read-only PAT.
- Git installed, and the repo cloned at a stable path (`<vps-path>`).
- A `.env` file at the repo root, populated from `.env.example`
  with production values (POSTGRES_PASSWORD, JWT_SECRET, etc.) and
  the desired image tags.

---

## One-time setup

### 1. Create the GitHub PAT (dev machine)

1. GitHub -> Settings -> Developer settings -> Personal access tokens ->
   Tokens (classic) -> Generate new token.
2. Note: a meaningful name (e.g. "GHCR push").
3. Expiration: 90 days recommended (renew on calendar).
4. Scopes: tick `write:packages` (it implies `read:packages` and `repo`).
5. Generate -> copy once -> paste into your password manager with a note
   mentioning the scope and the expiration date.

### 2. Login to GHCR (dev machine)

```bash
docker login ghcr.io
# Username: <github-username>
# Password: <paste the PAT>
# -> Login Succeeded
```

### 3. Create a separate read-only PAT for the VPS

Same procedure as step 1, but tick only `read:packages`. Store it
separately. Run `docker login ghcr.io` on the VPS with this PAT.
Keeping write-capable PATs off the VPS limits blast radius if the VPS
is ever compromised.

---

## Workflow 1 — Build & push the API image (dev side)

The API uses the .NET Container Support SDK, which builds and pushes
in a single `dotnet publish` command. No Dockerfile needed.

### Steps

1. Decide the new version (semver). Example: previous `v1.0.0` -> new `v1.0.1`.
2. From the API project root:

   ```bash
   cd memoRecipeAppProject/memorecipe-api/src/MemoRecipe.Api
   dotnet publish --os linux --arch x64 \
     /t:PublishContainer \
     /p:ContainerImageTag=v1.0.1
   ```

3. Verify on GHCR: GitHub profile -> Packages -> `memorecipe-api` ->
   new version listed.

### What the csproj does

- `<ContainerRegistry>` -> targets GHCR.
- `<ContainerRepository>` -> namespaces the image under the owner.
- `<ContainerBaseImage>` -> alpine for size + security.
- `<ContainerUser>app</ContainerUser>` -> non-root runtime.
- OCI labels (`org.opencontainers.image.source/description/licenses`)
  -> auto-link to the repo on GHCR (README, license badge displayed).

---

## Workflow 2 — Build & push the Frontend image (dev side)

The Frontend uses a custom Dockerfile (nginx serving Blazor WASM static
files, see DEC-027). Standard `docker build` + `docker push`.

### Steps

1. Decide the new version. Example: previous `v1.0.0` -> new `v1.0.1`.
2. From the Frontend project root:

   ```bash
   cd App/MemoRecipe.Web
   docker build -t ghcr.io/<owner>/memorecipe-web:v1.0.1 .
   docker push ghcr.io/<owner>/memorecipe-web:v1.0.1
   ```

3. Verify on GHCR: same Packages page -> `memorecipe-web` -> new version.

### Notes

- The trailing `.` in `docker build` is the build context (current dir).
- OCI labels are baked into the Dockerfile (Stage 2, after `FROM nginx`).
- On a fresh image name, GHCR may not auto-link to the repo even with
  labels present. Fallback: GHCR package page -> "Connect Repository"
  button -> select the repo manually (one-time).

---

## Workflow 3 — Deploy to production

On the VPS, inside `<vps-path>`:

```bash
# 1. Pull the latest compose + .env.example (in case of structure changes)
git pull origin main

# 2. Edit .env to set the new image tags
nano .env
# -> API_IMAGE_TAG=v1.0.1
# -> WEB_IMAGE_TAG=v1.0.1

# 3. Pull the new images from GHCR and recreate the containers
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d

# 4. Check health
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f --tail=50
```

Healthchecks (postgres / api / web) ensure dependent containers wait
for their dependencies. Allow ~45-60s for the API to become healthy.

---

## Workflow 4 — Rollback

If a deployment misbehaves, rollback is the inverse of step 2 above:
change the tag in `.env` to a known-good previous version, then
`pull && up -d`. Estimated downtime: ~30s.

```bash
# Set the previous version
nano .env
# -> API_IMAGE_TAG=v1.0.0   (was v1.0.1)

# Pull + restart
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

This works because all previous image versions remain available on GHCR
(immutable tags). Never delete a version that is currently a valid
rollback target.

---

## Troubleshooting

### `docker compose up` fails: container name already in use

Another compose project is using the same `container_name`
(e.g. `memorecipe_postgres` from the dev compose). Stop the conflicting
project first:

```bash
docker compose -f memoRecipeAppProject/memorecipe-api/docker-compose.yml down
```

### `docker pull` fails: denied or not found

The PAT is missing, expired, or lacks `read:packages`. Re-run
`docker login ghcr.io` with a valid PAT.

### API healthcheck stays "starting" forever

Check the logs: `docker compose ... logs api`. Most common causes:
- PostgreSQL not ready -> the `depends_on: condition: service_healthy`
  should prevent this, but verify postgres logs first.
- JWT_SECRET missing or too short (< 64 chars).
- Wrong connection string (POSTGRES_USER / DB mismatch between env vars).

### Data appears empty after switching from dev compose to prod compose

Expected. Docker named volumes are scoped per compose project. The
dev compose volume and the prod compose volume are separate. To
migrate data between them, use `pg_dump` / `pg_restore` (see BACK-068
for the documented procedure).

---

## Future improvements

- **CI/CD** (currently manual): GitHub Actions to build + push on tag,
  ssh to the VPS to pull + restart.
- **Automated rollback** on failed healthcheck (compose watch or
  external supervisor).
- **Image signing** (cosign) for supply chain integrity.
- **Public images** (free unlimited pulls on GHCR for public repos)
  if/when the project goes public-source.
