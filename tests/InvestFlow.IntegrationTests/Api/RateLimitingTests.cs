using System.Net;
using FluentAssertions;
using InvestFlow.Api.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace InvestFlow.IntegrationTests.Api;

public class RateLimitingTests
{
    private const string ListagemAtivos = "/api/v1/ativos";
    private const string ListagemOrdens = "/api/v1/ordens";

    [Fact]
    public async Task ListagemDeAtivos_SextaRequisicaoNaJanela_Retorna429PelaPoliticaEstrita()
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        await DeveResponderAsync(client, ListagemAtivos, RateLimitedApiFactory.EstritoPermitLimit, HttpStatusCode.OK);

        var response = await client.GetAsync(ListagemAtivos);

        await DeveSer429EmProblemDetailsAsync(response);
    }

    // Ativos: política estrita. Ordens: só o limite global — é o caso que pegaria uma isenção global indevida.
    [Theory]
    [InlineData(ListagemAtivos, RateLimitedApiFactory.EstritoPermitLimit)]
    [InlineData(ListagemOrdens, RateLimitedApiFactory.GlobalPermitLimit)]
    public async Task ChamadaDisparadaPeloSwaggerUi_ContinuaSujeitaAoLimite(string rota, int limite)
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        // O "Execute" do Swagger UI é um fetch do navegador para a própria rota da API, com a página do
        // Swagger como Referer. A isenção olha só o path da requisição, então esse Referer não muda nada.
        HttpRequestMessage ComoSwaggerUi() => new(HttpMethod.Get, $"{rota}?PageNumber=1&PageSize=10")
        {
            Headers =
            {
                { "Accept", "text/plain" },
                { "Referer", "http://localhost/swagger/index.html" },
                { "Sec-Fetch-Mode", "cors" },
            },
        };

        for (var i = 0; i < limite; i++)
            (await client.SendAsync(ComoSwaggerUi())).StatusCode.Should().Be(HttpStatusCode.OK);

        await DeveSer429EmProblemDetailsAsync(await client.SendAsync(ComoSwaggerUi()));
    }

    [Fact]
    public async Task OutrosEndpoints_NaoUsamAPoliticaEstrita()
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        // 10 + 10 requisições: acima do limite estrito (5), abaixo do global (30).
        await DeveResponderAsync(client, ListagemOrdens, 10, HttpStatusCode.OK);
        await DeveResponderAsync(client, $"{ListagemAtivos}/1", 10, HttpStatusCode.OK);
    }

    [Fact]
    public async Task LimiteGlobal_Excedido_Retorna429ComMesmoFormato()
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        await DeveResponderAsync(client, ListagemOrdens, RateLimitedApiFactory.GlobalPermitLimit, HttpStatusCode.OK);

        await DeveSer429EmProblemDetailsAsync(await client.GetAsync(ListagemOrdens));
    }

    [Fact]
    public async Task ListagemDeAtivos_TambemConsomeOLimiteGlobal()
    {
        using var factory = new RateLimitedApiFactory(globalPermitLimit: 3, estritoPermitLimit: 100);
        using var client = factory.CreateClient();

        await DeveResponderAsync(client, ListagemAtivos, 3, HttpStatusCode.OK);

        await DeveSer429EmProblemDetailsAsync(await client.GetAsync(ListagemAtivos));
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/index.html")]
    public async Task RotaIsenta_NaoEhLimitadaNemConsomeOLimiteGlobal(string rota)
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        await DeveResponderAsync(client, rota, RateLimitedApiFactory.GlobalPermitLimit + 10, HttpStatusCode.OK);

        (await client.GetAsync(ListagemOrdens)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/swagger", true)]
    [InlineData("/swagger/index.html", true)]
    [InlineData("/swagger/v1/swagger.json", true)]
    [InlineData("/health", true)]
    [InlineData("/health/live", true)]
    [InlineData("/HEALTH/LIVE", true)]
    [InlineData("/api/v1/ativos", false)]
    [InlineData("/api/v1/swagger", false)]
    [InlineData("/swaggerx", false)]
    [InlineData("/healthz", false)]
    [InlineData("/health/live/extra", false)]
    [InlineData("/", false)]
    public void RotaIsenta_SomenteSwaggerEHealthChecks(string path, bool isenta)
    {
        RateLimitingExtensions.RotaIsenta(new PathString(path)).Should().Be(isenta);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(30, 0)]
    public void ConfiguracaoInvalida_ImpedeASubida(int globalPermitLimit, int estritoPermitLimit)
    {
        using var factory = new RateLimitedApiFactory(globalPermitLimit, estritoPermitLimit);

        var subir = () => factory.CreateClient();

        subir.Should().Throw<OptionsValidationException>().WithMessage("*RateLimiting*");
    }

    private static async Task DeveResponderAsync(HttpClient client, string rota, int vezes, HttpStatusCode status)
    {
        for (var i = 1; i <= vezes; i++)
            (await client.GetAsync(rota)).StatusCode.Should().Be(status, "a requisição {0} de {1} em {2}", i, vezes, rota);
    }

    private static async Task DeveSer429EmProblemDetailsAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
        response.Headers.RetryAfter!.Delta.Should().BeGreaterThan(TimeSpan.Zero)
            .And.BeLessOrEqualTo(TimeSpan.FromSeconds(RateLimitedApiFactory.WindowSeconds));
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.LerAsync<ProblemDetails>();
        problem.Status.Should().Be(429);
        problem.Title.Should().Be("Muitas requisições.");
        problem.Detail.Should().StartWith("Limite de requisições excedido");
    }
}
