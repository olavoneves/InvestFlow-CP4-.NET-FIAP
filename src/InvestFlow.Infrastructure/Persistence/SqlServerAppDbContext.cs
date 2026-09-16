using Microsoft.EntityFrameworkCore;

namespace InvestFlow.Infrastructure.Persistence;

/// <summary>
/// Contexto do provider SQL Server. Existe para que as migrations de SQL Server fiquem separadas das
/// de SQLite (os tipos de coluna gerados são diferentes em cada provider).
/// </summary>
public class SqlServerAppDbContext : AppDbContext
{
    public SqlServerAppDbContext(DbContextOptions<SqlServerAppDbContext> options) : base(options)
    {
    }
}
