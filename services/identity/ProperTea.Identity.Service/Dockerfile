FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/identity/ProperTea.Identity.Service/ProperTea.Identity.Service.csproj", "services/identity/ProperTea.Identity.Service/"]
COPY ["services/shared/ProperTea.ProperTelemetry/ProperTea.ProperTelemetry.csproj", "services/shared/ProperTea.ProperTelemetry/"]
RUN dotnet restore "services/identity/ProperTea.Identity.Service/ProperTea.Identity.Service.csproj"
COPY . .
WORKDIR "/src/services/identity/ProperTea.Identity.Service"
RUN dotnet build "./ProperTea.Identity.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProperTea.Identity.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProperTea.Identity.Service.dll"]
