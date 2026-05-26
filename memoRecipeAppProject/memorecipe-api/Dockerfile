# Stage 1 "build" : header
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restore optimization — copy only project files first for Docker layer caching
COPY memorecipe-api.sln ./
COPY src/MemoRecipe.Domain/MemoRecipe.Domain.csproj ./src/MemoRecipe.Domain/
COPY src/MemoRecipe.Application/MemoRecipe.Application.csproj ./src/MemoRecipe.Application/
COPY src/MemoRecipe.Infrastructure/MemoRecipe.Infrastructure.csproj ./src/MemoRecipe.Infrastructure/
COPY src/MemoRecipe.Api/MemoRecipe.Api.csproj ./src/MemoRecipe.Api/

RUN dotnet restore memorecipe-api.sln

# Copy the rest of the source code (after restore, to benefit from cache)
COPY . .

# Publish the API in Release mode to /app/publish
WORKDIR /src/src/MemoRecipe.Api
RUN dotnet publish -c Release -o /app/publish --no-restore


# Stage 2: Runtime image (much smaller, no SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

WORKDIR /app
COPY --from=build /app/publish .

# Stage 2 (suite): app startup configuration
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT [ "dotnet", "MemoRecipe.Api.dll" ]