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

COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Remplace la config nginx par défaut par la nôtre (SPA routing + gzip)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Stage 2 (suite): app startup configuration
EXPOSE 80