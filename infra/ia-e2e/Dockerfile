# ============================================================================
# infra/ia-e2e/Dockerfile - Prebuilt image for E2E tests (CI-stable)
#
# This image uses the official Azure Functions runtime that runs the function 
# in production, rather than `func start`, a development tool. 
# The end-to-end (e2e) stack therefore tests the same runtime as production.
# ============================================================================

# 1- .NET 8 SDK on Debian bookworm (same base as ia-dev)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY memoRecipe-ia/memorecipe-ia.csproj ./
RUN dotnet restore memorecipe-ia.csproj
COPY memoRecipe-ia/ ./
RUN dotnet publish memorecipe-ia.csproj -c Release -o /app/publish --no-restore

# 2 - Add Azure Function and environment variables
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV ASPNETCORE_URLS=http://+:7071
ENV AzureWebJobsSecretStorageType=files
COPY --from=build /app/publish /home/site/wwwroot/
COPY /infra/ia-e2e/host-secrets.json /azure-functions-host/Secrets/host.json

EXPOSE 7071
