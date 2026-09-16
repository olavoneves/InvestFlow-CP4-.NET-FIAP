using AutoMapper;
using FluentAssertions;
using InvestFlow.Application;
using InvestFlow.Application.DTOs.Ativos;
using InvestFlow.Application.DTOs.Ordens;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;

namespace InvestFlow.UnitTests.Application.Mappings;

public class MappingProfileTests
{
    private readonly IMapper _mapper = TestData.CriarMapper();

    [Fact]
    public void Configuracao_TodosOsMembrosDeDestinoEstaoMapeados()
    {
        var configuracao = new MapperConfiguration(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly));

        configuracao.AssertConfigurationIsValid();
    }

    [Fact]
    public void Ativo_ParaAtivoResponse_CopiaTodosOsCampos()
    {
        var ativo = TestData.CriarAtivo(id: 7, ticker: "vale3", nome: "Vale ON");

        var response = _mapper.Map<AtivoResponse>(ativo);

        response.Should().BeEquivalentTo(new AtivoResponse
        {
            Id = 7,
            Ticker = "VALE3",
            Nome = "Vale ON",
            Tipo = TipoAtivo.Acao,
            PrecoAtual = 38.12m,
            CriadoEm = ativo.CriadoEm,
        });
    }

    [Fact]
    public void Ordem_ParaOrdemResponse_AchataNomeAtivoEValorFinanceiro()
    {
        var ativo = TestData.CriarAtivo(id: 3, ticker: "HGLG11", nome: "CSHG Logística FII");
        var ordem = TestData.CriarOrdem(id: 42, ativo: ativo, quantidade: 3, preco: 0.3333m);

        var response = _mapper.Map<OrdemResponse>(ordem);

        response.Should().BeEquivalentTo(new OrdemResponse
        {
            Id = 42,
            AtivoId = 3,
            NomeAtivo = "CSHG Logística FII",
            Lado = LadoOrdem.Compra,
            Quantidade = 3,
            PrecoExecucao = 0.3333m,
            ValorFinanceiro = 0.9999m,
            DataExecucao = TestData.Data,
            Status = StatusOrdem.Pendente,
        });
    }

    [Fact]
    public void Ordem_SemAtivoCarregado_MapeiaNomeAtivoVazio()
    {
        var ordem = new Ordem(1, LadoOrdem.Venda, 10, 5m, TestData.Data);

        var response = _mapper.Map<OrdemResponse>(ordem);

        response.NomeAtivo.Should().BeEmpty();
        response.ValorFinanceiro.Should().Be(50m);
    }
}
