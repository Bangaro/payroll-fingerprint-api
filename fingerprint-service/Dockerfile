FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5159
EXPOSE 5160

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["fingerprint-service/fingerprint-service.csproj", "fingerprint-service/"]
RUN dotnet restore "fingerprint-service/fingerprint-service.csproj"
COPY . .
WORKDIR "/src/fingerprint-service"
RUN dotnet build "fingerprint-service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "fingerprint-service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "fingerprint-service.dll"]
