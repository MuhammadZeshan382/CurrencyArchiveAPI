#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Directory.Build.props to root (MSBuild looks for it in parent directories)
COPY ["Directory.Build.props", "/"]

# Copy project file and restore dependencies
COPY ["src/CurrencyArchiveAPI.csproj", "src/"]
RUN dotnet restore "src/CurrencyArchiveAPI.csproj"

# Copy all source files
COPY ["src/", "src/"]

RUN dotnet build "src/CurrencyArchiveAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/CurrencyArchiveAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Azure App Service uses port 8080 by default for Linux containers
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CurrencyArchiveAPI.dll"]