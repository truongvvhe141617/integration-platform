FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY building-blocks/BuildingBlocks.Abstractions/*.csproj building-blocks/BuildingBlocks.Abstractions/
COPY services/config-service/ConfigService.Api/*.csproj services/config-service/ConfigService.Api/
RUN dotnet restore services/config-service/ConfigService.Api/ConfigService.Api.csproj

COPY building-blocks/ building-blocks/
COPY services/config-service/ services/config-service/
RUN dotnet publish services/config-service/ConfigService.Api/ConfigService.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ConfigService.Api.dll"]
