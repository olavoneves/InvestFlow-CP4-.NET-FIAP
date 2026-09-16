using FluentAssertions;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;
using InvestFlow.Infrastructure.Persistence;
using InvestFlow.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace InvestFlow.IntegrationTests.Infrastructure;

public class AppDbContextTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Migrate_AplicaSeedCom5AtivosE60Ordens()
    {
        await using var context = _database.CreateContext();

        (await context.Ativos.CountAsync()).Should().Be(5);
        (await context.Ordens.CountAsync()).Should().Be(SeedData.TotalOrdens);
        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Seed_ContemTodosOsTiposDeAtivoEStatusDeOrdem()
    {
        await using var context = _database.CreateContext();

        var tipos = await context.Ativos.Select(a => a.Tipo).Distinct().ToListAsync();
        var status = await context.Ordens.Select(o => o.Status).Distinct().ToListAsync();

        tipos.Should().BeEquivalentTo(Enum.GetValues<TipoAtivo>());
        status.Should().BeEquivalentTo(Enum.GetValues<StatusOrdem>());
    }

    [Fact]
    public async Task Seed_OrdensRespeitamRegrasDoDominio()
    {
        await using var context = _database.CreateContext();

        var ordens = await context.Ordens.AsNoTracking().ToListAsync();

        ordens.Should().OnlyContain(o => o.Quantidade > 0 && o.PrecoExecucao > 0);
    }

    [Fact]
    public async Task Migrate_PreservaQuatroCasasDecimais()
    {
        await using var context = _database.CreateContext();

        var ordem = await context.Ordens.AsNoTracking().SingleAsync(o => o.Id == 1);

        ordem.PrecoExecucao.Should().Be(38.3487m);
    }

    [Fact]
    public void Modelo_DefineIndiceUnicoEmTicker()
    {
        using var context = _database.CreateContext();

        var indice = Indice<Ativo>(context, nameof(Ativo.Ticker));

        indice.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void Modelo_DefineIndiceEmOrdemAtivoId()
    {
        using var context = _database.CreateContext();

        Indice<Ordem>(context, nameof(Ordem.AtivoId)).IsUnique.Should().BeFalse();
    }

    [Fact]
    public void Modelo_DefineIndiceCompostoEmDataExecucaoEStatus()
    {
        using var context = _database.CreateContext();

        Indice<Ordem>(context, nameof(Ordem.DataExecucao), nameof(Ordem.Status)).Should().NotBeNull();
    }

    [Fact]
    public void Modelo_NaoMapeiaValorFinanceiro()
    {
        using var context = _database.CreateContext();

        context.Model.FindEntityType(typeof(Ordem))!
            .FindProperty(nameof(Ordem.ValorFinanceiro))
            .Should().BeNull();
    }

    [Theory]
    [InlineData(typeof(Ativo), nameof(Ativo.PrecoAtual))]
    [InlineData(typeof(Ordem), nameof(Ordem.PrecoExecucao))]
    public void ModeloSqlServer_UsaDecimal18_4NosCamposMonetarios(Type entidade, string propriedade)
    {
        // UseSqlServer só configura o provider; nenhuma conexão é aberta.
        var options = new DbContextOptionsBuilder<SqlServerAppDbContext>()
            .UseSqlServer("Server=localhost;Database=InvestFlowModelo;Trusted_Connection=True")
            .Options;
        using var context = new SqlServerAppDbContext(options);

        var coluna = context.Model.FindEntityType(entidade)!.FindProperty(propriedade)!;

        coluna.GetColumnType().Should().Be("decimal(18,4)");
    }

    [Fact]
    public async Task InserirTickerDuplicado_ViolaIndiceUnico()
    {
        await using var context = _database.CreateContext();
        context.Ativos.Add(new Ativo("petr4", "Petrobras duplicado", TipoAtivo.Acao, 10m));

        var acao = () => context.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ExcluirAtivoComOrdens_ERestritoPelaChaveEstrangeira()
    {
        await using var context = _database.CreateContext();
        var ativo = await context.Ativos.SingleAsync(a => a.Id == 1);
        context.Ativos.Remove(ativo);

        var acao = () => context.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IIndex Indice<T>(AppDbContext context, params string[] colunas)
    {
        var indice = context.Model.FindEntityType(typeof(T))!
            .GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual(colunas));

        indice.Should().NotBeNull($"deve existir índice em {typeof(T).Name}({string.Join(", ", colunas)})");
        return indice!;
    }
}
