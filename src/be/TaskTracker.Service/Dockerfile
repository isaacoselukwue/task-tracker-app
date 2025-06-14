FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/be/TaskTracker.Service/TaskTracker.Service.csproj", "src/be/TaskTracker.Service/"]
COPY ["src/be/TaskTracker.Application/TaskTracker.Application.csproj", "src/be/TaskTracker.Application/"]
COPY ["src/be/TaskTracker.Domain/TaskTracker.Domain.csproj", "src/be/TaskTracker.Domain/"]
COPY ["src/be/TaskTracker.Infrastructure/TaskTracker.Infrastructure.csproj", "src/be/TaskTracker.Infrastructure/"]

RUN dotnet restore "src/be/TaskTracker.Service/TaskTracker.Service.csproj"

COPY . .

WORKDIR "/src/src/be/TaskTracker.Service"
RUN dotnet build "TaskTracker.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "TaskTracker.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TaskTracker.Service.dll"]