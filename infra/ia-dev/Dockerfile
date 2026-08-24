# .NET 8 SDK on Debian bookworm - provides runtime + dev tools for Azure Functions development
# Same base OS as prod Azure Functions runtime (Linux Debian glibc)
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install native libraries required by Tesseract OCR + Node.js from Debian repo
# All packages come from official Debian bookworm repos (safe: no external repos, no curl|bash)
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-fra \
        libwebp7 \
        libheif1 \
        imagemagick \
        nodejs \
        npm \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Azure Functions Core Tools v4 CLI (the "func" command)
# Isolated in this dev-only container, not exposed to the host
RUN npm install -g azure-functions-core-tools@4

# Working directory where the Function source code will be mounted at runtime via docker-compose volume
WORKDIR /workspace

# Expose Azure Functions default HTTP port
EXPOSE 7071

# Start the Function host in dev mode with hot reload on source changes
CMD ["func", "start", "--csharp"]