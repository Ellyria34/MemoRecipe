# MemoRecipe Deployment Guide

This document describes how to build, publish, deploy, and rollback the
MemoRecipe stack (API + Frontend + Backup) using GitHub Container Registry
(GHCR) for the API and Frontend images, and a locally-built image for the
Backup service (part 1 — see BACK-078 / DEC-038 for the rationale).

It follows DEC-031 (Registry GHCR over on-VPS build) and DEC-027
(Frontend served via nginx custom Dockerfile), and DEC-038 (backup with
GPG asymmetric encryption + 3-2-1 rule, split in two parts).

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

### Three artefacts flow through the pipeline

| Artefact                          | Source        | Pulled/built on VPS via                        |
|-----------------------------------|---------------|------------------------------------------------|
| Code + compose + .env.example     | GitHub repo   | `git pull origin main`                         |
| Docker images (API + Frontend)    | GHCR          | `docker compose pull`                          |
| Backup image (part 1)             | Local build   | `docker compose build backup` (uses `infra/backup/` from the repo) |

The GitHub repo and GHCR are two separate services that both live under
the same GitHub account, but store different things (source code vs
built container images). The Backup image is built locally in part 1
(BACK-078 part 1); pushing it to GHCR is deferred to part 2 (or when
adding a CI/CD pipeline in BACK-008) to keep the initial scope focused.

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

# 3. Pull the new API + Frontend images from GHCR
docker compose -f docker-compose.prod.yml pull api web

# 4. Build the backup image locally (uses infra/backup/ from the repo).
#    Only needed on first deploy or after changes to backup scripts / Dockerfile.
docker compose -f docker-compose.prod.yml build backup

# 5. Recreate all containers
docker compose -f docker-compose.prod.yml up -d

# 6. Check health
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f --tail=50
```

Healthchecks (postgres / api / web) ensure dependent containers wait
for their dependencies. Allow ~45-60s for the API to become healthy.
The `backup` service does not have a healthcheck — it runs cron in the
background and only becomes active once a day at 3am UTC. Verify it
runs via `docker logs memorecipe_backup` and `docker exec memorecipe_backup ls /backups`.

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

## Backup & Restore (PostgreSQL)

> ⚠️ **PART 1 ONLY — NOT PROD-READY**. This section documents the local backup pipeline implemented in BACK-078 part 1. Backups are stored **only on the VPS** (violates the 3-2-1 rule). Off-site copy (Swiss Backup or Backblaze B2) will be added in BACK-078 part 2 before the app is deployed to public production. See **DEC-038** for the full architectural rationale (GPG asymmetric encryption, 3-2-1 rule, part 1/2 split).

### Architecture

- **Container `backup`** (`infra/backup/Dockerfile`) built from `postgres:16-alpine` + `gnupg` + `busybox-suid` (cron).
- **Daily cron job** at 3am UTC runs `/usr/local/bin/backup.sh` (`infra/backup/backup.sh`).
- **`pg_dump` piped through `gpg --encrypt`** — the plaintext dump never touches disk, only the encrypted `.dump.gpg` is written.
- **Asymmetric encryption**: the container holds only the GPG public key. The private key stays on Sarah's laptop + Bitwarden + USB key. Compromising the VPS does NOT compromise the backups.
- **Retention 30 days** locally (`RETENTION_DAYS` env var). Old backups auto-deleted at each run.
- **Volume `backup_data`** persists the encrypted files across container restarts.

### One-time setup (already done — reference)

1. Generate the GPG key pair on Sarah's laptop:
   ```bash
   gpg --full-generate-key
   # Type: ECC (curve25519 = Ed25519)
   # Real name: Sarah MemoRecipe Backup
   # Email: backup@memorecipe.com
   # Passphrase: strong random passphrase from Bitwarden
   ```
2. Export the **public key** to the repo:
   ```bash
   gpg --export --armor -o infra/backup/memorecipe-backup-pubkey.asc backup@memorecipe.com
   ```
3. Export the **private key** for safekeeping (never commit!):
   ```bash
   gpg --export-secret-keys --armor -o memorecipe-privkey-BACKUP.asc backup@memorecipe.com
   ```
   - Store the content in **Bitwarden** as a secure note.
   - Optionally copy the file to a physical USB key.
   - **Delete the local `.asc` file after backup** (`rm memorecipe-privkey-BACKUP.asc`).
4. The passphrase is stored in **Bitwarden** as a login entry (with the "master password re-prompt" flag enabled for extra safety).

### Automatic backups

The `backup` service is defined in `docker-compose.prod.yml` with:
- `depends_on: postgres (service_healthy)` — waits for Postgres to be healthy.
- Environment variables mapping `.env` `POSTGRES_*` to the standard PostgreSQL `PG*` names (`PGHOST`, `PGUSER`, `PGPASSWORD`, `PGDATABASE`).
- `restart: unless-stopped` — the container stays alive between backups (cron waits inside).

Once the compose stack is up:
```bash
docker compose -f docker-compose.prod.yml up -d
```

The cron inside the `backup` container will run `backup.sh` every day at 3am UTC. Encrypted files land in the `backup_data` volume as `memorecipe_YYYY-MM-DD_HH-MM-SS.dump.gpg`.

### Manual backup (on-demand)

To trigger a backup immediately without waiting for the cron:
```bash
docker exec memorecipe_backup /usr/local/bin/backup.sh
```

Then verify the file was written:
```bash
docker exec memorecipe_backup ls -lh /backups
```

### Restore procedure (disaster recovery)

Prerequisites:
- Sarah's laptop with the **GPG private key imported** (via Kleopatra or `gpg --import`).
- Access to the **passphrase** (Bitwarden).

Step 1 — Copy the encrypted backup from the container to the laptop:
```bash
docker exec memorecipe_backup ls /backups
# Pick the file to restore, e.g. memorecipe_2026-07-07_16-24-10.dump.gpg
docker cp memorecipe_backup:/backups/memorecipe_2026-07-07_16-24-10.dump.gpg ./
```

Step 2 — Decrypt with the private key (passphrase prompted by GPG):
```bash
gpg --decrypt --output backup-to-restore.dump memorecipe_2026-07-07_16-24-10.dump.gpg
```
- On Windows/PowerShell, `gpg` uses Gpg4win/Kleopatra which shows a passphrase prompt window.
- On Linux/macOS, the passphrase is prompted in the terminal.

Step 3 — Copy the plaintext dump into the Postgres container:
```bash
docker cp backup-to-restore.dump memorecipe_postgres:/tmp/backup-to-restore.dump
```

Step 4 — Restore the database (`--clean --if-exists` = drop objects before recreating):
```bash
docker exec memorecipe_postgres pg_restore \
    -U memorecipe -d memorecipe \
    --clean --if-exists \
    /tmp/backup-to-restore.dump
```

Step 5 — Verify the data is restored (adapt the query to your actual tables):
```bash
docker exec memorecipe_postgres psql -U memorecipe -d memorecipe -c "SELECT COUNT(*) FROM \"Users\";"
```

Step 6 — Clean up the plaintext file (**contains all user data in the clear — do NOT leave it around**):
```bash
docker exec memorecipe_postgres rm /tmp/backup-to-restore.dump
rm backup-to-restore.dump
```

### Alternative: inspect a backup without restoring

To see what's inside a backup without applying it:
```bash
# List of objects in the dump
docker exec memorecipe_postgres pg_restore --list /tmp/backup-to-restore.dump

# Convert back to plain SQL for inspection
docker exec memorecipe_postgres pg_restore --file=- /tmp/backup-to-restore.dump > backup-content.sql
```

### Monitoring / verification

Check that the backup container is running and cron is alive:
```bash
docker compose -f docker-compose.prod.yml ps backup
docker logs memorecipe_backup
```

Check the latest backups in the volume:
```bash
docker exec memorecipe_backup ls -lh /backups
```

Check the age of the latest backup (should be < 25h):
```bash
docker exec memorecipe_backup sh -c 'ls -lt /backups/memorecipe_*.dump.gpg | head -1'
```

Alerts on backup failure / staleness will be implemented in **BACK-079** (monitoring + alerts).

### Known issues / caveats

- **GPG keybox lock in container** (fixed in `backup.sh`): the script uses a fresh temporary `GNUPGHOME` for each run to avoid stale `keyboxd` socket locks left over by previous `docker exec` invocations. Do NOT remove that logic without re-testing end-to-end.
- **Postgres version mismatch**: the backup container uses `postgres:16-alpine` as its base image, guaranteeing the exact same `pg_dump` binary version as the server. When bumping Postgres to a new major version, bump both containers together.
- **Retention is local only** in part 1: 30 days rolling window on the VPS. If the VPS goes down, everything is lost. Off-site copy will land in BACK-078 part 2 (Swiss Backup or Backblaze B2).

---

## Future improvements

- **CI/CD** (currently manual): GitHub Actions to build + push on tag,
  ssh to the VPS to pull + restart.
- **Automated rollback** on failed healthcheck (compose watch or
  external supervisor).
- **Image signing** (cosign) for supply chain integrity.
- **Public images** (free unlimited pulls on GHCR for public repos)
  if/when the project goes public-source.
