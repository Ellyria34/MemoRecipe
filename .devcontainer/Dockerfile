# Microsoft official image: Debian bookworm + .NET 10 SDK + Git + non-root user "vscode"
# Signed by Microsoft, updated regularly with security patches
FROM mcr.microsoft.com/devcontainers/dotnet:10.0

# Native libraries required by Tesseract OCR and image processing
# Kept minimal: only what the app actually needs at runtime
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-fra \
        libwebp7 \
        libheif1 \
        imagemagick \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Back to non-root user for the dev session (defense in depth)
USER vscode

