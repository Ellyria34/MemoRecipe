# Security Policy

MemoRecipe takes security seriously. This document explains how to responsibly report vulnerabilities so they can be fixed before public disclosure.

## Supported versions

Only the latest stable release receives security patches. Older versions are not maintained.

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a vulnerability

**Please do NOT open a public GitHub issue for security vulnerabilities.** Public issues can expose users before a fix is available.

Instead, use one of these private channels:

### Preferred — GitHub Private Vulnerability Reporting

1. Go to the **Security** tab of this repository.
2. Click **Report a vulnerability**.
3. Fill in the form with details (see "What to include" below).

This creates a private conversation between you and the maintainer, only visible to invited collaborators.

### Alternative — Email

Send an email to **contact@memorecipe.com** with the subject `[SECURITY] <short summary>`.

If you consider the vulnerability sensitive enough to warrant encryption, mention it in the initial email and a PGP key exchange can be arranged.

### What to include in your report

Please provide as much of the following as possible to help triage:

- **Type** of vulnerability (e.g., authentication bypass, XSS, SQL injection, RCE, information disclosure).
- **Affected version(s)** or commit SHA.
- **Reproduction steps** — minimal, deterministic if possible.
- **Impact assessment** — what an attacker can achieve.
- **Suggested mitigation** if you have one.
- **Your contact info** — how the maintainer can respond and credit you if desired.

## Response process

MemoRecipe is maintained by a solo developer as an open-source portfolio project. Response times reflect this constraint but follow responsible disclosure norms:

| Step                          | Timeline                          |
| ----------------------------- | --------------------------------- |
| Initial acknowledgment        | Within **7 days** of report       |
| Assessment and triage         | Within **14 days** of acknowledgment |
| Patch release (Critical/High) | Within **90 days** of triage      |
| Public disclosure             | Coordinated with reporter after patch |

If a vulnerability is actively exploited in the wild or affects a critical component, timelines may be accelerated.

## Scope

**In scope**:
- Authentication and authorization bypasses.
- Injection attacks (SQL, XSS, log injection, command injection).
- Sensitive data exposure (PII, credentials, session tokens).
- CSRF, clickjacking, and related web-app vulnerabilities.
- Cryptographic weaknesses (hashing, JWT, backup encryption).
- Container escape or Docker configuration issues in shipped images.
- CI/CD workflow vulnerabilities (GitHub Actions, GHCR images).

**Out of scope**:
- Denial-of-service via traffic flooding on public endpoints (rate limiting is applied but flooding is inherent to public HTTP).
- Vulnerabilities in third-party dependencies that are already tracked upstream (please report to the maintainer of that dependency).
- Issues requiring physical access to a user's device.
- Social engineering.
- Reports based solely on automated scanner output without proof of exploitability.

## Recognition

Contributors who report valid security issues will be credited (with permission) in:
- The GitHub Security Advisory when the fix is published.
- The release notes of the version that includes the fix.

## Handling of external dependencies

MemoRecipe relies on third-party NuGet packages. Dependency vulnerabilities are monitored via:
- **Dependabot alerts** (GitHub-native).
- **`dotnet list package --vulnerable --include-transitive`** run automatically in CI on every PR (see `.github/workflows/ci.yml` job `vuln-audit`, fails on High/Critical CVEs).
- **CodeQL static analysis** (SAST) on every PR (see `.github/workflows/codeql.yml`).

Fix cadence for third-party vulnerabilities:
- **Critical/High CVE** → patch or workaround within 7 days.
- **Medium CVE** → patch within 30 days.
- **Low CVE** → tracked and patched in the next regular release.

## License

MemoRecipe is licensed under [AGPL-3.0](LICENSE). This security policy is provided as-is, without warranty.
