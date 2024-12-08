FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5001


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Apis/TherapyHub.Api/TherapyHub.Api.csproj", "src/Apis/TherapyHub.Api/"]
RUN dotnet restore "./src/Apis/TherapyHub.Api/TherapyHub.Api.csproj"
COPY . .
WORKDIR "/src/src/Apis/TherapyHub.Api"
RUN dotnet build "./TherapyHub.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TherapyHub.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TherapyHub.Api.dll"]