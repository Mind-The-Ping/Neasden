# Base runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the projects Saver depends on
COPY Neasden.Saver/Neasden.Saver.csproj ./Neasden.Saver/
COPY Neasden.Repository.Redis/Neasden.Repository.Redis.csproj ./Neasden.Repository.Redis/
COPY Neasden.Models/Neasden.Models.csproj ./Neasden.Models/
COPY Neasden.Repository/Neasden.Repository.csproj ./Neasden.Repository/

# Restore Saver project
WORKDIR /src/Neasden.Saver
RUN dotnet restore Neasden.Saver.csproj

# Copy the source code for only the necessary projects
COPY Neasden.Saver/ ./Neasden.Saver/
COPY Neasden.Repository.Redis/ ./Neasden.Repository.Redis/
COPY Neasden.Models/ ./Neasden.Models/
COPY Neasden.Repository/ ./Neasden.Repository/

# Build and publish Saver
RUN dotnet build Neasden.Saver.csproj -c $BUILD_CONFIGURATION -o /app/build
RUN dotnet publish Neasden.Saver.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Neasden.Saver.dll"]
