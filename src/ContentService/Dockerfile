FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/SharedContracts/SharedContracts.csproj", "SharedContracts/"]
COPY ["src/ContentService/ContentService.Domain/ContentService.Domain.csproj", "ContentService/ContentService.Domain/"]
COPY ["src/ContentService/ContentService.Application/ContentService.Application.csproj", "ContentService/ContentService.Application/"]
COPY ["src/ContentService/ContentService.Infrastructure/ContentService.Infrastructure.csproj", "ContentService/ContentService.Infrastructure/"]
COPY ["src/ContentService/ContentService.API/ContentService.API.csproj", "ContentService/ContentService.API/"]
COPY ["NuGet.config", "."]

RUN dotnet restore "ContentService/ContentService.API/ContentService.API.csproj"

COPY src/SharedContracts/ SharedContracts/
COPY src/ContentService/ ContentService/

RUN dotnet build "ContentService/ContentService.API/ContentService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ContentService/ContentService.API/ContentService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContentService.API.dll"]
