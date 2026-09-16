using FluentAssertions;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;
using InvestFlow.Domain.Filters;
using InvestFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InvestFlow.IntegrationTests.Infrastructure;

public class AtivoRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetPagedAsync_RetornaPaginaOrdenadaPorTickerETotal()
    {
        await using var context = _database.CreateContext();
        var repository = new AtivoRepository(context);

        var (pagina1, total) = await repository.GetPagedAsync(1, 2);
        var (pagina3, _) = await repository.GetPagedAsync(3, 2);

        total.Should().Be(5);
        pagina1.Select(a => a.Ticker).Should().Equal("HGLG11", "IPCA2035");
        pagina3.Select(a => a.Ticker).Should().Equal("WINZ26");
    }

    [Fact]
    public async Task GetPagedAsync_PaginaAlemDoFim_RetornaVazioComTotal()
    {
        await using var context = _database.CreateContext();
        var repository = new AtivoRepository(context);

        var (items, total) = await repository.GetPagedAsync(10, 10);

        items.Should().BeEmpty();
        total.Should().Be(5);
    }

    [Fact]
    public async Task GetPagedAsync_FiltraPorTipo()
    {
        await using var context = _database.CreateContext();
        var repository = new AtivoRepository(context);

        var (items, total) = await repository.GetPagedAsync(1, 10, new AtivoFiltro(Tipo: TipoAtivo.Acao));

        total.Should().Be(2);
        items.Should().OnlyContain(a => a.Tipo == TipoAtivo.Acao);
    }

    [Fact]
    public async Task GetPagedAsync_FiltraPorParteDoTickerSemDiferenciarCaixa()
    {
        await using var context = _database.CreateContext();
        var repository = new AtivoRepository(context);

        var (items, total) = await repository.GetPagedAsync(1, 10, new AtivoFiltro(Ticker: "petr"));

        total.Should().Be(1);
        items.Single().Ticker.Should().Be("PETR4");
    }

    [Fact]
    public async Task GetPagedAsync_NaoRastreiaEntidades()
    {
        await using var context = _database.CreateContext();
        var repository = new AtivoRepository(context);

        await repository.GetPagedAsync(1, 10);

        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public async Task GetPagedAsync_ComParametrosInvalidos_LancaArgumentOutOfRange(int pageNumber, int pageSize)
    {
        await using var context = _database.CreateContext();
        var repository = new AtivoRepository(context);

        var acao = () => repository.GetPagedAsync(pageNumber, pageSize);

        await acao.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetByIdAsync_RetornaAtivoSemRastrear()
    {
        await using var context = _database.CreateContext();
        var repository = new AtivoRepository(context);

        var ativo = await repository.GetByIdAsync(2);

        ativo!.Ticker.Should().Be("VALE3");
        context.Entry(ativo).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task GetByIdAsync_Inexistente_RetornaNulo()
    {
        await using var context = _database.CreateContext();

        (await new AtivoRepository(context).GetByIdAsync(999)).Should().BeNull();
    }

    [Fact]
    public async Task GetByTickerAsync_E_TickerExistsAsync_NormalizamTicker()
    {
        await using var context = _database.CreateContext();
        var repository = new AtivoRepository(context);

        (await repository.GetByTickerAsync(" hglg11 "))!.Id.Should().Be(3);
        (await repository.TickerExistsAsync("vale3")).Should().BeTrue();
        (await repository.TickerExistsAsync("XPTO3")).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_PersisteAtivoEGeraId()
    {
        var ativo = new Ativo("BBAS3", "Banco do Brasil ON", TipoAtivo.Acao, 27.9012m);

        await using (var context = _database.CreateContext())
            await new AtivoRepository(context).AddAsync(ativo);

        ativo.Id.Should().BeGreaterThan(5);
        await using var leitura = _database.CreateContext();
        var salvo = await new AtivoRepository(leitura).GetByTickerAsync("BBAS3");
        salvo!.PrecoAtual.Should().Be(27.9012m);
    }

    [Fact]
    public async Task UpdateAsync_ComEntidadeRastreada_PersisteAlteracao()
    {
        await using (var context = _database.CreateContext())
        {
            var repository = new AtivoRepository(context);
            var ativo = await repository.GetByIdForUpdateAsync(1);
            ativo!.AtualizarPreco(40.5555m);
            await repository.UpdateAsync(ativo);
        }

        await using var leitura = _database.CreateContext();
        (await new AtivoRepository(leitura).GetByIdAsync(1))!.PrecoAtual.Should().Be(40.5555m);
    }

    [Fact]
    public async Task UpdateAsync_ComEntidadeNaoRastreada_PersisteAlteracao()
    {
        await using (var context = _database.CreateContext())
        {
            var repository = new AtivoRepository(context);
            var ativo = await repository.GetByIdAsync(3);
            ativo!.Atualizar("CSHG Logística", TipoAtivo.FII, 170m);
            await repository.UpdateAsync(ativo);
        }

        await using var leitura = _database.CreateContext();
        var salvo = await new AtivoRepository(leitura).GetByIdAsync(3);
        salvo!.Nome.Should().Be("CSHG Logística");
        salvo.PrecoAtual.Should().Be(170m);
    }

    [Fact]
    public async Task DeleteAsync_AtivoSemOrdens_RemoveDoBanco()
    {
        var ativo = new Ativo("TEMP3", "Temporário", TipoAtivo.Acao, 1m);
        await using (var context = _database.CreateContext())
        {
            var repository = new AtivoRepository(context);
            await repository.AddAsync(ativo);
            await repository.DeleteAsync(ativo);
        }

        await using var leitura = _database.CreateContext();
        (await new AtivoRepository(leitura).ExistsAsync(ativo.Id)).Should().BeFalse();
    }
}
