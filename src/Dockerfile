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

# Copy source files (excluding Data folder for now - not needed for build)
COPY ["src/Program.cs", "src/"]
COPY ["src/appsettings*.json", "src/"]
COPY ["src/Constants/", "src/Constants/"]
COPY ["src/Controllers/", "src/Controllers/"]
COPY ["src/Extensions/", "src/Extensions/"]
COPY ["src/Helpers/", "src/Helpers/"]
COPY ["src/Middleware/", "src/Middleware/"]
COPY ["src/Models/", "src/Models/"]
COPY ["src/Services/", "src/Services/"]
COPY ["src/Utilities/", "src/Utilities/"]
COPY ["src/Properties/", "src/Properties/"]

RUN dotnet build "src/CurrencyArchiveAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/CurrencyArchiveAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy Data folder into final image (after build to optimize layer caching)
COPY ["src/Data/", "./Data/"]

# Heroku dynamically assigns the PORT environment variable
ENV ASPNETCORE_ENVIRONMENT=Production

# Use CMD with shell form to allow environment variable expansion
CMD ASPNETCORE_URLS=http://+:$PORT dotnet CurrencyArchiveAPI.dll