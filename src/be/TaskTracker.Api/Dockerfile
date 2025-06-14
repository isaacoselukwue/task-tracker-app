FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/be/TaskTracker.Api/TaskTracker.Api.csproj", "src/be/TaskTracker.Api/"]
COPY ["src/be/TaskTracker.Application/TaskTracker.Application.csproj", "src/be/TaskTracker.Application/"]
COPY ["src/be/TaskTracker.Domain/TaskTracker.Domain.csproj", "src/be/TaskTracker.Domain/"]
COPY ["src/be/TaskTracker.Infrastructure/TaskTracker.Infrastructure.csproj", "src/be/TaskTracker.Infrastructure/"]

RUN dotnet restore "src/be/TaskTracker.Api/TaskTracker.Api.csproj"

COPY . .

WORKDIR "/src/src/be/TaskTracker.Api"
RUN dotnet build "TaskTracker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "TaskTracker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskTracker.Api.dll"]