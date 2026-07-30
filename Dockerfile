# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Sportner.slnx ./
COPY src/Domain/Sportner.Domain.csproj src/Domain/
COPY src/Application/Sportner.Application.csproj src/Application/
COPY src/Infrastructure/Sportner.Infrastructure.csproj src/Infrastructure/
COPY src/Localization/Sportner.Localization.csproj src/Localization/
COPY src/API/Sportner.API.csproj src/API/

RUN dotnet restore src/API/Sportner.API.csproj

COPY src/Domain/ src/Domain/
COPY src/Application/ src/Application/
COPY src/Infrastructure/ src/Infrastructure/
COPY src/Localization/ src/Localization/
COPY src/API/ src/API/

RUN dotnet publish src/API/Sportner.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Sportner.API.dll"]
