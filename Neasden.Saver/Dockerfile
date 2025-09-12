FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy entire project folder
COPY Neasden.Saver/ ./Neasden.Saver/

WORKDIR /src/Neasden.Saver
RUN dotnet restore Neasden.Saver.csproj
RUN dotnet build Neasden.Saver.csproj -c $BUILD_CONFIGURATION -o /app/build
RUN dotnet publish Neasden.Saver.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Neasden.Saver.dll"]
