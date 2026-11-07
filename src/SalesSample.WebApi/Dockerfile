# ============================================================
# SalesSample.WebApi — multi-stage
# build: docker build -t salessample-api -f src/SalesSample.WebApi/Dockerfile .
# ============================================================

# ---------- 1. BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia SÓ os .csproj (cache do restore entre builds)
COPY src/SalesSample.Domain/SalesSample.Domain.csproj               src/SalesSample.Domain/
COPY src/SalesSample.Application/SalesSample.Application.csproj     src/SalesSample.Application/
COPY src/SalesSample.Infrastructure/SalesSample.Infrastructure.csproj src/SalesSample.Infrastructure/
COPY src/SalesSample.WebApi/SalesSample.WebApi.csproj               src/SalesSample.WebApi/

RUN dotnet restore src/SalesSample.WebApi/SalesSample.WebApi.csproj

# Copia o código e publica em Release
COPY . .
RUN dotnet publish src/SalesSample.WebApi/SalesSample.WebApi.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- 2. RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl (healthcheck) + kerberos do librdkafka (elimina o erro libgssapi)
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# .NET 8+ já traz o usuário não-root "app"
USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "SalesSample.WebApi.dll"]