using InvestFlow.Domain.Entities;
using InvestFlow.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestFlow.Infrastructure.Persistence.Configurations;

public class OrdemConfiguration : IEntityTypeConfiguration<Ordem>
{
    public void Configure(EntityTypeBuilder<Ordem> builder)
    {
        builder.ToTable("Ordens");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Lado)
            .IsRequired();

        builder.Property(o => o.Quantidade)
            .IsRequired();

        // Campo monetário: decimal(18,4) evita arredondamento de preços com 4 casas.
        builder.Property(o => o.PrecoExecucao)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(o => o.DataExecucao)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired();

        // Calculado em memória (Quantidade * PrecoExecucao); não vira coluna.
        builder.Ignore(o => o.ValorFinanceiro);

        // ÍNDICE em AtivoId (chave estrangeira):
        // - Consultas "ordens de um ativo" (filtro OrdemFiltro.AtivoId) e o JOIN com Ativos
        //   passam a usar seek no índice em vez de varrer toda a tabela de ordens, que é a que mais cresce.
        // - A verificação antes de excluir um ativo (ExistsByAtivoIdAsync / FK Restrict) também usa o índice.
        // - O SQL Server não cria índice em FK automaticamente; declaramos explicitamente para
        //   não depender de convenção do EF.
        builder.HasIndex(o => o.AtivoId)
            .HasDatabaseName("IX_Ordens_AtivoId");

        // ÍNDICE COMPOSTO (DataExecucao, Status):
        // - A listagem paginada ordena por DataExecucao e filtra por período (DataInicio/DataFim);
        //   com DataExecucao como primeira coluna o banco percorre o índice já ordenado, e o
        //   Skip/Take não precisa ordenar a tabela inteira a cada página.
        // - Status como segunda coluna permite avaliar o filtro de status (ex.: pendentes do mês)
        //   dentro do próprio índice, sem ler as linhas que serão descartadas.
        builder.HasIndex(o => new { o.DataExecucao, o.Status })
            .HasDatabaseName("IX_Ordens_DataExecucao_Status");

        builder.HasData(SeedData.Ordens());
    }
}
