# ==================================================================
# 1. BUILD PHASE (Full Linux SDK Environment handles packages cleanly)
# ==================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Cache the NuGet restore layer
COPY ["MyProject.csproj", "./"]
RUN dotnet restore "MyProject.csproj"

# Copy remaining application source code
COPY . .

# --- SURGICAL MOVE: Install Chromium and Linux dependencies here ---
# The SDK image is a full OS container and will resolve these safely 
RUN apt-get update && apt-get install -y \
    libgdiplus \
    chromium \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    fonts-liberation \
    --no-install-recommends && \
    rm -rf /var/lib/apt/lists/*
# ------------------------------------------------------------------

# Compile and publish the web application binary assets
RUN dotnet publish "MyProject.csproj" -c Release -o /app/publish


# ==================================================================
# 2. RUNTIME PHASE (Clean, stable container execution)
# ==================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Pull the compiled app assets AND the resolved Linux binaries into the final image
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MyProject.dll"]