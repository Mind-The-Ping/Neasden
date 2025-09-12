# Base runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the project file first and restore dependencies
COPY Neasden.Saver.csproj ./
RUN dotnet restore ./Neasden.Saver.csproj

# Copy the rest of the source code
COPY . .
WORKDIR /src/Neasden.Saver
RUN dotnet build ./Neasden.Saver.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish ./Neasden.Saver.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Neasden.Saver.dll"]