FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

# Restore dependencies separately so source changes can reuse the package cache.
COPY Directory.Build.props global.json .editorconfig ./
COPY src/Core/Common/Common.csproj src/Core/Common/
COPY src/Core/Application/Application.csproj src/Core/Application/
COPY src/Data/Data/Data.csproj src/Data/Data/
COPY src/Data/Postgresql/Postgresql.csproj src/Data/Postgresql/
COPY src/Api/AspNetCore/AspNetCore.csproj src/Api/AspNetCore/

RUN dotnet restore src/Api/AspNetCore/AspNetCore.csproj

COPY src/ src/

RUN dotnet publish src/Api/AspNetCore/AspNetCore.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM build AS migration-build

COPY .config/dotnet-tools.json .config/

RUN dotnet tool restore
RUN dotnet ef migrations bundle \
    --project src/Data/Postgresql \
    --startup-project src/Api/AspNetCore \
    --configuration Release --no-build --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrations

WORKDIR /app

COPY --from=migration-build /app/efbundle .
COPY --from=build /app/publish/appsettings.json .

USER $APP_UID

ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "ConferenceBooking.Api.AspNetCore.dll"]
