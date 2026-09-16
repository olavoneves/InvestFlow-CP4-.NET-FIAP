using FluentAssertions;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;
using InvestFlow.Domain.Exceptions;

namespace InvestFlow.UnitTests.Domain;

public class OrdemTests
{
    private static readonly DateTime Data = new(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc);

    private static Ordem CriarOrdemValida() => new(1, LadoOrdem.Compra, 100, 32.50m, Data);

    [Fact]
    public void Construtor_ComDadosValidos_CriaOrdemPendente()
    {
        var ordem = CriarOrdemValida();

        ordem.AtivoId.Should().Be(1);
        ordem.Lado.Should().Be(LadoOrdem.Compra);
        ordem.Quantidade.Should().Be(100);
        ordem.PrecoExecucao.Should().Be(32.50m);
        ordem.DataExecucao.Should().Be(Data);
        ordem.Status.Should().Be(StatusOrdem.Pendente);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_ComQuantidadeNaoPositiva_LancaDomainException(int quantidade)
    {
        var acao = () => new Ordem(1, LadoOrdem.Compra, quantidade, 10m, Data);

        acao.Should().Throw<DomainException>().WithMessage("*quantidade*maior que zero*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Construtor_ComPrecoExecucaoNaoPositivo_LancaDomainException(decimal preco)
    {
        var acao = () => new Ordem(1, LadoOrdem.Venda, 10, preco, Data);

        acao.Should().Throw<DomainException>().WithMessage("*preço de execução*maior que zero*");
    }

    [Fact]
    public void Construtor_ComAtivoIdInvalido_LancaDomainException()
    {
        var acao = () => new Ordem(0, LadoOrdem.Compra, 10, 10m, Data);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Construtor_ComLadoInvalido_LancaDomainException()
    {
        var acao = () => new Ordem(1, (LadoOrdem)99, 10, 10m, Data);

        acao.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(100, 32.50, 3250.00)]
    [InlineData(3, 0.3333, 0.9999)]
    [InlineData(1, 1234.5678, 1234.5678)]
    public void ValorFinanceiro_RetornaQuantidadeVezesPrecoExecucao(int quantidade, decimal preco, decimal esperado)
    {
        var ordem = new Ordem(1, LadoOrdem.Compra, quantidade, preco, Data);

        ordem.ValorFinanceiro.Should().Be(esperado);
    }

    [Fact]
    public void Cancelar_OrdemPendente_AlteraStatusParaCancelada()
    {
        var ordem = CriarOrdemValida();

        ordem.Cancelar();

        ordem.Status.Should().Be(StatusOrdem.Cancelada);
    }

    [Fact]
    public void Cancelar_OrdemExecutada_LancaDomainException()
    {
        var ordem = CriarOrdemValida();
        ordem.Executar();

        var acao = () => ordem.Cancelar();

        acao.Should().Throw<DomainException>().WithMessage("*já executada*");
        ordem.Status.Should().Be(StatusOrdem.Executada);
    }

    [Fact]
    public void Cancelar_OrdemJaCancelada_LancaDomainException()
    {
        var ordem = CriarOrdemValida();
        ordem.Cancelar();

        var acao = () => ordem.Cancelar();

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Executar_OrdemPendente_AlteraStatusParaExecutada()
    {
        var ordem = CriarOrdemValida();

        ordem.Executar();

        ordem.Status.Should().Be(StatusOrdem.Executada);
    }

    [Fact]
    public void Executar_OrdemCancelada_LancaDomainException()
    {
        var ordem = CriarOrdemValida();
        ordem.Cancelar();

        var acao = () => ordem.Executar();

        acao.Should().Throw<DomainException>();
        ordem.Status.Should().Be(StatusOrdem.Cancelada);
    }

    [Fact]
    public void AlterarQuantidade_OrdemPendenteComValorValido_AtualizaQuantidade()
    {
        var ordem = CriarOrdemValida();

        ordem.AlterarQuantidade(250);

        ordem.Quantidade.Should().Be(250);
    }

    [Fact]
    public void AlterarQuantidade_ComValorNaoPositivo_LancaDomainException()
    {
        var ordem = CriarOrdemValida();

        var acao = () => ordem.AlterarQuantidade(0);

        acao.Should().Throw<DomainException>();
        ordem.Quantidade.Should().Be(100);
    }

    [Fact]
    public void AlterarPrecoExecucao_ComValorNaoPositivo_LancaDomainException()
    {
        var ordem = CriarOrdemValida();

        var acao = () => ordem.AlterarPrecoExecucao(-5m);

        acao.Should().Throw<DomainException>();
        ordem.PrecoExecucao.Should().Be(32.50m);
    }

    [Fact]
    public void AlterarPrecoExecucao_OrdemExecutada_LancaDomainException()
    {
        var ordem = CriarOrdemValida();
        ordem.Executar();

        var acao = () => ordem.AlterarPrecoExecucao(40m);

        acao.Should().Throw<DomainException>();
    }
}
