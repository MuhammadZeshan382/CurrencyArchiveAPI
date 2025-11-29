#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CurrencyArchiveAPI.csproj", "."]
RUN dotnet restore "./CurrencyArchiveAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./CurrencyArchiveAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CurrencyArchiveAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Heroku dynamically assigns the PORT environment variable
ENV ASPNETCORE_ENVIRONMENT=Production

# Use CMD with shell form to allow environment variable expansion
CMD ASPNETCORE_URLS=http://+:$PORT dotnet CurrencyArchiveAPI.dll