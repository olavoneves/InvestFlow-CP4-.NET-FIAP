using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace InvestFlow.IntegrationTests.Api;

/// <summary>Health checks, compressão de resposta e documentação Swagger.</summary>
public class InfraestruturaHttpTests : IClassFixture<InvestFlowApiFactory>
{
    private readonly HttpClient _client;

    public InfraestruturaHttpTests(InvestFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_RetornaJsonDetalhadoComChecagemDoBanco()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        json.RootElement.GetProperty("entries").GetProperty("database").GetProperty("status").GetString()
            .Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthLive_RetornaHealthySimples()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Theory]
    [InlineData("br")]
    [InlineData("gzip")]
    public async Task RespostaJson_ComprimidaConformeAcceptEncoding(string encoding)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ordens?pageSize=50");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(encoding));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.Should().Equal(encoding);
    }

    [Fact]
    public async Task SwaggerJson_DocumentaEndpointsRespostasEExemplos()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var raiz = json.RootElement;

        raiz.GetProperty("info").GetProperty("title").GetString().Should().Be("InvestFlow API");

        var paths = raiz.GetProperty("paths");
        paths.EnumerateObject().Select(p => p.Name).Should().Contain(new[]
        {
            "/api/v1/ativos", "/api/v1/ativos/{id}",
            "/api/v1/ordens", "/api/v1/ordens/{id}", "/api/v1/ordens/{id}/executar", "/api/v1/ordens/{id}/cancelar",
        });

        var listarAtivos = paths.GetProperty("/api/v1/ativos").GetProperty("get");
        listarAtivos.GetProperty("summary").GetString().Should().Be("Lista ativos com paginação");
        listarAtivos.GetProperty("responses").EnumerateObject().Select(r => r.Name)
            .Should().Contain(new[] { "200", "400", "429", "500" });

        // Política estrita documentada só na listagem de ativos.
        listarAtivos.GetProperty("description").GetString().Should().Contain("estrito").And.Contain("5 requisições");
        listarAtivos.GetProperty("responses").GetProperty("429").GetProperty("description").GetString()
            .Should().Contain("estrito");
        paths.GetProperty("/api/v1/ordens").GetProperty("get").GetProperty("description").GetString()
            .Should().NotContain("estrito");

        paths.GetProperty("/api/v1/ativos").GetProperty("post").GetProperty("responses")
            .EnumerateObject().Select(r => r.Name).Should().Contain("201");

        // <example> dos XML comments dos DTOs.
        raiz.GetProperty("components").GetProperty("schemas").GetProperty("AtivoCreateRequest")
            .GetProperty("properties").GetProperty("ticker").GetProperty("example").GetString()
            .Should().Be("ITUB4");
    }
}
