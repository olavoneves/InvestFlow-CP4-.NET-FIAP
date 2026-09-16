using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvestFlow.Api.Observability;
using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ordens;
using InvestFlow.Domain.Enums;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace InvestFlow.IntegrationTests.Api;

public class OrdensControllerTests : IClassFixture<InvestFlowApiFactory>
{
    private const string Rota = "/api/v1/ordens";

    // Ordem 1 do seed está executada.
    private const int IdOrdemExecutada = 1;

    private readonly InvestFlowApiFactory _factory;
    private readonly HttpClient _client;

    public OrdensControllerTests(InvestFlowApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Listar_FiltroPorStatus_RetornaSomenteOrdensDoStatusComPaginacao()
    {
        var response = await _client.GetAsync($"{Rota}?status=Executada&pageSize=5&pageNumber=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await response.LerAsync<PagedResult<OrdemResponse>>();
        pagina.PageNumber.Should().Be(2);
        pagina.PageSize.Should().Be(5);
        pagina.Items.Should().HaveCount(5).And.OnlyContain(o => o.Status == StatusOrdem.Executada);
        pagina.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task Listar_DataInicioDepoisDaDataFim_Retorna400()
    {
        var response = await _client.GetAsync($"{Rota}?dataInicio=2026-02-01&dataFim=2026-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.LerAsync<ValidationProblemDetails>()).Errors.Should().ContainKey("DataFim");
    }

    [Fact]
    public async Task ObterPorId_Existente_RetornaNomeDoAtivoEValorFinanceiro()
    {
        var response = await _client.GetAsync($"{Rota}/{IdOrdemExecutada}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ordem = await response.LerAsync<OrdemResponse>();
        ordem.NomeAtivo.Should().Be("Petrobras PN");
        ordem.ValorFinanceiro.Should().Be(ordem.Quantidade * ordem.PrecoExecucao);
    }

    [Fact]
    public async Task ObterPorId_Inexistente_Retorna404()
    {
        var response = await _client.GetAsync($"{Rota}/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.LerAsync<ProblemDetails>()).Status.Should().Be(404);
    }

    [Fact]
    public async Task Criar_Valida_Retorna201PendenteComLocation()
    {
        var response = await _client.PostAsJsonAsync(Rota, NovaOrdem(), ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ordem = await response.LerAsync<OrdemResponse>();
        ordem.Status.Should().Be(StatusOrdem.Pendente);
        ordem.NomeAtivo.Should().Be("Vale ON");
        response.Headers.Location!.AbsolutePath.Should().Be($"{Rota}/{ordem.Id}");
    }

    [Fact]
    public async Task Criar_Valida_EmiteMetricaOrdensCriadas()
    {
        var response = await _client.PostAsJsonAsync(Rota, NovaOrdem(LadoOrdem.Venda), ApiJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Métricas são agregadas; o Flush força o envio do agregado para o canal.
        _factory.Services.GetRequiredService<TelemetryClient>().Flush();

        _factory.Telemetria.Itens.OfType<MetricTelemetry>().Should().Contain(m =>
            m.Name == Metricas.OrdensCriadas &&
            m.Properties[Metricas.DimensaoLado] == nameof(LadoOrdem.Venda) &&
            m.Sum >= 1);
    }

    [Fact]
    public async Task Criar_AtivoInexistente_Retorna400NoCampoAtivoId()
    {
        var request = NovaOrdem();
        request.AtivoId = 999999;

        var response = await _client.PostAsJsonAsync(Rota, request, ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.LerAsync<ValidationProblemDetails>()).Errors.Should().ContainKey("AtivoId");
    }

    [Fact]
    public async Task Executar_Pendente_Retorna200ESegundaExecucaoRetorna409()
    {
        var ordem = await CriarOrdemAsync();

        var primeira = await _client.PostAsync($"{Rota}/{ordem.Id}/executar", null);
        var segunda = await _client.PostAsync($"{Rota}/{ordem.Id}/executar", null);

        primeira.StatusCode.Should().Be(HttpStatusCode.OK);
        (await primeira.LerAsync<OrdemResponse>()).Status.Should().Be(StatusOrdem.Executada);
        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancelar_Pendente_Retorna200Cancelada()
    {
        var ordem = await CriarOrdemAsync();

        var response = await _client.PostAsync($"{Rota}/{ordem.Id}/cancelar", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.LerAsync<OrdemResponse>()).Status.Should().Be(StatusOrdem.Cancelada);
    }

    [Fact]
    public async Task Cancelar_OrdemExecutada_Retorna409ProblemDetails()
    {
        var response = await _client.PostAsync($"{Rota}/{IdOrdemExecutada}/cancelar", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.LerAsync<ProblemDetails>();
        problem.Status.Should().Be(409);
        problem.Detail.Should().Contain("já executada");
    }

    [Fact]
    public async Task Cancelar_Inexistente_Retorna404()
    {
        var response = await _client.PostAsync($"{Rota}/999999/cancelar", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Excluir_Existente_Retorna204EDeixaDeExistir()
    {
        var ordem = await CriarOrdemAsync();

        var response = await _client.DeleteAsync($"{Rota}/{ordem.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"{Rota}/{ordem.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static OrdemCreateRequest NovaOrdem(LadoOrdem lado = LadoOrdem.Compra) => new()
    {
        AtivoId = 2,
        Lado = lado,
        Quantidade = 100,
        PrecoExecucao = 61.4m,
    };

    private async Task<OrdemResponse> CriarOrdemAsync()
    {
        var response = await _client.PostAsJsonAsync(Rota, NovaOrdem(), ApiJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.LerAsync<OrdemResponse>();
    }
}
