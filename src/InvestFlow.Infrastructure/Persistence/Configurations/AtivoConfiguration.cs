using InvestFlow.Domain.Entities;
using InvestFlow.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestFlow.Infrastructure.Persistence.Configurations;

public class AtivoConfiguration : IEntityTypeConfiguration<Ativo>
{
    public void Configure(EntityTypeBuilder<Ativo> builder)
    {
        builder.ToTable("Ativos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Ticker)
            .HasMaxLength(Ativo.TickerMaxLength)
            .IsRequired();

        builder.Property(a => a.Nome)
            .HasMaxLength(Ativo.NomeMaxLength)
            .IsRequired();

        builder.Property(a => a.Tipo)
            .IsRequired();

        // Campo monetário: decimal(18,4) evita arredondamento de cotações com 4 casas.
        // No SQLite o EF Core persiste decimal como TEXT, preservando a precisão.
        builder.Property(a => a.PrecoAtual)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(a => a.CriadoEm)
            .IsRequired();

        // ÍNDICE ÚNICO em Ticker:
        // 1) Integridade: o ticker identifica o ativo no mercado. A checagem na aplicação
        //    (TickerExistsAsync) não impede duplicidade entre requisições concorrentes; o índice
        //    único garante a regra no próprio banco.
        // 2) Desempenho: buscas por ticker (GetByTickerAsync, TickerExistsAsync) viram seek no
        //    índice em vez de varrer a tabela inteira.
        builder.HasIndex(a => a.Ticker)
            .IsUnique()
            .HasDatabaseName("IX_Ativos_Ticker");

        builder.HasMany(a => a.Ordens)
            .WithOne(o => o.Ativo)
            .HasForeignKey(o => o.AtivoId)
            // Restrict: um ativo com histórico de ordens não pode ser apagado em cascata.
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(a => a.Ordens)
            .HasField("_ordens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(SeedData.Ativos());
    }
}
