FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/services/Organization/ProperTea.Organization/ProperTea.Organization.csproj", "src/services/Organization/ProperTea.Organization/"]
COPY ["src/services/Organization/ProperTea.Organization.Domain/ProperTea.Organization.Domain.csproj", "src/services/Organization/ProperTea.Organization.Domain/"]
COPY ["src/shared/ProperTea.Utilities/ProperTea.Utilities.csproj", "src/shared/ProperTea.Utilities/"]
COPY ["src/shared/ProperTea.Core/ProperTea.Core.csproj", "src/shared/ProperTea.Core/"]
COPY ["src/shared/ProperTea.Infrastructure/ProperTea.Infrastructure.csproj", "src/shared/ProperTea.Infrastructure/"]
RUN dotnet restore "src/services/Organization/ProperTea.Organization/ProperTea.Organization.csproj"
COPY . .
WORKDIR "/src/src/services/Organization/ProperTea.Organization"
RUN dotnet build "./ProperTea.Organization.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProperTea.Organization.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

HEALTHCHECK --interval=15s --timeout=5s --start-period=5s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProperTea.Organization.dll"]
