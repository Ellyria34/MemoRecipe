FROM postgres:16-alpine

# Stage 1 -  Install required packages
RUN apk add --no-cache \
    gnupg \
    busybox-suid

# Stage 2 - Copy scripts and public GPG key
COPY infra/backup/backup.sh /usr/local/bin/backup.sh
COPY infra/backup/restore.sh /usr/local/bin/restore.sh
COPY infra/backup/memorecipe-backup-pubkey.asc /keys/memorecipe-backup-pubkey.asc

# Stage 3 - Make scripts executable + import GPG public key
RUN chmod +x /usr/local/bin/backup.sh /usr/local/bin/restore.sh \
 && gpg --import /keys/memorecipe-backup-pubkey.asc

# Stage 4 - Setup crontab (daily backup at 3am UTC)
COPY infra/backup/crontab /etc/crontabs/root

# Stage 5 - Ensure /backups directory exists
RUN mkdir -p /backups

# Stage 6 - Run crond in foreground with logging
CMD ["crond", "-f", "-d", "8"]
