FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/SharedContracts/SharedContracts.csproj", "SharedContracts/"]
COPY ["src/UserService/UserService.Domain/UserService.Domain.csproj", "UserService/UserService.Domain/"]
COPY ["src/UserService/UserService.Application/UserService.Application.csproj", "UserService/UserService.Application/"]
COPY ["src/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj", "UserService/UserService.Infrastructure/"]
COPY ["src/UserService/UserService.API/UserService.API.csproj", "UserService/UserService.API/"]
COPY ["NuGet.config", "."]

RUN dotnet restore "UserService/UserService.API/UserService.API.csproj"

COPY src/SharedContracts/ SharedContracts/
COPY src/UserService/ UserService/

RUN dotnet build "UserService/UserService.API/UserService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserService/UserService.API/UserService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.API.dll"]
