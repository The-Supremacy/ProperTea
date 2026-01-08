FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080
EXPOSE 443

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["src/landlord/portal/bff/ProperTea.Landlord.Bff/ProperTea.Landlord.Bff.csproj", "src/landlord/portal/bff/ProperTea.Landlord.Bff/"]
RUN dotnet restore "src/landlord/portal/bff/ProperTea.Landlord.Bff/ProperTea.Landlord.Bff.csproj"
COPY . .
WORKDIR "/src/src/landlord/portal/bff/ProperTea.Landlord.Bff"
RUN dotnet build "ProperTea.Landlord.Bff.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "ProperTea.Landlord.Bff.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProperTea.Landlord.Bff.dll"]
