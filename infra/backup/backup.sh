#!/bin/sh
#
# PostgreSQL backup script for MemoRecipe
# - pg_dump (format custom) piped through GPG asymmetric encryption
# - Retention policy: delete backups older than RETENTION_DAYS
# - Detailed logging (timestamp, step, size, duration)
#
# Environment variables required:
#   PGHOST, PGUSER, PGPASSWORD, PGDATABASE
#   GPG_RECIPIENT (email of the public key imported in the container)
#   BACKUP_DIR (default: /backups)
#   RETENTION_DAYS (default: 30)

# ------------------------------------------------------------
# Section 1 - Fail-fast on error
# ------------------------------------------------------------
set -e
trap 'log ERROR "Script failed at line $LINENO with exit code $?"' ERR

# ------------------------------------------------------------
# Section 2 - Logging helper
# Format: [YYYY-MM-DD HH:MM:SS] [LEVEL] message
# Levels: INFO, WARN, ERROR
# ------------------------------------------------------------
log() {
    # Usage: log LEVEL "message"
    level="$1"
    message="$2"
    printf '[%s] [%s] %s\n' "$(date '+%Y-%m-%d %H:%M:%S')" "$level" "$message"
}

# ------------------------------------------------------------
# Section 3 - Environment variable validation (fail-fast)
# ------------------------------------------------------------
: "${PGHOST:?PGHOST is required}"
: "${PGUSER:?PGUSER is required}"
: "${PGPASSWORD:?PGPASSWORD is required}"
: "${PGDATABASE:?PGDATABASE is required}"
: "${GPG_RECIPIENT:?GPG_RECIPIENT is required}"
BACKUP_DIR="${BACKUP_DIR:-/backups}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"

log INFO "=== MemoRecipe backup started ==="
log INFO "Target: ${PGUSER}@${PGHOST}/${PGDATABASE}"
log INFO "Backup dir: ${BACKUP_DIR}"
log INFO "GPG recipient: ${GPG_RECIPIENT}"
log INFO "Retention: ${RETENTION_DAYS} days"

# ------------------------------------------------------------
# Section 4 - Generate timestamp and filename
# ------------------------------------------------------------
TIMESTAMP=$(date '+%Y-%m-%d_%H-%M-%S')
BACKUP_FILE="${BACKUP_DIR}/memorecipe_${TIMESTAMP}.dump.gpg"

mkdir -p "${BACKUP_DIR}"


# ------------------------------------------------------------
# Section 5 - pg_dump piped through GPG (encrypt)
# ------------------------------------------------------------
log INFO "Starting pg_dump + gpg encryption..."
START_TS=$(date '+%s')

pg_dump \
    --host="${PGHOST}" \
    --username="${PGUSER}" \
    --dbname="${PGDATABASE}" \
    --format=custom \
    --no-owner \
    --no-acl \
  | gpg \
    --batch \
    --yes \
    --trust-model always \
    --encrypt \
    --recipient "${GPG_RECIPIENT}" \
    --output "${BACKUP_FILE}"

END_TS=$(date '+%s')
DURATION=$((END_TS - START_TS))
SIZE=$(du -h "${BACKUP_FILE}" | cut -f1)

log INFO "Backup written: ${BACKUP_FILE}"
log INFO "Size: ${SIZE} | Duration: ${DURATION}s"


# ------------------------------------------------------------
# Section 6 - Cleanup old backups + final summary
# ------------------------------------------------------------
log INFO "Cleaning up backups older than ${RETENTION_DAYS} days..."
DELETED=$(find "${BACKUP_DIR}" -name "memorecipe_*.dump.gpg" -type f -mtime "+${RETENTION_DAYS}" -print -delete | wc -l)
log INFO "Deleted ${DELETED} old backup(s)"

REMAINING=$(find "${BACKUP_DIR}" -name "memorecipe_*.dump.gpg" -type f | wc -l)
log INFO "Total backups remaining: ${REMAINING}"
log INFO "=== MemoRecipe backup completed successfully ==="

