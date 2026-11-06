using Microsoft.EntityFrameworkCore;
using SalesSample.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SalesSample.IntegrationTests;

/// <summary>
/// FIXTURE COMPARTILHADO (IClassFixture).
///
/// Sobe UM container Postgres REAL para todos os testes da classe.
///
/// Por que NÃO usar InMemory?
///   InMemory não tem SQL, FK, índice único, jsonb, numeric nem transação —
///   o teste passa e quebra em produção. Aqui é Postgres de verdade (Docker).
///
/// Ciclo de vida (xunit v3):
///   InitializeAsync -> UMA vez, antes de todos os testes da classe.
///   DisposeAsync    -> UMA vez, no fim (derruba o container).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Testcontainers 4.16: a imagem vai no CONSTRUTOR (o construtor vazio é obsoleto).
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // 1) docker run postgres:17 + espera ficar healthy (pg_isready)
        await _postgres.StartAsync();

        // 2) host/porta aleatórios expostos pelo container
        ConnectionString = _postgres.GetConnectionString();

        // 3) aplica as MIGRATIONS reais -> schema de produção no banco de teste
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

    // Contexto NOVO por chamada = change tracker limpo (evita entidade "presa" de outro teste).
    public SalesDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);
}