# 1. Build the App
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyProject.csproj", "./"]
RUN dotnet restore "MyProject.csproj"
COPY . .
RUN dotnet publish "MyProject.csproj" -c Release -o /app/publish

# 2. Run the App
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# --- SURGICAL ADDITION: Install Chromium and Linux dependencies ---
RUN apt-get update && apt-get install -y \
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
    && rm -rf /var/lib/apt/lists/*
# ------------------------------------------------------------------

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyProject.dll"]