# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file first to leverage Docker layer caching
COPY SmartELibrary.csproj ./
RUN dotnet restore SmartELibrary.csproj

# Copy source and publish
COPY . .
RUN dotnet publish SmartELibrary.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# App listens on port 10000 inside container
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartELibrary.dll"]
