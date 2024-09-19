FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    ffmpeg \
    libavdevice-dev \
    libavfilter-dev \
    libavformat-dev \
    libavcodec-dev \
    libswscale-dev \
    libavutil-dev \
    libswresample-dev \
    libblas-dev \
    libblas3 && \
    ln -s /usr/lib/x86_64-linux-gnu/blas/libblas.so.3 /usr/lib/ && \
    ln -s /usr/lib/x86_64-linux-gnu/lapack/liblapack.so.3 /usr/lib/ && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*   
ENV PATH="/usr/bin/ffmpeg:${PATH}"

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/VideoProcessingService/VideoProcessingService.App/VideoProcessingService.App.csproj", "src/VideoProcessingService/VideoProcessingService.App/"]
COPY ["localNuGetPackages", "src/VideoProcessingService/localNuGetPackages"]
RUN dotnet nuget add source /src/src/VideoProcessingService/localNuGetPackages --name localNuGetPackages
RUN dotnet restore "src/VideoProcessingService/VideoProcessingService.App/VideoProcessingService.App.csproj"
COPY ["src/Shared", "src/Shared/"]
COPY ["src/VideoProcessingService", "src/VideoProcessingService/"]
WORKDIR src/VideoProcessingService/VideoProcessingService.App
RUN dotnet build "VideoProcessingService.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VideoProcessingService.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN if ! command -v ffmpeg > /dev/null; then \
        echo "ffmpeg not found"; \
        exit 1; \
    fi

ENTRYPOINT ["dotnet", "VideoProcessingService.App.dll"]
