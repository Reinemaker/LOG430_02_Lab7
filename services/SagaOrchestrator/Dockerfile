FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared library first
COPY ["shared/CornerShop.Shared/CornerShop.Shared.csproj", "shared/CornerShop.Shared/"]
RUN dotnet restore "shared/CornerShop.Shared/CornerShop.Shared.csproj"

# Copy and build Saga Orchestrator
COPY ["services/SagaOrchestrator/SagaOrchestrator.csproj", "services/SagaOrchestrator/"]
RUN dotnet restore "services/SagaOrchestrator/SagaOrchestrator.csproj"

COPY . .
WORKDIR "/src/services/SagaOrchestrator"
RUN dotnet build "SagaOrchestrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SagaOrchestrator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SagaOrchestrator.dll"] 