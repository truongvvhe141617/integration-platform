FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY building-blocks/BuildingBlocks.Abstractions/*.csproj building-blocks/BuildingBlocks.Abstractions/
COPY building-blocks/BuildingBlocks.Core/*.csproj building-blocks/BuildingBlocks.Core/
COPY services/integration-service/IntegrationService.Api/*.csproj services/integration-service/IntegrationService.Api/
RUN dotnet restore services/integration-service/IntegrationService.Api/IntegrationService.Api.csproj

COPY building-blocks/ building-blocks/
COPY services/integration-service/ services/integration-service/
RUN dotnet publish services/integration-service/IntegrationService.Api/IntegrationService.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Non-root user (security)
RUN adduser --disabled-password --gecos "" appuser && \
    mkdir -p /app/configs /app/plugins && \
    chown -R appuser:appuser /app
USER appuser

COPY --from=build --chown=appuser:appuser /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "IntegrationService.Api.dll"]
