# Microsoft official image: Debian bookworm + .NET 10 SDK + Git + non-root user "vscode"
# Signed by Microsoft, updated regularly with security patches
FROM mcr.microsoft.com/devcontainers/dotnet:10.0

# Native libraries required by Tesseract OCR and image processing
# Kept minimal: only what the app actually needs at runtime
# curl is required to download the .NET install script below
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-fra \
        libwebp7 \
        libheif1 \
        imagemagick \
        curl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install .NET 8 SDK alongside the .NET 10 SDK already in the base image.
# We use Microsoft's official install script (not apt) so both SDKs live in the
# same directory (/usr/share/dotnet). If we installed dotnet-sdk-8.0 via apt,
# it would land in /usr/lib/dotnet/ and its own dotnet binary would override
# the base image's, hiding the .NET 10 SDK from `dotnet --list-sdks`.
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh

# Back to non-root user for the dev session (defense in depth)
USER vscode

