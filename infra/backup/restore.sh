#!/bin/sh
#
# PostgreSQL restore script for MemoRecipe
# Decrypts a .dump.gpg file with GPG and restores it via pg_restore.
#
# Usage:
#   ./restore.sh <path/to/backup.dump.gpg>
#
# Environment variables required:
#   PGHOST, PGUSER, PGPASSWORD, PGDATABASE
#
# Prerequisites:
#   - The GPG PRIVATE key must be imported locally
#     (import: gpg --import memorecipe-privkey.asc)
#   - The GPG passphrase will be prompted interactively

# ------------------------------------------------------------
# Section 1 - Fail-fast on error
# ------------------------------------------------------------
set -e
trap 'log ERROR "Script failed at line $LINENO with exit code $?"' ERR

# ------------------------------------------------------------
# Section 2 - Logging helper (same as backup.sh)
# ------------------------------------------------------------
log() {
    level="$1"
    message="$2"
    printf '[%s] [%s] %s\n' "$(date '+%Y-%m-%d %H:%M:%S')" "$level" "$message"
}

# ------------------------------------------------------------
# Section 3 - Argument and environment validation
# ------------------------------------------------------------
if [ $# -ne 1 ]; then
    log ERROR "Usage: $0 <path/to/backup.dump.gpg>"
    exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "${BACKUP_FILE}" ]; then
    log ERROR "Backup file to restore not found: ${BACKUP_FILE}"
    exit 1
fi

: "${PGHOST:?PGHOST is required}"
: "${PGUSER:?PGUSER is required}"
: "${PGPASSWORD:?PGPASSWORD is required}"
: "${PGDATABASE:?PGDATABASE is required}"

log INFO "=== MemoRecipe restore started ==="
log INFO "Source: ${BACKUP_FILE}"
log INFO "Target: ${PGUSER}@${PGHOST}/${PGDATABASE}"

# ------------------------------------------------------------
# Section 4 - Decrypt + restore
# ------------------------------------------------------------
TEMP_DUMP=$(mktemp)
trap 'rm -f "${TEMP_DUMP}"' EXIT

log INFO "Decrypting backup with GPG (passphrase required)..."
gpg --decrypt --output "${TEMP_DUMP}" "${BACKUP_FILE}"

log INFO "Restoring database (drop + recreate all tables)..."
START_TS=$(date '+%s')

pg_restore \
    --host="${PGHOST}" \
    --username="${PGUSER}" \
    --dbname="${PGDATABASE}" \
    --clean \
    --if-exists \
    --no-owner \
    --no-acl \
    "${TEMP_DUMP}"

END_TS=$(date '+%s')
DURATION=$((END_TS - START_TS))

log INFO "Restore completed | Duration: ${DURATION}s"
log INFO "=== MemoRecipe restore completed successfully ==="
