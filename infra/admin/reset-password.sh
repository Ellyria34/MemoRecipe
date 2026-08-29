#!/usr/bin/env bash
# Admin password reset runbook (P0-7).
# Usage: ./reset-password.sh <container-name> <user-email>
# Example: ./reset-password.sh memorecipe-api sarah@example.com
#
# Password is prompted interactively (never in shell history, never in ps output,
# never passed as CLI arg). File used to bridge host -> container is chmod 600
# and shredded on exit even on error or Ctrl+C.

set -euo pipefail

CONTAINER_NAME="${1:-}"
USER_EMAIL="${2:-}"

if [[ -z "$CONTAINER_NAME" || -z "$USER_EMAIL" ]]; then
    echo "Usage: $0 <container-name> <user-email>"
    echo "Example: $0 memorecipe-api sarah@example.com"
    exit 1
fi

# Prompt password silently (never echoed, never in bash history)
echo -n "New password for $USER_EMAIL: "
read -rs NEW_PASSWORD
echo

if [[ -z "$NEW_PASSWORD" ]]; then
    echo "[ERROR] Empty password, aborting"
    exit 1
fi

# Audit: force identity verification confirmation before any DB write
echo
echo "IMPORTANT: Verify user identity BEFORE resetting."
echo "The user MUST have contacted you via a known channel (email of record,"
echo "phone number of record). An unverified channel request may be an impersonator."
echo -n "Have you verified identity? (yes/no): "
read -r CONFIRM
if [[ "$CONFIRM" != "yes" ]]; then
    echo "[ABORTED] Identity not verified"
    exit 1
fi

# Write password to a host temp file (chmod 600 = owner-only read/write)
TMPFILE=$(mktemp)
chmod 600 "$TMPFILE"

# Cleanup temp file on ANY exit (success, error, Ctrl+C, killed)
trap 'shred -uz "$TMPFILE" 2>/dev/null || rm -f "$TMPFILE"' EXIT

echo -n "$NEW_PASSWORD" > "$TMPFILE"

# Copy the temp file into the container (docker exec cannot read host paths)
CONTAINER_TMP="/tmp/pwd-reset-$$.txt"
docker cp "$TMPFILE" "$CONTAINER_NAME:$CONTAINER_TMP"

# Run the admin reset via the container's dotnet runtime
docker exec "$CONTAINER_NAME" dotnet MemoRecipe.Api.dll \
    --reset-password \
    --email "$USER_EMAIL" \
    --password-file "$CONTAINER_TMP"

RESULT=$?

# Cleanup the temp file INSIDE the container (best effort, ignore errors)
docker exec "$CONTAINER_NAME" rm -f "$CONTAINER_TMP" 2>/dev/null || true

# Host temp file is cleaned up by the EXIT trap
exit $RESULT
