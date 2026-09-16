using FluentAssertions;
using InvestFlow.Application.DTOs.Ativos;
using InvestFlow.Application.Exceptions;
using InvestFlow.Application.Services;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;
using InvestFlow.Domain.Exceptions;
using InvestFlow.Domain.Filters;
using InvestFlow.Domain.Repositories;
using Moq;

namespace InvestFlow.UnitTests.Application.Services;

public class AtivoServiceTests
{
    private readonly Mock<IAtivoRepository> _ativoRepository = new(MockBehavior.Strict);
    private readonly Mock<IOrdemRepository> _ordemRepository = new(MockBehavior.Strict);
    private readonly AtivoService _service;

    public AtivoServiceTests()
    {
        _service = new AtivoService(_ativoRepository.Object, _ordemRepository.Object, TestData.CriarMapper());
    }

    private static AtivoCreateRequest CreateRequestValido() => new()
    {
        Ticker = " bbas3 ",
        Nome = "Banco do Brasil ON",
        Tipo = TipoAtivo.Acao,
        PrecoAtual = 27.9012m,
    };

    [Fact]
    public async Task GetPagedAsync_RepassaPaginacaoEFiltrosEMontaPagedResult()
    {
        var ativos = new List<Ativo> { TestData.CriarAtivo(1, "PETR4"), TestData.CriarAtivo(2, "PRIO3", "PetroRio ON") };
        _ativoRepository
            .Setup(r => r.GetPagedAsync(2, 2, new AtivoFiltro("petr", TipoAtivo.Acao), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ativos, 5));

        var resultado = await _service.GetPagedAsync(
            new AtivoQuery { PageNumber = 2, PageSize = 2, Busca = "petr", Tipo = TipoAtivo.Acao });

        resultado.Items.Select(a => a.Ticker).Should().Equal("PETR4", "PRIO3");
        resultado.PageNumber.Should().Be(2);
        resultado.PageSize.Should().Be(2);
        resultado.TotalCount.Should().Be(5);
        resultado.TotalPages.Should().Be(3);
        resultado.HasPrevious.Should().BeTrue();
        resultado.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_SemParametros_UsaPaginaUmComDezItensESemFiltros()
    {
        _ativoRepository
            .Setup(r => r.GetPagedAsync(1, 10, new AtivoFiltro(null, null), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Ativo>(), 0));

        var resultado = await _service.GetPagedAsync(new AtivoQuery());

        resultado.Items.Should().BeEmpty();
        resultado.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_ComPageSizeAcimaDe50_LancaValidationExceptionSemConsultar()
    {
        var acao = () => _service.GetPagedAsync(new AtivoQuery { PageSize = 51 });

        await acao.Should().ThrowAsync<ValidationException>();
        _ativoRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_Existente_RetornaResponse()
    {
        _ativoRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.CriarAtivo(1, "PETR4"));

        var response = await _service.GetByIdAsync(1);

        response.Id.Should().Be(1);
        response.Ticker.Should().Be("PETR4");
    }

    [Fact]
    public async Task GetByIdAsync_Inexistente_LancaNotFoundException()
    {
        _ativoRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Ativo?)null);

        var acao = () => _service.GetByIdAsync(99);

        await acao.Should().ThrowAsync<NotFoundException>().WithMessage("*99*");
    }

    [Fact]
    public async Task CreateAsync_Valido_PersisteAtivoNormalizadoERetornaResponse()
    {
        Ativo? adicionado = null;
        _ativoRepository.Setup(r => r.TickerExistsAsync(" bbas3 ", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _ativoRepository.Setup(r => r.AddAsync(It.IsAny<Ativo>(), It.IsAny<CancellationToken>()))
            .Callback<Ativo, CancellationToken>((a, _) => adicionado = TestData.ComId(a, 6))
            .Returns(Task.CompletedTask);

        var response = await _service.CreateAsync(CreateRequestValido());

        adicionado.Should().NotBeNull();
        response.Id.Should().Be(6);
        response.Ticker.Should().Be("BBAS3");
        response.Nome.Should().Be("Banco do Brasil ON");
        response.PrecoAtual.Should().Be(27.9012m);
    }

    [Fact]
    public async Task CreateAsync_TickerJaCadastrado_LancaValidationExceptionSemPersistir()
    {
        _ativoRepository.Setup(r => r.TickerExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var acao = () => _service.CreateAsync(CreateRequestValido());

        var erro = (await acao.Should().ThrowAsync<ValidationException>()).Which;
        erro.Errors[nameof(AtivoCreateRequest.Ticker)].Should().ContainSingle().Which.Should().Contain("BBAS3");
        _ativoRepository.Verify(r => r.AddAsync(It.IsAny<Ativo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RequestInvalido_LancaValidationExceptionSemAcessarRepositorio()
    {
        var request = CreateRequestValido();
        request.PrecoAtual = 0m;

        var acao = () => _service.CreateAsync(request);

        await acao.Should().ThrowAsync<ValidationException>();
        _ativoRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_Existente_AtualizaEntidadeEPersiste()
    {
        var ativo = TestData.CriarAtivo(3, "HGLG11", "CSHG Log");
        _ativoRepository.Setup(r => r.GetByIdForUpdateAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(ativo);
        _ativoRepository.Setup(r => r.UpdateAsync(ativo, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var response = await _service.UpdateAsync(3, new AtivoUpdateRequest
        {
            Nome = "CSHG Logística FII",
            Tipo = TipoAtivo.FII,
            PrecoAtual = 170m,
        });

        _ativoRepository.Verify(r => r.UpdateAsync(ativo, It.IsAny<CancellationToken>()), Times.Once);
        ativo.Nome.Should().Be("CSHG Logística FII");
        response.Tipo.Should().Be(TipoAtivo.FII);
        response.PrecoAtual.Should().Be(170m);
        response.Ticker.Should().Be("HGLG11");
    }

    [Fact]
    public async Task UpdateAsync_Inexistente_LancaNotFoundException()
    {
        _ativoRepository.Setup(r => r.GetByIdForUpdateAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Ativo?)null);

        var acao = () => _service.UpdateAsync(99, new AtivoUpdateRequest { Nome = "X", Tipo = TipoAtivo.Acao, PrecoAtual = 1m });

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_RequestInvalido_LancaValidationExceptionAntesDeBuscar()
    {
        var acao = () => _service.UpdateAsync(1, new AtivoUpdateRequest { Nome = "", Tipo = TipoAtivo.Acao, PrecoAtual = 1m });

        await acao.Should().ThrowAsync<ValidationException>();
        _ativoRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_AtivoSemOrdens_Remove()
    {
        var ativo = TestData.CriarAtivo(4);
        _ativoRepository.Setup(r => r.GetByIdForUpdateAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(ativo);
        _ordemRepository.Setup(r => r.ExistsByAtivoIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _ativoRepository.Setup(r => r.DeleteAsync(ativo, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteAsync(4);

        _ativoRepository.Verify(r => r.DeleteAsync(ativo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AtivoComOrdens_LancaDomainExceptionSemRemover()
    {
        _ativoRepository.Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.CriarAtivo(1));
        _ordemRepository.Setup(r => r.ExistsByAtivoIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var acao = () => _service.DeleteAsync(1);

        await acao.Should().ThrowAsync<DomainException>().WithMessage("*possui ordens*");
        _ativoRepository.Verify(r => r.DeleteAsync(It.IsAny<Ativo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Inexistente_LancaNotFoundException()
    {
        _ativoRepository.Setup(r => r.GetByIdForUpdateAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Ativo?)null);

        var acao = () => _service.DeleteAsync(99);

        await acao.Should().ThrowAsync<NotFoundException>();
        _ordemRepository.VerifyNoOtherCalls();
    }
}
