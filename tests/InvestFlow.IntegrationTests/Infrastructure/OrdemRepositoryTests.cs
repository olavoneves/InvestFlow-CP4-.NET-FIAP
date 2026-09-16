using FluentAssertions;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;
using InvestFlow.Domain.Filters;
using InvestFlow.Infrastructure.Persistence.Seed;
using InvestFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InvestFlow.IntegrationTests.Infrastructure;

public class OrdemRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetPagedAsync_PaginasCobremTodasAsOrdensSemRepeticao()
    {
        await using var context = _database.CreateContext();
        var repository = new OrdemRepository(context);
        var ids = new List<int>();

        for (var pagina = 1; pagina <= 6; pagina++)
        {
            var (items, total) = await repository.GetPagedAsync(pagina, 10);
            total.Should().Be(SeedData.TotalOrdens);
            items.Should().HaveCount(10);
            ids.AddRange(items.Select(o => o.Id));
        }

        ids.Should().OnlyHaveUniqueItems().And.HaveCount(SeedData.TotalOrdens);
        (await repository.GetPagedAsync(7, 10)).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPagedAsync_OrdenaDaMaisRecenteParaMaisAntigaComAtivoCarregado()
    {
        await using var context = _database.CreateContext();
        var repository = new OrdemRepository(context);

        var (items, _) = await repository.GetPagedAsync(1, 10);

        items.Should().BeInDescendingOrder(o => o.DataExecucao);
        items.First().Id.Should().Be(SeedData.TotalOrdens);
        items.Should().OnlyContain(o => o.Ativo != null && o.Ativo.Id == o.AtivoId);
    }

    [Fact]
    public async Task GetPagedAsync_NaoRastreiaEntidades()
    {
        await using var context = _database.CreateContext();

        await new OrdemRepository(context).GetPagedAsync(1, 10);

        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetPagedAsync_FiltraPorAtivoELado()
    {
        await using var context = _database.CreateContext();
        var repository = new OrdemRepository(context);

        var (items, total) = await repository.GetPagedAsync(1, 50, new OrdemFiltro(AtivoId: 1, Lado: LadoOrdem.Venda));

        total.Should().Be(items.Count).And.BeGreaterThan(0);
        items.Should().OnlyContain(o => o.AtivoId == 1 && o.Lado == LadoOrdem.Venda);
    }

    [Fact]
    public async Task GetPagedAsync_FiltraPorStatusEPeriodo()
    {
        await using var context = _database.CreateContext();
        var repository = new OrdemRepository(context);
        var inicio = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc);

        var (items, total) = await repository.GetPagedAsync(
            1, 50, new OrdemFiltro(Status: StatusOrdem.Executada, DataInicio: inicio, DataFim: fim));

        total.Should().Be(items.Count).And.BeGreaterThan(0);
        items.Should().OnlyContain(o =>
            o.Status == StatusOrdem.Executada && o.DataExecucao >= inicio && o.DataExecucao <= fim);
    }

    [Fact]
    public async Task GetByIdAsync_RetornaOrdemComAtivoSemRastrear()
    {
        await using var context = _database.CreateContext();

        var ordem = await new OrdemRepository(context).GetByIdAsync(3);

        ordem!.Ativo!.Ticker.Should().Be("HGLG11");
        ordem.ValorFinanceiro.Should().Be(ordem.Quantidade * ordem.PrecoExecucao);
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsByAtivoIdAsync_IndicaSeHaOrdensDoAtivo()
    {
        await using var context = _database.CreateContext();
        var repository = new OrdemRepository(context);

        (await repository.ExistsByAtivoIdAsync(1)).Should().BeTrue();
        (await repository.ExistsByAtivoIdAsync(999)).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_PersisteOrdemPendente()
    {
        var ordem = new Ordem(2, LadoOrdem.Compra, 300, 60.1234m, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        await using (var context = _database.CreateContext())
            await new OrdemRepository(context).AddAsync(ordem);

        await using var leitura = _database.CreateContext();
        var salva = await new OrdemRepository(leitura).GetByIdAsync(ordem.Id);
        salva!.Status.Should().Be(StatusOrdem.Pendente);
        salva.ValorFinanceiro.Should().Be(18037.02m);
    }

    [Fact]
    public async Task UpdateAsync_CancelamentoDeOrdemRastreada_EPersistido()
    {
        const int idPendente = 51;
        await using (var context = _database.CreateContext())
        {
            var repository = new OrdemRepository(context);
            var ordem = await repository.GetByIdForUpdateAsync(idPendente);
            ordem!.Status.Should().Be(StatusOrdem.Pendente);
            ordem.Cancelar();
            await repository.UpdateAsync(ordem);
        }

        await using var leitura = _database.CreateContext();
        (await new OrdemRepository(leitura).GetByIdAsync(idPendente))!.Status.Should().Be(StatusOrdem.Cancelada);
    }

    [Fact]
    public async Task UpdateAsync_ComOrdemNaoRastreada_NaoAlteraOAtivoCarregado()
    {
        const int idPendente = 52;
        await using (var context = _database.CreateContext())
        {
            var repository = new OrdemRepository(context);
            var ordem = await repository.GetByIdAsync(idPendente);
            ordem!.Executar();

            await repository.UpdateAsync(ordem);

            context.Entry(ordem.Ativo!).State.Should().Be(EntityState.Unchanged);
        }

        await using var leitura = _database.CreateContext();
        (await new OrdemRepository(leitura).GetByIdAsync(idPendente))!.Status.Should().Be(StatusOrdem.Executada);
    }

    [Fact]
    public async Task DeleteAsync_RemoveOrdem()
    {
        await using (var context = _database.CreateContext())
        {
            var repository = new OrdemRepository(context);
            var ordem = await repository.GetByIdForUpdateAsync(1);
            await repository.DeleteAsync(ordem!);
        }

        await using var leitura = _database.CreateContext();
        (await new OrdemRepository(leitura).GetByIdAsync(1)).Should().BeNull();
    }
}
