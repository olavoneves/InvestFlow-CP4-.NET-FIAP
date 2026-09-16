using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InvestFlow.Infrastructure.Persistence.DesignTime;

// Usadas apenas pelo `dotnet ef` para gerar migrations; não conectam ao banco.
// Exemplo:
//   dotnet ef migrations add <Nome> --project src/InvestFlow.Infrastructure --startup-project src/InvestFlow.Api
//     --context SqliteAppDbContext --output-dir Persistence/Migrations/Sqlite

public class SqliteAppDbContextFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqliteAppDbContext>()
            .UseSqlite(DependencyInjection.DefaultSqliteConnectionString)
            .Options;

        return new SqliteAppDbContext(options);
    }
}

public class SqlServerAppDbContextFactory : IDesignTimeDbContextFactory<SqlServerAppDbContext>
{
    public SqlServerAppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqlServerAppDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=InvestFlow;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new SqlServerAppDbContext(options);
    }
}
