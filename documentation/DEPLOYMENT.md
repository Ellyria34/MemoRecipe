# MemoRecipe Deployment Guide

This document describes how to build, publish, deploy, and rollback the
MemoRecipe stack (API + Frontend + Backup) using GitHub Container Registry
(GHCR) for the API and Frontend images, and a locally-built image for the
Backup service (see BACK-078 / DEC-038 for the rationale).

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
| Backup image                      | Local build   | `docker compose build backup` (uses `infra/backup/` from the repo) |

The GitHub repo and GHCR are two separate services that both live under
the same GitHub account, but store different things (source code vs
built container images). The Backup image is still built locally on the VPS
(BACK-008 CI/CD done 03/08 pushes API + Frontend to GHCR on tag `v*` but
did not migrate the backup image — tracked V1.1 as a follow-up).

### Versioning & rollback

Every image is tagged with semver (`v1.0.0`, `v1.0.1`, ...). The compose
file does NOT reference a version directly — it references env variables
`${API_IMAGE_TAG}` and `${WEB_IMAGE_TAG}` defined in `.env`.

Consequence: to deploy a new version OR to rollback to a previous one,
the only thing to change is `.env`. The compose file itself stays
untouched. Rollback = put the previous tag in `.env`, then re-run
`docker compose pull && up -d`. Estimated downtime ~30s.

---

## Network architecture

The production stack uses a "reverse proxy + localhost bind" pattern for defense in depth :

- The `web` service (nginx serving Blazor WASM) binds its port to `127.0.0.1:8080` inside the VPS. This makes the container reachable ONLY from the VPS itself (loopback interface), never directly from the public Internet.
- An HTTP reverse proxy (Apache or nginx installed on the VPS host, out of scope of this compose stack) listens on port 443 with a Let's Encrypt TLS certificate (see US-08). It terminates HTTPS and forwards each request to `http://127.0.0.1:8080` internally.
- Result : the only public entry point is HTTPS on port 443. Any attempt to reach `http://<vps-public-ip>:8080` directly returns "connection refused" — HTTP clear text exposure is impossible.

This pattern also allows hosting multiple apps on the same VPS behind the same reverse proxy (each with its own subdomain / path routing), and centralizes TLS certificate management (renewal, cipher config, HSTS headers).

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
  with **non-secret** production values (image tags, non-sensitive
  configuration). All sensitive values (JWT signing key, database
  password, third-party API keys) are provisioned separately via
  Docker Compose secrets — see [Secret management (production)](#secret-management-production).

---

## Initial VPS setup (first-time provisioning)

One-time procedure to prepare a fresh Debian/Ubuntu-based VPS for the MemoRecipe stack. Assumes SSH access as `root` initially; subsequent operations use a dedicated non-root `<deploy-user>`.

### 1. System update + create non-root deploy user

```bash
# As root, first login via SSH
apt update && apt upgrade -y

# Create a dedicated non-root user for deployments
adduser <deploy-user>
usermod -aG sudo <deploy-user>

# Copy SSH authorized_keys from root to the new user (if using key auth)
mkdir -p /home/<deploy-user>/.ssh
cp /root/.ssh/authorized_keys /home/<deploy-user>/.ssh/
chown -R <deploy-user>:<deploy-user> /home/<deploy-user>/.ssh
chmod 700 /home/<deploy-user>/.ssh
chmod 600 /home/<deploy-user>/.ssh/authorized_keys

# Log out and reconnect as <deploy-user>. Subsequent commands use sudo.
```

### 2. UFW firewall (least privilege — only expose 22 + 80 + 443)

```bash
sudo apt install -y ufw

# Default deny all inbound, allow all outbound
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Allow SSH (22), HTTP (80 for Let's Encrypt HTTP-01 challenge), HTTPS (443)
sudo ufw allow 22/tcp comment 'SSH'
sudo ufw allow 80/tcp comment 'HTTP (Let'\''s Encrypt challenge + redirect to HTTPS)'
sudo ufw allow 443/tcp comment 'HTTPS'

# Enable UFW (SSH connection stays open during enable)
sudo ufw --force enable
sudo ufw status verbose
```

**Never expose port 8080 publicly** — the `web` container binds to `127.0.0.1:8080` (loopback only) and the reverse proxy on 443 forwards to it internally.

### 3. Docker Engine + docker compose plugin

```bash
# Uninstall any old Docker packages first
sudo apt remove -y docker docker-engine docker.io containerd runc

# Install prerequisites
sudo apt install -y ca-certificates curl gnupg lsb-release

# Add Docker's official GPG key
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Add Docker repository
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker Engine + Compose plugin
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Add <deploy-user> to docker group (avoids sudo for docker commands)
sudo usermod -aG docker <deploy-user>

# Log out and reconnect to apply group membership
```

**Verify installation** :
```bash
docker --version           # >= 24.x
docker compose version     # v2.x plugin
docker run --rm hello-world
```

### 4. Prepare deployment directory + clone repo

```bash
sudo mkdir -p <vps-path>
sudo chown <deploy-user>:<deploy-user> <vps-path>
cd <vps-path>
git clone https://github.com/<owner>/MemoRecipe.git .
```

**Note** : cloning the public repo does NOT require GitHub auth. Secrets are provisioned separately (next section). PATs are only needed for `docker login ghcr.io` (see [One-time setup](#one-time-setup)).

### 5. Basic hardening (SSH + fail2ban optional)

```bash
# Disable SSH root login (edit /etc/ssh/sshd_config)
sudo sed -i 's/^#*PermitRootLogin.*/PermitRootLogin no/' /etc/ssh/sshd_config
sudo sed -i 's/^#*PasswordAuthentication.*/PasswordAuthentication no/' /etc/ssh/sshd_config
sudo systemctl restart sshd

# Optional : install fail2ban (bans IPs after N failed SSH attempts)
sudo apt install -y fail2ban
sudo systemctl enable fail2ban
sudo systemctl start fail2ban
```

**After this section** : the VPS is ready for [Secret management](#secret-management-production) and [First deploy](#first-deploy).

---

## Secret management (production)

Sensitive values (JWT key, DB password, external API tokens) live in
**one file per secret** on the host, mounted read-only by Docker into
`/run/secrets/*` in the container. The API reads them via ASP.NET Core's
`AddKeyPerFile` provider (registered in `Program.cs`). This is safer
than `.env` because file-based secrets do not appear in
`docker inspect`, `ps aux`, or process env dumps.

### Files to create

One file per secret, no extension. The filename becomes the config key
(`Section__Key` → `Section:Key` in the API).

| File name                                | API config key                        |
|------------------------------------------|---------------------------------------|
| `JwtSettings__Secret`                    | `JwtSettings:Secret`                  |
| `ConnectionStrings__DefaultConnection`   | `ConnectionStrings:DefaultConnection` |
| `OcrScan__BaseUrl`                       | `OcrScan:BaseUrl`                     |
| `OcrScan__FunctionKey`                   | `OcrScan:FunctionKey`                 |
| `Telegram__BotToken`                     | `Telegram:BotToken`                   |
| `Telegram__ChatId`                       | `Telegram:ChatId`                     |
| `postgres_password`                      | (used by Postgres container)          |

### One-time setup on the VPS

Placeholders: `<secrets-path>` = host directory (e.g. `/opt/<app>/secrets`),
`<deploy-user>` = the non-root Linux user that runs the deployment.

```bash
# Create the directory, owner-only, off the repo
sudo mkdir -p <secrets-path>
sudo chown <deploy-user>:<deploy-user> <secrets-path>
sudo chmod 700 <secrets-path>

# Write each secret. IMPORTANT: use printf (not echo) — a trailing
# newline would corrupt the value silently.
openssl rand -base64 64 | tr -d '\n' > <secrets-path>/JwtSettings__Secret
printf 'Host=postgres;Port=5432;Database=<db>;Username=<user>;Password=<pass>' \
    > <secrets-path>/ConnectionStrings__DefaultConnection
printf 'https://<function-name>.azurewebsites.net' \
    > <secrets-path>/OcrScan__BaseUrl
# Function key: Azure Portal > Function App > Function Keys > default (host key preferred)
printf '<azure-function-key>'   > <secrets-path>/OcrScan__FunctionKey
printf '<telegram-bot-token>' > <secrets-path>/Telegram__BotToken
printf '<telegram-chat-id>'   > <secrets-path>/Telegram__ChatId
printf '<postgres-password>'  > <secrets-path>/postgres_password

# Lock down every file: read-only, owner only
sudo chmod 400 <secrets-path>/*
```

Back up the plaintext values in a secure secrets vault immediately —
these files are the only copies.

### Compose secrets integration

The compose file at the repo root (`docker-compose.prod.yml`) uses the Docker Secrets pattern with `${SECRETS_PATH}` (set in `.env`) pointing to the host directory holding the one-file-per-secret files. Each service only lists the secrets it actually needs (least privilege).

The `backup` service reads `postgres_password` from `/run/secrets/postgres_password` in `infra/backup/backup.sh` (loaded into `PGPASSWORD` at the start of the script, before any `pg_dump` call).

**Single source of truth** : the actual compose file is `docker-compose.prod.yml` at the repo root. Refer to it directly rather than to any inline extract in this document (extracts get stale as the file evolves — the file itself is versioned in git).

### Verify (never print values)

```bash
# List the mounted files — shows names + sizes, not contents
docker compose -f docker-compose.prod.yml exec api ls -la /run/secrets/
```

If a required secret is missing, the API crashes at startup with a
`Configuration '<key>' is invalid` message from `RequireConfig`. A
running container = all secrets present and non-placeholder.

### Rotate a secret

Overwrite the file on the host, then restart the consuming service:

```bash
# Example: rotate the JWT signing key
openssl rand -base64 64 | tr -d '\n' > <secrets-path>/JwtSettings__Secret
docker compose -f docker-compose.prod.yml up -d --force-recreate api
```

**Impact by secret type:**
- `JwtSettings__Secret` → invalidates ALL active JWTs → every logged-in
  user gets logged out. Rotate during low-traffic hours.
- `ConnectionStrings__DefaultConnection` → API reconnects, ~30s of
  possible 500s on in-flight requests.
- Others → no visible impact on the running app.

---

## Container hardening (OWASP baseline)

`docker-compose.prod.yml` applies 3 OWASP baseline directives on all 4 services (postgres, api, web, backup) for defense in depth (US-06):

- **`read_only: true`**: immutable rootfs. A compromised container cannot persist via rootfs writes (no persistent webshell / backdoor possible).
- **`cap_drop: [ALL]`**: strips all default Docker Linux capabilities. `cap_add:` explicitly re-grants only strictly required caps per service.
- **`tmpfs:`**: mounts runtime write paths as in-memory volumes (volatile, wiped on each restart) instead of the host disk.

### Minimum capabilities per service

| Service | `cap_add` | Justification |
|---|---|---|
| `postgres` | `CHOWN`, `DAC_OVERRIDE`, `FOWNER`, `SETGID`, `SETUID` | Data dir init + WAL rotation |
| `api` | *(none)* | HTTP server on unprivileged port 8080, no cap required |
| `web` (nginx) | `CHOWN`, `SETGID`, `SETUID` | chown cache dirs at startup + master root → worker nginx user switch |
| `backup` | `CHOWN`, `DAC_OVERRIDE`, `FOWNER`, `SETGID`, `SETUID` | `pg_dump` + cron busybox-suid + GPG encryption |

### tmpfs mounts per service

| Service | tmpfs paths | Runtime usage |
|---|---|---|
| `postgres` | `/tmp`, `/var/run/postgresql` | Temp files + Unix socket + PID |
| `api` | `/tmp` | Kestrel temporary files (upload buffering, etc.) |
| `web` | `/var/cache/nginx`, `/var/run`, `/tmp` | nginx cache + PID + temp |
| `backup` | `/tmp`, `/var/log`, `/var/run` | Fresh `GNUPGHOME` per run + cron logs + cron PID |

### Persistent volume for API DataProtection Keys

ASP.NET Core `DataProtection` generates and rotates symmetric keys in `~/.aspnet/DataProtection-Keys` (= `/home/app/.aspnet/DataProtection-Keys` for user `app`). With `read_only: true`, these keys must be stored in a dedicated persistent volume:

```yaml
services:
  api:
    volumes:
      - dataprotection_keys:/home/app/.aspnet/DataProtection-Keys

volumes:
  dataprotection_keys:
    driver: local
```

**Bonus benefit**: keys survive redeploys → no JWT / HttpOnly cookie invalidation on each `docker compose up -d`.

### Non-root users (image defaults preserved)

No explicit `user:` in the compose file. Images already use non-root users by default:

| Service | Image default user | Source |
|---|---|---|
| `postgres` | `postgres` UID 999 | Official `postgres:16-alpine` image |
| `api` | `app` UID 1654 | `<ContainerUser>app</ContainerUser>` csproj (.NET Container Support) |
| `web` (nginx) | `nginx` UID 101 | Official `nginx:alpine` image switches master root → worker after boot |
| `backup` | `root` (for cron) | Cron busybox `/etc/crontabs/root` must run as root to execute crontab tasks |

Do NOT force `user: "1000:1000"`: would break permissions on files copied into images at build time (UID chown mismatch).

### nginx port 8080 (unprivileged) choice

The `web` service listens on port `8080` (unprivileged, ≥ 1024) instead of the traditional nginx port `80`. Benefit: allows strict `cap_drop: [ALL]` without compromising with `NET_BIND_SERVICE` (required to bind ports < 1024). Configured in:
- `App/MemoRecipe.Web/nginx.conf`: `listen 8080;`
- `App/MemoRecipe.Web/Dockerfile`: `EXPOSE 8080`
- `docker-compose.prod.yml`: `target: 8080` (container port) → mapped to host `127.0.0.1:8080` (host port unchanged)

### Healthchecks: explicit IPv4 `127.0.0.1`

Internal healthchecks use `http://127.0.0.1:8080/...` instead of `http://localhost:8080/...`. Reason: the nginx alpine image's `10-listen-on-ipv6-by-default.sh` script tries to modify `default.conf` at boot to enable IPv6 → blocked by `read_only: true`. Result: nginx listens on IPv4 only. `localhost` inside the container may resolve to `::1` (IPv6) depending on config, which fails. `127.0.0.1` forces IPv4 explicitly = deterministic healthcheck.

### Post-deploy verification

Confirm that the 3 directives are applied on all 4 services:

```bash
docker inspect memorecipe_postgres memorecipe_api memorecipe_web memorecipe_backup \
    --format "{{.Name}} | ReadOnly={{.HostConfig.ReadonlyRootfs}} | CapDrop={{.HostConfig.CapDrop}} | CapAdd={{.HostConfig.CapAdd}} | User={{.Config.User}}"
```

**Expected output**:
- `ReadOnly=true` on all 4 services
- `CapDrop=[ALL]` on all 4 services
- `CapAdd` per the capabilities table above
- `User` = image default (non-root)

### Historical context

Baseline `security_opt: no-new-privileges` set in **BACK-007p3** (30/07/2026). Advanced hardening (`read_only` + `cap_drop` + `tmpfs` + `dataprotection_keys` volume) added in **US-06** (31/08/2026, Sprint Alpha.3) to strengthen defense in depth before public exposure.

---

## HTTPS + Let's Encrypt reverse proxy (US-08 anticipation)

> **Status** : this section is written in anticipation of **US-08 (HTTPS forcé prod + Let's Encrypt sur VPS)**. Commands are indicative and will be validated / adapted during the actual US-08 execution. Reverse proxy choice (nginx vs Apache on the VPS host) will be finalized in US-08.

### Prerequisites

- Domain name pointing to the VPS public IP (e.g. `<your-domain>` → A record → `<vps-public-ip>`)
- UFW allows 80 + 443 (done in [Initial VPS setup](#initial-vps-setup-first-time-provisioning))
- Reverse proxy installed on the VPS host (nginx recommended for consistency with the Blazor `web` container)

### 1. Install nginx (reverse proxy) + certbot

```bash
sudo apt install -y nginx certbot python3-certbot-nginx
```

### 2. Create nginx virtual host

```bash
sudo nano /etc/nginx/sites-available/<your-domain>
```

Minimal config (adapt `<your-domain>`) :
```nginx
server {
    listen 80;
    server_name <your-domain>;

    # Let's Encrypt HTTP-01 challenge (before HTTPS is active)
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    # Redirect everything else to HTTPS (added by certbot below)
    location / {
        return 301 https://$host$request_uri;
    }
}
```

Enable the site :
```bash
sudo ln -s /etc/nginx/sites-available/<your-domain> /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 3. Obtain the Let's Encrypt certificate

```bash
sudo certbot --nginx -d <your-domain> --agree-tos --non-interactive --email <admin-email>
```

Certbot auto-configures nginx to :
- Add the `listen 443 ssl` block
- Add `ssl_certificate` + `ssl_certificate_key` paths
- Add HSTS-friendly config (verify + strengthen with `Strict-Transport-Security` preload settings)

### 4. Add reverse proxy to the Blazor `web` container (`127.0.0.1:8080`)

Edit `/etc/nginx/sites-available/<your-domain>` (the `server { listen 443 ssl; ... }` block created by certbot) :

```nginx
server {
    listen 443 ssl http2;
    server_name <your-domain>;

    # SSL config (auto-added by certbot)
    ssl_certificate /etc/letsencrypt/live/<your-domain>/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/<your-domain>/privkey.pem;

    # HSTS preload (recommended for production)
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;

    # Forward to the Blazor web container
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Reload nginx :
```bash
sudo nginx -t && sudo systemctl reload nginx
```

### 5. Auto-renewal (certbot systemd timer already installed)

Verify :
```bash
sudo systemctl status certbot.timer
sudo certbot renew --dry-run
```

Let's Encrypt certs are valid 90 days. The systemd timer runs `certbot renew` twice a day; renewal happens automatically ~30 days before expiry.

### 6. Verify HTTPS end-to-end

```bash
curl -I https://<your-domain>/
# Expected : HTTP/2 200 + Strict-Transport-Security header
```

Test SSL config quality via [SSL Labs](https://www.ssllabs.com/ssltest/) (target : A+ rating post-hardening).

---

## One-time setup

> **Context** : the dev-side write-capable PAT (step 1 + 2 below) is a **fallback** for Workflow 1 + 2 manual builds. The normal build flow uses the CI/CD's native `GITHUB_TOKEN` (BACK-008 DONE), no PAT needed. The VPS read-only PAT (step 3) remains **required** — the VPS is not a GitHub Actions runner, it cannot use `GITHUB_TOKEN` to pull from GHCR.

### 1. Create the GitHub PAT (dev machine, fallback only)

1. GitHub -> Settings -> Developer settings -> Personal access tokens ->
   Tokens (classic) -> Generate new token.
2. Note: a meaningful name (e.g. "GHCR push fallback").
3. Expiration: 90 days recommended (renew on calendar).
4. Scopes: tick `write:packages` (it implies `read:packages` and `repo`).
5. Generate -> copy once -> paste into your password manager with a note
   mentioning the scope and the expiration date.

### 2. Login to GHCR (dev machine, fallback only)

```bash
docker login ghcr.io
# Username: <github-username>
# Password: <paste the PAT>
# -> Login Succeeded
```

### 3. Create a separate read-only PAT for the VPS (required)

Same procedure as step 1, but tick only `read:packages`. Store it
separately. Run `docker login ghcr.io` on the VPS with this PAT.
Keeping write-capable PATs off the VPS limits blast radius if the VPS
is ever compromised.

---

## Workflow 1 — Build & push the API image (dev side)

> **Normal workflow** : push a `v*` git tag → GitHub Actions CI/CD builds and pushes the API image to GHCR automatically (see [`.github/workflows/ci.yml`](../.github/workflows/ci.yml), BACK-008 DONE 03/08/2026). This manual workflow below is a **fallback** for edge cases : hotfix without a proper tag, testing new csproj container settings locally before committing, offline environment without CI access.

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

> **Normal workflow** : same as Workflow 1 — CI/CD (BACK-008 DONE) auto-builds and pushes the Frontend image on tag `v*`. This manual workflow below is a **fallback** for the same edge cases as the API workflow above.

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

## First deploy (initial provisioning)

Distinct from the routine "Deploy update" (Workflow 3 below). This section describes the **one-time first-time deploy** on a fresh VPS, assuming [Initial VPS setup](#initial-vps-setup-first-time-provisioning) + [Secret management](#secret-management-production) + [HTTPS + Let's Encrypt](#https--lets-encrypt-reverse-proxy-us-08-anticipation) are done.

### Prerequisites checklist

- [ ] VPS provisioned + hardened (UFW, non-root user, Docker installed)
- [ ] `<vps-path>` cloned with the repo
- [ ] `.env` populated at repo root (image tags, `POSTGRES_USER`, `POSTGRES_DB`, `SECRETS_PATH`, `GPG_RECIPIENT`, `JWT_ISSUER`, `JWT_AUDIENCE`)
- [ ] All 7 secret files created in `<secrets-path>` (`chmod 400`, owner-only)
- [ ] `docker login ghcr.io` done on the VPS with the read-only PAT
- [ ] nginx reverse proxy configured with Let's Encrypt cert on port 443 → forwards to `127.0.0.1:8080`
- [ ] DNS A record for the domain → VPS public IP

### Steps

```bash
cd <vps-path>

# 1. Set the initial image tags in .env (first release = v1.0.0-alpha.3)
nano .env
# -> API_IMAGE_TAG=v1.0.0-alpha.3
# -> WEB_IMAGE_TAG=v1.0.0-alpha.3

# 2. Pull the initial API + Frontend images from GHCR
docker compose -f docker-compose.prod.yml pull api web

# 3. Build the backup image locally (first time)
docker compose -f docker-compose.prod.yml build backup

# 4. Start the full stack (postgres inits its DB on first run — takes ~30s)
docker compose -f docker-compose.prod.yml up -d

# 5. Wait for all services to become healthy (~60-90s total)
watch -n 5 'docker compose -f docker-compose.prod.yml ps'
# Wait until all 4 services show STATUS = healthy (except backup = Up, no healthcheck)

# 6. Verify the API health endpoint (from the VPS localhost)
curl -f http://localhost:8080/health
# Expected : "Healthy" (200)

# 7. Verify HTTPS end-to-end (from anywhere)
curl -I https://<your-domain>/
# Expected : HTTP/2 200 + security headers (HSTS, X-Frame, CSP, etc.)

# 8. Trigger a manual first backup to verify the backup chain works
docker exec memorecipe_backup /usr/local/bin/backup.sh
docker exec memorecipe_backup ls -lh /backups
# Expected : one memorecipe_YYYY-MM-DD_HH-MM-SS.dump.gpg file
```

### First user provisioning

The initial user must be created via the API `POST /api/auth/register` since the registration endpoint is admin-only in Alpha.3 (`Features:RegistrationEnabled=false` for public, but registration works when called directly by the admin). See US-B1-15 for the invitation mail template.

Alternatively, insert the user directly in the database with a bcrypt hash (advanced, not documented here — prefer the API route).

---

## Workflow 3 — Deploy update (routine deploy)

On the VPS, inside `<vps-path>` — this is the routine flow used after the first deploy, when pushing a new version tag :

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
# Functional health check via HTTP endpoint (BACK-011)
curl -f http://localhost:8080/health
# Expected response: "Healthy" (200) or "Unhealthy" (503)

```

Healthchecks (postgres / api / web) ensure dependent containers wait
for their dependencies. Allow ~45-60s for the API to become healthy.
The `backup` service does not have a healthcheck — it runs cron in the
background and only becomes active once a day during off-peak hours. Verify it
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

## Consultation logs

The production stack emits logs from 4 sources. Use the right tool for each source.

### 1. Container stdout/stderr (Docker native)

Any log written by the application to `stdout` or `stderr` is captured by Docker and accessible via `docker logs`. All 4 services (postgres, api, web, backup) use this by default.

```bash
# Recent logs (last 50 lines) for one service
docker compose -f docker-compose.prod.yml logs --tail=50 api

# Live tail (Ctrl+C to exit) for one service
docker compose -f docker-compose.prod.yml logs -f api

# All services combined, live tail
docker compose -f docker-compose.prod.yml logs -f

# Filter by timestamp (Docker supports RFC3339 or relative)
docker compose -f docker-compose.prod.yml logs --since 1h api
docker compose -f docker-compose.prod.yml logs --since 2026-09-01T14:00:00 api
```

### 2. Serilog structured logs (API)

The API uses Serilog with the `Console` sink in production (writes to stdout, thus captured by `docker logs api`). Log level is configured via `appsettings.json` (default `Information`). Grep patterns for common queries :

```bash
# All login attempts (success + failure) in the last 24h
docker compose -f docker-compose.prod.yml logs --since 24h api | grep -E "LoginSuccess|LoginFailed"

# All admin password resets (audit trail — see Runbook incidents)
docker compose -f docker-compose.prod.yml logs api | grep AdminPasswordReset

# All 4xx/5xx HTTP responses
docker compose -f docker-compose.prod.yml logs api | grep -E "responded (4|5)[0-9]{2}"

# All warnings + errors (skip info/debug noise)
docker compose -f docker-compose.prod.yml logs api | grep -E "\[(WRN|ERR|FTL)\]"
```

**PII redaction** : Serilog is configured with `EmailMasker` (RGPD Art. 5 minimization) — emails appear as `s***@example.com` in logs, never in the clear. See DEC-060 pattern.

### 3. nginx access + error logs (web container)

The nginx image logs `access.log` + `error.log` to symlinks pointing to `/dev/stdout` + `/dev/stderr` respectively (image officielle default). So they are captured by `docker logs web`.

```bash
# All requests routed through the nginx SPA server
docker compose -f docker-compose.prod.yml logs --tail=100 web

# Grep for 404s (missing static assets, wrong routes)
docker compose -f docker-compose.prod.yml logs web | grep " 404 "

# Grep for 5xx from the api upstream (proxy_pass failures)
docker compose -f docker-compose.prod.yml logs web | grep -E " (502|503|504) "
```

### 4. PostgreSQL logs

Postgres logs slow queries + connection issues + init to stdout (via the official `postgres:16-alpine` image config).

```bash
# Recent postgres activity
docker compose -f docker-compose.prod.yml logs --tail=100 postgres

# Grep for authentication failures (e.g. wrong password, missing user)
docker compose -f docker-compose.prod.yml logs postgres | grep -i "authentication failed"

# Grep for slow queries (if log_min_duration_statement is set)
docker compose -f docker-compose.prod.yml logs postgres | grep -i "duration:"
```

### 5. Backup cron logs

The backup container runs cron; each backup execution logs to stdout (captured by `docker logs backup`).

```bash
# Recent backup runs (should be 1 per day at off-peak hours)
docker logs memorecipe_backup --tail=100

# Grep for backup successes / failures
docker logs memorecipe_backup | grep -E "backup completed|backup failed"
```

See [Backup & Restore → Monitoring / verification](#monitoring--verification) for backup-specific health checks.

### Log rotation

Docker's default log driver (`json-file`) grows indefinitely. In production, configure log rotation via `/etc/docker/daemon.json` on the VPS host :

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
```

Restart Docker to apply : `sudo systemctl restart docker`. Each container keeps up to 30 MB of logs (3 × 10 MB rotated files) before oldest logs are discarded.

### Correlation IDs (future)

Structured correlation IDs across `web` → `api` → `postgres` are tracked as a follow-up (BACK-079 remaining scope). Currently, correlate manually via timestamps + user email masked patterns.

---

## Troubleshooting

### `docker compose up` fails: container name already in use

Another compose project is using the same `container_name`
(e.g. `memorecipe_postgres` from the dev compose). Stop the conflicting
project first:

```bash
# From the repository root:
docker compose down
```

### `docker pull` fails: denied or not found

The PAT is missing, expired, or lacks `read:packages`. Re-run
`docker login ghcr.io` with a valid PAT.

### API healthcheck stays "starting" forever

Check the logs: `docker compose ... logs api`. Most common causes:
- PostgreSQL not ready -> the `depends_on: condition: service_healthy`
  should prevent this, but verify postgres logs first.
- `JwtSettings__Secret` secret file missing or too short (< 64 chars). The API fails fast with `Configuration 'JwtSettings:Secret' is invalid`.
- Mismatch between the `postgres_password` file (used by Postgres to init the user) and the `Password=...` inside the `ConnectionStrings__DefaultConnection` file. Both must be strictly identical (no trailing whitespace/newline).
- Mismatch between `.env` `POSTGRES_USER` / `POSTGRES_DB` and the `Username=` / `Database=` values inside the `ConnectionStrings__DefaultConnection` file.

### Data appears empty after switching from dev compose to prod compose

Expected. Docker named volumes are scoped per compose project. The
dev compose volume and the prod compose volume are separate. To
migrate data between them, use `pg_dump` / `pg_restore` (see BACK-068
for the documented procedure).

---

## Runbook incidents

Emergency operational procedures — not routine deployment, not bug troubleshooting.

### User password reset (P0-7)

**When to use** : a user has lost their password. No `forgot password` self-service exists in Alpha.3 (registration is admin-only, password reset planned post-V1). Manual admin intervention required.

**Prerequisites** :
- SSH access to the VPS.
- Docker permissions to `docker exec` on the `memorecipe_api` container.
- The user's registered email address of record.
- **Identity verified via a known channel** (email of record or phone of record) BEFORE resetting. A password reset requested via an unverified channel could be an impersonator.

**Procedure (~5 min end-to-end)** :

1. SSH into the VPS.
2. Navigate to the repo root : `cd <vps-path>`.
3. Run the bash wrapper (prompts for password interactively, never in shell history) :
   ```bash
   ./infra/admin/reset-password.sh memorecipe_api <user-email>
   ```
4. When prompted `New password for <user-email>:`, type the temporary password (input is hidden, no echo).
5. When prompted `Have you verified identity? (yes/no):`, type `yes` only if you have verified the requester's identity through a known channel. Otherwise type anything else to abort — the script exits with `[ABORTED] Identity not verified`, no DB change.
6. On success, the script prints `[OK] Password reset succeeded for <user-email>` and exits with code 0.
7. **Communicate the temporary password to the user through a secure channel** (encrypted messenger, in-person, or password manager share). NEVER via cleartext email.
8. Advise the user to change the password immediately after login (self-service password change endpoint TBD post-V1).

**What the script does under the hood** :
- Prompts password via `read -s` (invisible, never in bash history).
- Writes to a host temp file with `chmod 600` (owner-only).
- Copies the temp file into the container via `docker cp`.
- Runs `dotnet MemoRecipe.Api.dll --reset-password --email <email> --password-file <path>` inside the container. The API's `AdminPasswordResetService` normalizes the email, finds the user, hashes the new password using the SAME `PasswordHasher<User>` as the production login flow (zero divergence guaranteed), persists via EF Core.
- Cleans up the temp file on host (`shred -uz` via `trap EXIT`, guaranteed even on Ctrl+C or error) and in the container.
- Emits a Serilog audit log `AdminPasswordResetPerformed` with the user ID and masked email — never the password.

**Exit codes** :
- `0` : reset succeeded, user can login with the new password.
- `2` : user not found (verify email spelling with the requester).
- `1` : configuration or file error (missing args, unreadable password file, DB unreachable). Check the container logs.

**Failure modes** :
- **User not found** → verify email address of record against your user management source (DB backup, initial invite mail archive).
- **DB unreachable** → check `docker compose ps postgres` and `docker logs memorecipe_postgres`.
- **Permission denied on script** → run `chmod +x infra/admin/reset-password.sh` (already tracked as `100755` in git index post-FIX-004, should not happen in normal flow).

**Audit trail** : each reset emits a WARNING-level Serilog entry with masked email + user ID. Query via `docker logs memorecipe_api | grep AdminPasswordReset` for post-incident review.

### Container down (crash loop)

**Symptom** : one service (`api`, `web`, `postgres`, `backup`) is `Restarting` or `Exited` in `docker compose ps`. Client sees 502 / 503 / connection refused.

**Diagnostic** :
```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs --tail=80 <service>
```

**Common causes + fixes** :
- **API crashes on `Configuration '<key>' is invalid`** → secret file missing or contains `CHANGE_ME` placeholder. Verify with `docker compose exec api ls -la /run/secrets/`. Fix : re-create the missing secret file, `sudo chmod 400`, then `docker compose up -d --force-recreate api`.
- **Postgres crashes on `password authentication failed`** → mismatch between `postgres_password` file and `Password=…` inside `ConnectionStrings__DefaultConnection`. See [Troubleshooting](#troubleshooting).
- **Web crashes on `host not found in upstream "api"`** → api container is down. Bring api healthy first, then web restarts automatically.
- **Backup crashes on GPG lock** → stale `keyboxd` socket. See [Backup Known issues / caveats](#known-issues--caveats). Fix : `docker compose restart backup`.

**Escalation** : if crash loop persists after fixing the config, [rollback](#workflow-4--rollback) to the previous image tag.

### Disk full

**Symptom** : postgres refuses writes with `could not extend file`, backup fails with `No space left on device`, API returns 500 on write endpoints.

**Diagnostic** :
```bash
df -h                                    # host disk usage
docker system df                         # Docker's disk usage breakdown
du -sh /var/lib/docker/volumes/*         # per-volume disk usage
```

**Common causes + fixes** :
- **Backup volume grows unbounded** → verify `RETENTION_DAYS=30` env var is applied. Manual cleanup : `docker exec memorecipe_backup find /backups -mtime +30 -delete`.
- **Docker logs grow unbounded** → apply log rotation config (see [Consultation logs → Log rotation](#log-rotation)) then `sudo systemctl restart docker`.
- **Dangling images from old rollbacks** → `docker image prune -a` (careful : removes all unused images, including previous rollback targets. Verify current tag first).
- **Postgres data volume unexpectedly large** → check for orphan tables / slow query log fill. Emergency : bump the VPS disk allocation via your hosting provider dashboard (requires reboot).

**Prevention** : monitor host disk via `df -h` in a cron alert. Currently manual; automated alerts tracked in BACK-079 remaining scope.

### Let's Encrypt cert expired

**Symptom** : browsers refuse the site with `NET::ERR_CERT_DATE_INVALID` or `Your connection is not private`. `curl -I https://<your-domain>/` returns SSL handshake failure.

**Diagnostic** :
```bash
# Check cert expiry date
sudo certbot certificates

# Check the auto-renewal systemd timer
sudo systemctl status certbot.timer
sudo journalctl -u certbot.timer --since 7d
```

**Common causes + fixes** :
- **Certbot timer disabled/failed** → re-enable : `sudo systemctl enable --now certbot.timer` + test with `sudo certbot renew --dry-run`.
- **Port 80 blocked** (UFW misconfig or reverse proxy config accident) → Let's Encrypt HTTP-01 challenge requires port 80 open. Verify : `sudo ufw status` shows 80/tcp ALLOW. Fix : `sudo ufw allow 80/tcp`.
- **Rate limit hit** (Let's Encrypt : 5 certs / week per domain) → wait for the rolling window (7 days) or use staging environment for tests : `--staging` flag on certbot.

**Emergency force-renew** :
```bash
sudo certbot renew --force-renewal
sudo systemctl reload nginx
```

Verify SSL is back : `curl -I https://<your-domain>/` should return `HTTP/2 200` again.

### EF Core migration failed at startup

**Symptom** : API container crash loop with `System.InvalidOperationException` or Npgsql migration error at boot (Program.cs calls `db.Database.Migrate()` on startup).

**Diagnostic** :
```bash
docker compose -f docker-compose.prod.yml logs --tail=100 api | grep -A 20 "Migration"

# Check applied migrations directly in the DB
docker exec memorecipe_postgres psql -U <db-user> -d <db-name> -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";"
```

**Common causes + fixes** :
- **Migration script incompatible with existing data** (e.g. new NOT NULL column without default on a non-empty table) → restore the previous API image tag ([rollback](#workflow-4--rollback)) + fix the migration locally (add a default value, split in 2 migrations : add nullable → backfill → alter NOT NULL) → re-tag → re-deploy.
- **Migration succeeded partially then crashed** → DB in inconsistent state. Check `__EFMigrationsHistory` : if the failed migration is listed, remove it manually via `DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901xxxxxx_YourMigration';` then retry with a fixed migration.
- **DB user lacks DDL permissions** → verify `<db-user>` has `CREATE, ALTER, DROP` grants on the schema. Fix : temporarily elevate permissions, apply migration, re-lock.

**Prevention** :
- Always test migrations locally against a **production-like DB dump** before deploying.
- For destructive migrations (drop column, rename table), split in 2+ releases with a compatibility window.
- Add `--dry-run` migration checks to CI (tracked follow-up).

**Emergency rollback of a bad migration** : requires manual SQL to reverse the migration's changes (EF Core does not auto-generate down scripts in prod). Consult the migration source `.cs` file in `MemoRecipe.Infrastructure/Migrations/` to identify what to reverse.

---

## Backup & Restore (PostgreSQL)

> **Backup strategy**: local automated encrypted backup (this section) combined with an off-site copy on a separate medium (operator-managed for the initial release, automation tracked as a follow-up in the private ops backlog). The 3-2-1 rule is satisfied through this dual approach. See **DEC-038** for the architectural rationale (GPG asymmetric encryption, phased delivery).

### Architecture

- **Container `backup`** (`infra/backup/Dockerfile`) built from `postgres:16-alpine` + `gnupg` + `busybox-suid` (cron).
- **Daily cron job** during off-peak hours runs `/usr/local/bin/backup.sh` (`infra/backup/backup.sh`).
- **`pg_dump` piped through `gpg --encrypt`** — the plaintext dump never touches disk, only the encrypted `.dump.gpg` is written.
- **Asymmetric encryption**: the container holds only the GPG public key. The private key stays off the VPS (offline secure storage of your choice). Compromising the VPS does NOT compromise the backups.
- **Retention 30 days** locally (`RETENTION_DAYS` env var). Old backups auto-deleted at each run.
- **Volume `backup_data`** persists the encrypted files across container restarts.

### One-time setup (already done — reference)

1. Generate the GPG key pair on your workstation:
   ```bash
   gpg --full-generate-key
   # Type: ECC (curve25519 = Ed25519)
   # Real name: MemoRecipe Backup (or any label of your choice)
   # Email: backup@<your-domain>
   # Passphrase: strong random passphrase from your password manager
   ```
2. Export the **public key** to the repo:
   ```bash
   gpg --export --armor -o infra/backup/memorecipe-backup-pubkey.asc backup@<your-domain>
   ```
3. Export the **private key** for safekeeping (never commit!):
   ```bash
   gpg --export-secret-keys --armor -o memorecipe-privkey-BACKUP.asc backup@<your-domain>
   ```
   - Store the content in **your password manager** as a secure note.
   - Optionally copy the file to an additional offline secure medium for redundancy.
   - **Delete the local `.asc` file after backup** (`rm memorecipe-privkey-BACKUP.asc`).
4. The passphrase is stored in **your password manager** as a login entry (with the "master password re-prompt" flag enabled for extra safety).

### Automatic backups

The `backup` service is defined in `docker-compose.prod.yml` with:
- `depends_on: postgres (service_healthy)` — waits for Postgres to be healthy.
- Environment variables passed to the container: `PGHOST` (hardcoded to the `postgres` service name), `PGUSER` and `PGDATABASE` (from `.env` via `${POSTGRES_USER}` / `${POSTGRES_DB}`). The Postgres password is NOT passed as an env var — `backup.sh` loads it from the mounted file secret `/run/secrets/postgres_password` and exports it as `PGPASSWORD` at script start (section 3a).
- `restart: unless-stopped` — the container stays alive between backups (cron waits inside).

Once the compose stack is up:
```bash
docker compose -f docker-compose.prod.yml up -d
```

The cron inside the `backup` container will run `backup.sh` daily during off-peak hours. Encrypted files land in the `backup_data` volume as `memorecipe_YYYY-MM-DD_HH-MM-SS.dump.gpg`.

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
- your workstation with the **GPG private key imported** (via Kleopatra or `gpg --import`).
- Access to the **passphrase** (your password manager).

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

Alerts on backup failure / staleness are part of **BACK-079** (monitoring + alerts). The `/health` endpoint (BACK-011) is already live via C9; backup-specific alerts are still pending in the remaining BACK-079 scope.

### Known issues / caveats

- **GPG keybox lock in container** (fixed in `backup.sh`): the script uses a fresh temporary `GNUPGHOME` for each run to avoid stale `keyboxd` socket locks left over by previous `docker exec` invocations. Do NOT remove that logic without re-testing end-to-end.
- **Postgres version mismatch**: the backup container uses `postgres:16-alpine` as its base image, guaranteeing the exact same `pg_dump` binary version as the server. When bumping Postgres to a new major version, bump both containers together.
- **Local retention** is a rolling window on the VPS. Off-site protection is delivered through an operator-managed copy on a separate medium; full automation of the off-site step is tracked in the private ops backlog.

---

## Future improvements

- **Automated VPS deploy on tag** : the build+push image step is already automated via GitHub Actions on tag `v*` (see [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)). Missing : SSH-based auto-pull + restart on the VPS after image push (currently manual, tracked for V1.1).
- **Automated rollback** on failed healthcheck (compose watch or
  external supervisor).
- **Image signing** (cosign) for supply chain integrity.
- **Public images** (free unlimited pulls on GHCR for public repos)
  if/when the project goes public-source.
