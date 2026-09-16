using Microsoft.EntityFrameworkCore;

namespace InvestFlow.Infrastructure.Persistence;

/// <summary>
/// Contexto do provider SQLite. Existe para que as migrations de SQLite fiquem separadas das de
/// SQL Server (os tipos de coluna gerados são diferentes em cada provider).
/// </summary>
public class SqliteAppDbContext : AppDbContext
{
    public SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : base(options)
    {
    }
}
