FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["tests/be/TaskTracker.Tests/TaskTracker.Tests.csproj", "tests/be/TaskTracker.Tests/"]
COPY ["src/be/TaskTracker.Api/TaskTracker.Api.csproj", "src/be/TaskTracker.Api/"]
COPY ["src/be/TaskTracker.Application/TaskTracker.Application.csproj", "src/be/TaskTracker.Application/"]
COPY ["src/be/TaskTracker.Domain/TaskTracker.Domain.csproj", "src/be/TaskTracker.Domain/"]
COPY ["src/be/TaskTracker.Infrastructure/TaskTracker.Infrastructure.csproj", "src/be/TaskTracker.Infrastructure/"]

RUN dotnet restore "tests/be/TaskTracker.Tests/TaskTracker.Tests.csproj"

COPY . .

WORKDIR "/src/tests/be/TaskTracker.Tests"

ENTRYPOINT ["dotnet", "test", "--no-restore", "-c", "${BUILD_CONFIGURATION}", "--logger", "trx;LogFileName=test_results.trx", "--results-directory", "/testresults"]

VOLUME ["/testresults"]