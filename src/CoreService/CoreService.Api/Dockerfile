#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/CoreService/CoreService.Api/CoreService.Api.csproj", "src/CoreService/CoreService.Api/"]
RUN dotnet restore "src/CoreService/CoreService.Api/CoreService.Api.csproj" #--configfile NuGet.config
COPY ["src/Shared", "src/Shared/"]
COPY ["src/CoreService", "src/CoreService/"]
WORKDIR src/CoreService/CoreService.Api
RUN dotnet build "./CoreService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CoreService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CoreService.Api.dll"]