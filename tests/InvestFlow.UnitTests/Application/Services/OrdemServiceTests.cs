using FluentAssertions;
using InvestFlow.Application.DTOs.Ordens;
using InvestFlow.Application.Exceptions;
using InvestFlow.Application.Services;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;
using InvestFlow.Domain.Exceptions;
using InvestFlow.Domain.Filters;
using InvestFlow.Domain.Repositories;
using Moq;

namespace InvestFlow.UnitTests.Application.Services;

public class OrdemServiceTests
{
    private readonly Mock<IOrdemRepository> _ordemRepository = new(MockBehavior.Strict);
    private readonly Mock<IAtivoRepository> _ativoRepository = new(MockBehavior.Strict);
    private readonly OrdemService _service;

    public OrdemServiceTests()
    {
        _service = new OrdemService(_ordemRepository.Object, _ativoRepository.Object, TestData.CriarMapper());
    }

    private static OrdemCreateRequest CreateRequestValido() => new()
    {
        AtivoId = 1,
        Lado = LadoOrdem.Venda,
        Quantidade = 200,
        PrecoExecucao = 38.5m,
        DataExecucao = TestData.Data,
    };

    [Fact]
    public async Task GetPagedAsync_RepassaPaginacaoEFiltrosEMapeiaCamposAchatados()
    {
        var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc);
        var filtroEsperado = new OrdemFiltro(AtivoId: 1, Status: StatusOrdem.Executada, DataInicio: inicio, DataFim: fim);
        var ordens = new List<Ordem> { TestData.CriarOrdem(10, quantidade: 100, preco: 32.5m) };
        _ordemRepository
            .Setup(r => r.GetPagedAsync(3, 5, filtroEsperado, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ordens, 11));

        var resultado = await _service.GetPagedAsync(new OrdemQuery
        {
            PageNumber = 3,
            PageSize = 5,
            AtivoId = 1,
            Status = StatusOrdem.Executada,
            DataInicio = inicio,
            DataFim = fim,
        });

        var item = resultado.Items.Should().ContainSingle().Subject;
        item.NomeAtivo.Should().Be("Petrobras PN");
        item.ValorFinanceiro.Should().Be(3250m);
        resultado.TotalCount.Should().Be(11);
        resultado.TotalPages.Should().Be(3);
        resultado.HasNext.Should().BeFalse();
        resultado.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_ComPeriodoInvertido_LancaValidationExceptionSemConsultar()
    {
        var query = new OrdemQuery { DataInicio = TestData.Data, DataFim = TestData.Data.AddDays(-1) };

        var acao = () => _service.GetPagedAsync(query);

        await acao.Should().ThrowAsync<ValidationException>();
        _ordemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_Existente_RetornaResponse()
    {
        _ordemRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.CriarOrdem(10));

        var response = await _service.GetByIdAsync(10);

        response.Id.Should().Be(10);
        response.NomeAtivo.Should().Be("Petrobras PN");
    }

    [Fact]
    public async Task GetByIdAsync_Inexistente_LancaNotFoundException()
    {
        _ordemRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Ordem?)null);

        var acao = () => _service.GetByIdAsync(99);

        await acao.Should().ThrowAsync<NotFoundException>().WithMessage("*99*");
    }

    [Fact]
    public async Task CreateAsync_Valido_PersisteOrdemPendenteERetornaComAtivo()
    {
        Ordem? adicionada = null;
        _ativoRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _ordemRepository.Setup(r => r.AddAsync(It.IsAny<Ordem>(), It.IsAny<CancellationToken>()))
            .Callback<Ordem, CancellationToken>((o, _) => adicionada = TestData.ComId(o, 61))
            .Returns(Task.CompletedTask);
        _ordemRepository.Setup(r => r.GetByIdAsync(61, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TestData.CriarOrdem(61, quantidade: adicionada!.Quantidade, preco: adicionada.PrecoExecucao));

        var response = await _service.CreateAsync(CreateRequestValido());

        adicionada!.Lado.Should().Be(LadoOrdem.Venda);
        adicionada.Quantidade.Should().Be(200);
        adicionada.PrecoExecucao.Should().Be(38.5m);
        adicionada.DataExecucao.Should().Be(TestData.Data);
        adicionada.Status.Should().Be(StatusOrdem.Pendente);
        response.Id.Should().Be(61);
        response.NomeAtivo.Should().Be("Petrobras PN");
        response.ValorFinanceiro.Should().Be(7700m);
    }

    [Fact]
    public async Task CreateAsync_SemDataExecucao_UsaMomentoAtualEmUtc()
    {
        Ordem? adicionada = null;
        _ativoRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _ordemRepository.Setup(r => r.AddAsync(It.IsAny<Ordem>(), It.IsAny<CancellationToken>()))
            .Callback<Ordem, CancellationToken>((o, _) => adicionada = TestData.ComId(o, 62))
            .Returns(Task.CompletedTask);
        _ordemRepository.Setup(r => r.GetByIdAsync(62, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.CriarOrdem(62));
        var request = CreateRequestValido();
        request.DataExecucao = null;

        await _service.CreateAsync(request);

        adicionada!.DataExecucao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        adicionada.DataExecucao.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task CreateAsync_AtivoInexistente_LancaValidationExceptionSemPersistir()
    {
        _ativoRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var acao = () => _service.CreateAsync(CreateRequestValido());

        var erro = (await acao.Should().ThrowAsync<ValidationException>()).Which;
        erro.Errors.Should().ContainKey(nameof(OrdemCreateRequest.AtivoId));
        _ordemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_RequestInvalido_LancaValidationExceptionSemAcessarRepositorios()
    {
        var request = CreateRequestValido();
        request.Quantidade = 0;

        var acao = () => _service.CreateAsync(request);

        await acao.Should().ThrowAsync<ValidationException>();
        _ativoRepository.VerifyNoOtherCalls();
        _ordemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecutarAsync_OrdemPendente_AlteraStatusEPersiste()
    {
        var ordem = TestData.CriarOrdem(10);
        _ordemRepository.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ordem);
        _ordemRepository.Setup(r => r.UpdateAsync(ordem, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _ordemRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ordem);

        var response = await _service.ExecutarAsync(10);

        ordem.Status.Should().Be(StatusOrdem.Executada);
        response.Status.Should().Be(StatusOrdem.Executada);
        _ordemRepository.Verify(r => r.UpdateAsync(ordem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarAsync_OrdemPendente_AlteraStatusEPersiste()
    {
        var ordem = TestData.CriarOrdem(10);
        _ordemRepository.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ordem);
        _ordemRepository.Setup(r => r.UpdateAsync(ordem, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _ordemRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ordem);

        var response = await _service.CancelarAsync(10);

        response.Status.Should().Be(StatusOrdem.Cancelada);
    }

    [Fact]
    public async Task CancelarAsync_OrdemExecutada_PropagaDomainExceptionSemPersistir()
    {
        var ordem = TestData.CriarOrdem(10);
        ordem.Executar();
        _ordemRepository.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ordem);

        var acao = () => _service.CancelarAsync(10);

        await acao.Should().ThrowAsync<DomainException>();
        _ordemRepository.Verify(r => r.UpdateAsync(It.IsAny<Ordem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_Inexistente_LancaNotFoundException()
    {
        _ordemRepository.Setup(r => r.GetByIdForUpdateAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Ordem?)null);

        var acao = () => _service.ExecutarAsync(99);

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_Existente_Remove()
    {
        var ordem = TestData.CriarOrdem(10);
        _ordemRepository.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ordem);
        _ordemRepository.Setup(r => r.DeleteAsync(ordem, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteAsync(10);

        _ordemRepository.Verify(r => r.DeleteAsync(ordem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Inexistente_LancaNotFoundException()
    {
        _ordemRepository.Setup(r => r.GetByIdForUpdateAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Ordem?)null);

        var acao = () => _service.DeleteAsync(99);

        await acao.Should().ThrowAsync<NotFoundException>();
    }
}
