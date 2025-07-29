FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/ChoreographedSagaCoordinator/ChoreographedSagaCoordinator.csproj", "services/ChoreographedSagaCoordinator/"]
COPY ["shared/CornerShop.Shared/CornerShop.Shared.csproj", "shared/CornerShop.Shared/"]
RUN dotnet restore "services/ChoreographedSagaCoordinator/ChoreographedSagaCoordinator.csproj"
COPY . .
WORKDIR "/src/services/ChoreographedSagaCoordinator"
RUN dotnet build "ChoreographedSagaCoordinator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChoreographedSagaCoordinator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChoreographedSagaCoordinator.dll"] 