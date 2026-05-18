FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/business-service/BusinessService.Api/*.csproj services/business-service/BusinessService.Api/
RUN dotnet restore services/business-service/BusinessService.Api/BusinessService.Api.csproj

COPY services/business-service/ services/business-service/
RUN dotnet publish services/business-service/BusinessService.Api/BusinessService.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "BusinessService.Api.dll"]
