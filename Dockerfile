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
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyProject.dll"]
