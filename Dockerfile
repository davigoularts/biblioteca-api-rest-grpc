# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Estágio de build: restaura, compila e publica com o SDK .NET 8.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia só os manifests primeiro para aproveitar o cache do restore.
COPY Biblioteca.sln ./
COPY src/Biblioteca.Dominio/Biblioteca.Dominio.csproj      src/Biblioteca.Dominio/
COPY src/Biblioteca.Repositorio/Biblioteca.Repositorio.csproj src/Biblioteca.Repositorio/
COPY src/Biblioteca.Api/Biblioteca.Api.csproj              src/Biblioteca.Api/
COPY tests/Biblioteca.Dominio.Testes/Biblioteca.Dominio.Testes.csproj tests/Biblioteca.Dominio.Testes/
RUN dotnet restore Biblioteca.sln

COPY . .
RUN dotnet publish src/Biblioteca.Api/Biblioteca.Api.csproj \
      -c Release \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Estágio de testes: alvo opcional (docker build --target test .)
# ---------------------------------------------------------------------------
FROM build AS test
RUN dotnet test tests/Biblioteca.Dominio.Testes/Biblioteca.Dominio.Testes.csproj \
      -c Release --no-restore

# ---------------------------------------------------------------------------
# Imagem final: apenas o runtime ASP.NET.
# 8080 = REST/Swagger (HTTP/1.1)   8081 = gRPC (HTTP/2 em texto claro, h2c)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# As portas são definidas em Program.cs (ConfigureKestrel); zerar esta variável
# evita o aviso de "overriding address" do Kestrel.
ENV ASPNETCORE_HTTP_PORTS=""
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_gcServer=0

EXPOSE 8080
EXPOSE 8081

USER $APP_UID

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Biblioteca.Api.dll"]
