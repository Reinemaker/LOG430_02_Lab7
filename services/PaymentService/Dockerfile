FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared library first
COPY shared/CornerShop.Shared/CornerShop.Shared.csproj shared/CornerShop.Shared/
RUN dotnet restore shared/CornerShop.Shared/CornerShop.Shared.csproj

# Copy and restore PaymentService
COPY services/PaymentService/PaymentService.csproj services/PaymentService/
RUN dotnet restore services/PaymentService/PaymentService.csproj

# Copy source code
COPY shared/CornerShop.Shared/ shared/CornerShop.Shared/
COPY services/PaymentService/ services/PaymentService/

# Build PaymentService
WORKDIR /src/services/PaymentService
RUN dotnet build PaymentService.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish PaymentService.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"] 