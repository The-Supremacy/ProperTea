FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/gateway/ProperTea.Landlord.Bff/ProperTea.Landlord.Bff.csproj", "services/gateway/ProperTea.Landlord.Bff/"]
COPY ["services/shared/ProperTea.ProperTelemetry/ProperTea.ProperTelemetry.csproj", "services/shared/ProperTea.ProperTelemetry/"]
RUN dotnet restore "services/gateway/ProperTea.Landlord.Bff/ProperTea.Landlord.Bff.csproj"
COPY . .
WORKDIR "/src/services/gateway/ProperTea.Landlord.Bff"
RUN dotnet build "./ProperTea.Landlord.Bff.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProperTea.Landlord.Bff.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProperTea.Landlord.Bff.dll"]
