using InvestFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvestFlow.Infrastructure.Persistence;

/// <summary>
/// Contexto base usado pelos repositórios. O modelo é o mesmo para todos os providers;
/// cada provider tem um contexto derivado (<see cref="SqliteAppDbContext"/>,
/// <see cref="SqlServerAppDbContext"/>) apenas para manter um conjunto de migrations próprio.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Ativo> Ativos => Set<Ativo>();

    public DbSet<Ordem> Ordens => Set<Ordem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
