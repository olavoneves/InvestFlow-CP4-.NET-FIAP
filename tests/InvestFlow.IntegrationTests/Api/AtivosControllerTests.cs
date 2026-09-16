using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ativos;
using InvestFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InvestFlow.IntegrationTests.Api;

public class AtivosControllerTests : IClassFixture<InvestFlowApiFactory>
{
    private const string Rota = "/api/v1/ativos";

    private readonly HttpClient _client;

    public AtivosControllerTests(InvestFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Listar_SemParametros_RetornaPrimeiraPaginaComPaginacaoPadrao()
    {
        var response = await _client.GetAsync(Rota);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await response.LerAsync<PagedResult<AtivoResponse>>();
        pagina.PageNumber.Should().Be(1);
        pagina.PageSize.Should().Be(10);
        pagina.TotalCount.Should().BeGreaterOrEqualTo(5);
        pagina.Items.Should().NotBeEmpty().And.BeInAscendingOrder(a => a.Ticker, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Listar_ComBuscaETipo_RetornaSomenteAtivosFiltrados()
    {
        var response = await _client.GetAsync($"{Rota}?busca=petro&tipo=Acao&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await response.LerAsync<PagedResult<AtivoResponse>>();
        pagina.Items.Should().ContainSingle(a => a.Ticker == "PETR4");
        pagina.Items.Should().OnlyContain(a => a.Tipo == TipoAtivo.Acao);
    }

    [Theory]
    [InlineData("pageSize=51", "PageSize")]
    [InlineData("pageSize=0", "PageSize")]
    [InlineData("pageNumber=0", "PageNumber")]
    public async Task Listar_PaginacaoInvalida_Retorna400ComErroNoCampo(string queryString, string campo)
    {
        var response = await _client.GetAsync($"{Rota}?{queryString}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.LerAsync<ValidationProblemDetails>();
        problem.Errors.Should().ContainKey(campo);
    }

    [Fact]
    public async Task ObterPorId_Existente_Retorna200()
    {
        var response = await _client.GetAsync($"{Rota}/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ativo = await response.LerAsync<AtivoResponse>();
        ativo.Id.Should().Be(1);
        ativo.Ticker.Should().Be("PETR4");
    }

    [Fact]
    public async Task ObterPorId_Inexistente_Retorna404ProblemDetails()
    {
        var response = await _client.GetAsync($"{Rota}/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.LerAsync<ProblemDetails>();
        problem.Status.Should().Be(404);
        problem.Detail.Should().Contain("999999");
        problem.Instance.Should().Be($"{Rota}/999999");
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task Criar_Valido_Retorna201ComLocationDoRecursoCriado()
    {
        var ticker = ApiJson.NovoTicker();

        // NovoTicker tem 9 caracteres: com o espaço, fica no limite de 10 do [StringLength].
        var response = await _client.PostAsJsonAsync(Rota, NovoAtivo($" {ticker.ToLowerInvariant()}"), ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var criado = await response.LerAsync<AtivoResponse>();
        criado.Ticker.Should().Be(ticker);
        response.Headers.Location!.AbsolutePath.Should().Be($"{Rota}/{criado.Id}");

        var consulta = await _client.GetAsync(response.Headers.Location);
        consulta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await consulta.LerAsync<AtivoResponse>()).Should().BeEquivalentTo(criado);
    }

    [Fact]
    public async Task Atualizar_Valido_Retorna200ComDadosNovos()
    {
        var criado = await CriarAtivoAsync();
        var request = new AtivoUpdateRequest { Nome = "Nome Atualizado", Tipo = TipoAtivo.FII, PrecoAtual = 99.5m };

        var response = await _client.PutAsJsonAsync($"{Rota}/{criado.Id}", request, ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = await response.LerAsync<AtivoResponse>();
        atualizado.Ticker.Should().Be(criado.Ticker);
        atualizado.Nome.Should().Be("Nome Atualizado");
        atualizado.Tipo.Should().Be(TipoAtivo.FII);
        atualizado.PrecoAtual.Should().Be(99.5m);
    }

    [Fact]
    public async Task Atualizar_Inexistente_Retorna404()
    {
        var request = new AtivoUpdateRequest { Nome = "Qualquer", Tipo = TipoAtivo.Acao, PrecoAtual = 1m };

        var response = await _client.PutAsJsonAsync($"{Rota}/999999", request, ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Atualizar_DadosInvalidos_Retorna400()
    {
        var request = new AtivoUpdateRequest { Nome = "", Tipo = TipoAtivo.Acao, PrecoAtual = 1m };

        var response = await _client.PutAsJsonAsync($"{Rota}/1", request, ApiJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.LerAsync<ValidationProblemDetails>()).Errors.Should().ContainKey("Nome");
    }

    [Fact]
    public async Task Excluir_AtivoSemOrdens_Retorna204EDeixaDeExistir()
    {
        var criado = await CriarAtivoAsync();

        var response = await _client.DeleteAsync($"{Rota}/{criado.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"{Rota}/{criado.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Excluir_AtivoComOrdens_Retorna409ProblemDetails()
    {
        var response = await _client.DeleteAsync($"{Rota}/1");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.LerAsync<ProblemDetails>();
        problem.Status.Should().Be(409);
        problem.Detail.Should().Contain("possui ordens");
    }

    private static AtivoCreateRequest NovoAtivo(string ticker) => new()
    {
        Ticker = ticker,
        Nome = "Ativo de Teste",
        Tipo = TipoAtivo.Acao,
        PrecoAtual = 10.25m,
    };

    private async Task<AtivoResponse> CriarAtivoAsync()
    {
        var response = await _client.PostAsJsonAsync(Rota, NovoAtivo(ApiJson.NovoTicker()), ApiJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.LerAsync<AtivoResponse>();
    }
}
