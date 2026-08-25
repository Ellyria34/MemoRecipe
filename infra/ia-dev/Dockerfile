# .NET 8 SDK on Debian bookworm - provides runtime + dev tools for Azure Functions development
# Same base OS as prod Azure Functions runtime (Linux Debian glibc)
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install native libraries required by Tesseract OCR + tools needed to add Microsoft's package feed
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-fra \
        libwebp7 \
        libheif1 \
        imagemagick \
        curl \
        gnupg \
        ca-certificates \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Azure Functions Core Tools v4 (the "func" command) via Microsoft's official APT feed.
# Rationale: AFCT v4.13+ requires Node.js 22+, but Debian bookworm only ships Node 18.
# Microsoft's APT package bundles its own runtime and works out of the box on Debian 12.
# The Microsoft package feed is GPG-signed and verified below.
RUN curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
        | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/debian/12/prod bookworm main" \
        > /etc/apt/sources.list.d/microsoft-prod.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends azure-functions-core-tools-4 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Working directory where the Function source code will be mounted at runtime via docker-compose volume
WORKDIR /workspace

# Expose Azure Functions default HTTP port
EXPOSE 7071

# Start the Function host in dev mode with hot reload on source changes
CMD ["func", "start"]