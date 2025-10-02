FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/api-gateway/ProperTea.ApiGateway/ProperTea.ApiGateway.csproj", "services/api-gateway/ProperTea.ApiGateway/"]
RUN dotnet restore "./services/api-gateway/ProperTea.ApiGateway/ProperTea.ApiGateway.csproj"
COPY . .
WORKDIR "/src/services/api-gateway/ProperTea.ApiGateway"
RUN dotnet build "./ProperTea.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProperTea.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProperTea.ApiGateway.dll"]