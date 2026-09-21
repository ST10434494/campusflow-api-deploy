FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /source

COPY api/CampusFlow.Api/CampusFlow.Api.csproj api/CampusFlow.Api/
RUN dotnet restore api/CampusFlow.Api/CampusFlow.Api.csproj

COPY api/CampusFlow.Api/ api/CampusFlow.Api/

RUN dotnet publish api/CampusFlow.Api/CampusFlow.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "CampusFlow.Api.dll"]
