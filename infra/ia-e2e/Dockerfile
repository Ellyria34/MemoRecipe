# ============================================================================
# infra/ia-e2e/Dockerfile - Prebuilt image for E2E tests (CI-stable)
#
# Based on infra/ia-dev/Dockerfile but with the Function project precompiled
# into the image. Removes the need for a volume mount at runtime, which
# eliminates the build+init race that caused CI flakiness with `func start`.
#
# Differs from infra/ia-dev/Dockerfile:
#   - dev: mounts source at /workspace and rebuilds at each container start
#   - e2e: COPY source at build time, `dotnet build` once, no volume mount
# ============================================================================

# .NET 8 SDK on Debian bookworm - same base as ia-dev
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install native libraries required by Tesseract OCR + tools needed to add Microsoft's package feed
# (kept identical to ia-dev/Dockerfile for behavior parity)
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

# Install Azure Functions Core Tools v4 (identical to ia-dev)
RUN curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
        | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/debian/12/prod bookworm main" \
        > /etc/apt/sources.list.d/microsoft-prod.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends azure-functions-core-tools-4 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace

# Copy source and prebuild the Function project into /workspace/bin/output
# so `func start` at container startup skips the build phase entirely
COPY memoRecipe-ia/ /workspace/
RUN dotnet build memorecipe-ia.csproj -c Release

EXPOSE 7071

CMD ["func", "start", "--host", "0.0.0.0"]