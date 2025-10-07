FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/gateway/ProperTea.Gateway/ProperTea.Gateway.csproj", "services/gateway/ProperTea.Gateway/"]
RUN dotnet restore "./services/gateway/ProperTea.Gateway/ProperTea.Gateway.csproj"
COPY . .
WORKDIR "/src/services/gateway/ProperTea.Gateway"
RUN dotnet build "./ProperTea.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProperTea.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProperTea.Gateway.dll"]