using FluentAssertions;
using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ativos;
using InvestFlow.Application.DTOs.Ordens;
using InvestFlow.Application.Exceptions;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Enums;

namespace InvestFlow.UnitTests.Application.Validation;

public class RequestValidatorTests
{
    [Fact]
    public void PageRequest_UsaDefaults1E10()
    {
        var request = new PageRequest();

        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(10);
        FluentActions.Invoking(() => RequestValidator.Validar(request)).Should().NotThrow();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 50)]
    [InlineData(PageRequest.MaxPageNumber, 50)]
    public void PageRequest_NosLimites_EhValido(int pageNumber, int pageSize)
    {
        var request = new PageRequest { PageNumber = pageNumber, PageSize = pageSize };

        FluentActions.Invoking(() => RequestValidator.Validar(request)).Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PageRequest.MaxPageNumber + 1)]
    public void PageRequest_ComPageNumberForaDoLimite_LancaValidationException(int pageNumber)
    {
        var request = new PageRequest { PageNumber = pageNumber };

        var erro = FluentActions.Invoking(() => RequestValidator.Validar(request))
            .Should().Throw<ValidationException>().Which;

        erro.Errors.Should().ContainKey(nameof(PageRequest.PageNumber)).And.HaveCount(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void PageRequest_ComPageSizeForaDoLimite_LancaValidationException(int pageSize)
    {
        var request = new PageRequest { PageSize = pageSize };

        var erro = FluentActions.Invoking(() => RequestValidator.Validar(request))
            .Should().Throw<ValidationException>().Which;

        erro.Errors[nameof(PageRequest.PageSize)].Should().ContainSingle().Which.Should().Contain("50");
    }

    [Fact]
    public void AtivoCreateRequest_Invalido_AgrupaErrosPorCampo()
    {
        var request = new AtivoCreateRequest
        {
            Ticker = "   ",
            Nome = new string('x', 101),
            Tipo = (TipoAtivo)99,
            PrecoAtual = 0m,
        };

        var erro = FluentActions.Invoking(() => RequestValidator.Validar(request))
            .Should().Throw<ValidationException>().Which;

        erro.Message.Should().Be(ValidationException.MensagemPadrao);
        erro.Errors.Keys.Should().BeEquivalentTo("Ticker", "Nome", "Tipo", "PrecoAtual");
    }

    [Theory]
    [InlineData("0.0001")]
    [InlineData("99999999999999.9999")]
    public void AtivoCreateRequest_PrecoNosLimitesDaColuna_EhValido(string preco)
    {
        var request = new AtivoCreateRequest
        {
            Ticker = "PETR4",
            Nome = "Petrobras PN",
            Tipo = TipoAtivo.Acao,
            PrecoAtual = decimal.Parse(preco, System.Globalization.CultureInfo.InvariantCulture),
        };

        FluentActions.Invoking(() => RequestValidator.Validar(request)).Should().NotThrow();
    }

    [Fact]
    public void AtivoCreateRequest_PrecoAcimaDaPrecisaoDaColuna_LancaValidationException()
    {
        var request = new AtivoCreateRequest
        {
            Ticker = "PETR4",
            Nome = "Petrobras PN",
            Tipo = TipoAtivo.Acao,
            PrecoAtual = 100_000_000_000_000m,
        };

        FluentActions.Invoking(() => RequestValidator.Validar(request))
            .Should().Throw<ValidationException>()
            .Which.Errors.Should().ContainKey(nameof(AtivoCreateRequest.PrecoAtual));
    }

    [Fact]
    public void AtivoQuery_ValidaPaginacaoHerdadaEFiltros()
    {
        var query = new AtivoQuery { PageSize = 51, Tipo = (TipoAtivo)0, Busca = new string('a', 101) };

        var erro = FluentActions.Invoking(() => RequestValidator.Validar(query))
            .Should().Throw<ValidationException>().Which;

        erro.Errors.Keys.Should().BeEquivalentTo("PageSize", "Tipo", "Busca");
    }

    [Fact]
    public void OrdemQuery_ComDataInicioDepoisDaDataFim_LancaValidationException()
    {
        var query = new OrdemQuery { DataInicio = new DateTime(2026, 2, 1), DataFim = new DateTime(2026, 1, 1) };

        var erro = FluentActions.Invoking(() => RequestValidator.Validar(query))
            .Should().Throw<ValidationException>().Which;

        erro.Errors.Should().ContainKey(nameof(OrdemQuery.DataFim));
    }

    [Theory]
    [InlineData("2026-01-01", "2026-01-01")]
    [InlineData("2026-01-01", null)]
    [InlineData(null, "2026-01-01")]
    public void OrdemQuery_ComPeriodoValidoOuAberto_EhValido(string? inicio, string? fim)
    {
        var query = new OrdemQuery
        {
            DataInicio = inicio is null ? null : DateTime.Parse(inicio),
            DataFim = fim is null ? null : DateTime.Parse(fim),
        };

        FluentActions.Invoking(() => RequestValidator.Validar(query)).Should().NotThrow();
    }

    [Fact]
    public void OrdemCreateRequest_Invalido_AgrupaErrosPorCampo()
    {
        var request = new OrdemCreateRequest { AtivoId = 0, Lado = 0, Quantidade = 0, PrecoExecucao = -1m };

        var erro = FluentActions.Invoking(() => RequestValidator.Validar(request))
            .Should().Throw<ValidationException>().Which;

        erro.Errors.Keys.Should().BeEquivalentTo("AtivoId", "Lado", "Quantidade", "PrecoExecucao");
    }

    [Fact]
    public void Validar_ComRequestNulo_LancaArgumentNull()
    {
        FluentActions.Invoking(() => RequestValidator.Validar(null!)).Should().Throw<ArgumentNullException>();
    }
}
