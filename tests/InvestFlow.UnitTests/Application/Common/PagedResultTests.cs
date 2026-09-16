using FluentAssertions;
using InvestFlow.Application.Common;

namespace InvestFlow.UnitTests.Application.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(60, 10, 6)]
    [InlineData(int.MaxValue, 50, 42949673)]
    public void TotalPages_ArredondaParaCima(int totalCount, int pageSize, int esperado)
    {
        var resultado = new PagedResult<int>(Array.Empty<int>(), 1, pageSize, totalCount);

        resultado.TotalPages.Should().Be(esperado);
    }

    [Fact]
    public void PrimeiraPagina_TemProximaMasNaoAnterior()
    {
        var resultado = new PagedResult<int>(new[] { 1, 2 }, 1, 2, 5);

        resultado.HasPrevious.Should().BeFalse();
        resultado.HasNext.Should().BeTrue();
    }

    [Fact]
    public void PaginaIntermediaria_TemProximaEAnterior()
    {
        var resultado = new PagedResult<int>(new[] { 3, 4 }, 2, 2, 5);

        resultado.HasPrevious.Should().BeTrue();
        resultado.HasNext.Should().BeTrue();
    }

    [Fact]
    public void UltimaPagina_TemAnteriorMasNaoProxima()
    {
        var resultado = new PagedResult<int>(new[] { 5 }, 3, 2, 5);

        resultado.HasPrevious.Should().BeTrue();
        resultado.HasNext.Should().BeFalse();
    }

    [Fact]
    public void ListagemVazia_NaoTemProximaNemAnterior()
    {
        var resultado = new PagedResult<int>(Array.Empty<int>(), 1, 10, 0);

        resultado.TotalPages.Should().Be(0);
        resultado.HasPrevious.Should().BeFalse();
        resultado.HasNext.Should().BeFalse();
    }

    [Fact]
    public void Construtor_PreservaItensEMetadados()
    {
        var items = new[] { "a", "b" };

        var resultado = new PagedResult<string>(items, 4, 2, 9);

        resultado.Items.Should().Equal("a", "b");
        resultado.PageNumber.Should().Be(4);
        resultado.PageSize.Should().Be(2);
        resultado.TotalCount.Should().Be(9);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 10, -1)]
    public void Construtor_ComMetadadosInvalidos_LancaArgumentOutOfRange(int pageNumber, int pageSize, int totalCount)
    {
        var acao = () => new PagedResult<int>(Array.Empty<int>(), pageNumber, pageSize, totalCount);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Construtor_ComItensNulos_LancaArgumentNull()
    {
        var acao = () => new PagedResult<int>(null!, 1, 10, 0);

        acao.Should().Throw<ArgumentNullException>();
    }
}
