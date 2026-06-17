# Stage 1 — build : publish Blazor WASM en fichiers statiques
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restore optimization
COPY MemoRecipe.Web.csproj ./
RUN dotnet restore MemoRecipe.Web.csproj

# Copy the rest of the source code (after restore, to benefit from cache)
COPY . .

# Publish the Blazor WASM in Release mode to /app/publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime image (smaller nginx for static file)
FROM nginx:alpine AS runtime

# OCI standard labels for image self-documentation on GHCR
LABEL org.opencontainers.image.source="https://github.com/Ellyria34/MemoRecipe"
LABEL org.opencontainers.image.description="MemoRecipe Frontend - Blazor WASM SPA served by nginx"
LABEL org.opencontainers.image.licenses="AGPL-3.0"

COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Remplace la config nginx par défaut par la nôtre (SPA routing + gzip)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Stage 2 (suite): app startup configuration
EXPOSE 80