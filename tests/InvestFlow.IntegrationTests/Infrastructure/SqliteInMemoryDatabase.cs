using InvestFlow.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InvestFlow.IntegrationTests.Infrastructure;

/// <summary>
/// Banco SQLite em memória com as migrations reais aplicadas (esquema, índices e seed).
/// Cada instância é um banco isolado, que existe enquanto a conexão estiver aberta.
/// </summary>
public sealed class SqliteInMemoryDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteInMemoryDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.Migrate();
    }

    /// <summary>Novo contexto sobre o mesmo banco — simula uma nova requisição (change tracker vazio).</summary>
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SqliteAppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new SqliteAppDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
