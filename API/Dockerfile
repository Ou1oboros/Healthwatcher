# Build context is this directory (the repository root).
#   docker build -t healthwatcherapi .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the project file alone so the layer caches across source edits.
COPY HealthwatcherApi/HealthwatcherApi.csproj HealthwatcherApi/
RUN dotnet restore HealthwatcherApi/HealthwatcherApi.csproj

COPY HealthwatcherApi/ HealthwatcherApi/
RUN dotnet publish HealthwatcherApi/HealthwatcherApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# Non-root by default; $APP_UID is provided by the Microsoft base image.
USER $APP_UID

ENTRYPOINT ["dotnet", "HealthwatcherApi.dll"]
