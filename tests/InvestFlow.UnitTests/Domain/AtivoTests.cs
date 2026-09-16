using FluentAssertions;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;
using InvestFlow.Domain.Exceptions;

namespace InvestFlow.UnitTests.Domain;

public class AtivoTests
{
    [Fact]
    public void Construtor_ComDadosValidos_NormalizaTickerERegistraCriacao()
    {
        var antes = DateTime.UtcNow;

        var ativo = new Ativo(" petr4 ", " Petrobras PN ", TipoAtivo.Acao, 38.12m);

        ativo.Ticker.Should().Be("PETR4");
        ativo.Nome.Should().Be("Petrobras PN");
        ativo.Tipo.Should().Be(TipoAtivo.Acao);
        ativo.PrecoAtual.Should().Be(38.12m);
        ativo.CriadoEm.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        ativo.Ordens.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("TICKERLONGO1")]
    public void Construtor_ComTickerInvalido_LancaDomainException(string ticker)
    {
        var acao = () => new Ativo(ticker, "Nome", TipoAtivo.Acao, 10m);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Construtor_ComTickerDeDezCaracteres_Aceita()
    {
        var ativo = new Ativo("ABCDEFGHIJ", "Nome", TipoAtivo.Derivativo, 1m);

        ativo.Ticker.Should().HaveLength(Ativo.TickerMaxLength);
    }

    [Fact]
    public void Construtor_SemNome_LancaDomainException()
    {
        var acao = () => new Ativo("VALE3", " ", TipoAtivo.Acao, 10m);

        acao.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Construtor_ComPrecoNaoPositivo_LancaDomainException(decimal preco)
    {
        var acao = () => new Ativo("VALE3", "Vale ON", TipoAtivo.Acao, preco);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Construtor_ComTipoInvalido_LancaDomainException()
    {
        var acao = () => new Ativo("VALE3", "Vale ON", (TipoAtivo)0, 10m);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Atualizar_ComDadosValidos_AlteraNomeTipoEPreco()
    {
        var ativo = new Ativo("HGLG11", "CSHG Logística", TipoAtivo.Acao, 160m);

        ativo.Atualizar("CSHG Logística FII", TipoAtivo.FII, 162.35m);

        ativo.Nome.Should().Be("CSHG Logística FII");
        ativo.Tipo.Should().Be(TipoAtivo.FII);
        ativo.PrecoAtual.Should().Be(162.35m);
        ativo.Ticker.Should().Be("HGLG11");
    }

    [Fact]
    public void AtualizarPreco_ComValorNaoPositivo_LancaDomainExceptionEMantemPreco()
    {
        var ativo = new Ativo("ITUB4", "Itaú PN", TipoAtivo.Acao, 33m);

        var acao = () => ativo.AtualizarPreco(0m);

        acao.Should().Throw<DomainException>();
        ativo.PrecoAtual.Should().Be(33m);
    }
}
