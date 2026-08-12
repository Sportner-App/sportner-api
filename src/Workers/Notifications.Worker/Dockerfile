# syntax=docker/dockerfile:1
# Render / Docker: root context = repo root, dockerfile path = this file
#   docker build -f src/Workers/Notifications.Worker/Dockerfile -t sportner-notifications-worker:local .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Sportner.slnx ./
COPY src/Domain/Sportner.Domain.csproj src/Domain/
COPY src/Application/Sportner.Application.csproj src/Application/
COPY src/Infrastructure/Sportner.Infrastructure.csproj src/Infrastructure/
COPY src/Localization/Sportner.Localization.csproj src/Localization/
COPY src/Workers/Hosting/Sportner.Workers.Hosting.csproj src/Workers/Hosting/
COPY src/Workers/Notifications.Worker/Sportner.Notifications.Worker.csproj src/Workers/Notifications.Worker/

RUN dotnet restore src/Workers/Notifications.Worker/Sportner.Notifications.Worker.csproj

COPY src/Domain/ src/Domain/
COPY src/Application/ src/Application/
COPY src/Infrastructure/ src/Infrastructure/
COPY src/Localization/ src/Localization/
COPY src/Workers/Hosting/ src/Workers/Hosting/
COPY src/Workers/Notifications.Worker/ src/Workers/Notifications.Worker/

RUN dotnet publish src/Workers/Notifications.Worker/Sportner.Notifications.Worker.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV DOTNET_EnableDiagnostics=0 \
    DOTNET_ENVIRONMENT=Production \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Sportner.Notifications.Worker.dll"]
