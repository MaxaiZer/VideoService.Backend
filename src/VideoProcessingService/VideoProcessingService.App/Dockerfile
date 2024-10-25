FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
RUN apk add --no-cache ffmpeg

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/VideoProcessingService/VideoProcessingService.App/VideoProcessingService.App.csproj", "src/VideoProcessingService/VideoProcessingService.App/"]
COPY ["src/VideoProcessingService/VideoProcessingService.Infrastructure/VideoProcessingService.Infrastructure.csproj", "src/VideoProcessingService/VideoProcessingService.Infrastructure/"]
COPY ["src/VideoProcessingService/VideoProcessingService.Core/VideoProcessingService.Core.csproj", "src/VideoProcessingService/VideoProcessingService.Core/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/VideoProcessingService/VideoProcessingService.App/VideoProcessingService.App.csproj"

COPY ["src/Shared", "src/Shared/"]
COPY ["src/VideoProcessingService", "src/VideoProcessingService/"]
WORKDIR src/VideoProcessingService/VideoProcessingService.App
RUN dotnet publish "VideoProcessingService.App.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VideoProcessingService.App.dll"]
