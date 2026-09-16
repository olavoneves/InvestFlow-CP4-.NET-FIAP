using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InvestFlow.Application.DTOs.Ativos;
using InvestFlow.Application.Exceptions;
using InvestFlow.Domain.Enums;

namespace InvestFlow.IntegrationTests.Api;

/// <summary>
/// O 400 do model binding (DataAnnotations) e o 400 da ValidationException lançada pelo service precisam
/// ter exatamente o mesmo formato de corpo.
/// </summary>
public class ValidacaoPadronizadaTests : IClassFixture<InvestFlowApiFactory>
{
    private const string Rota = "/api/v1/ativos";

    private readonly HttpClient _client;

    public ValidacaoPadronizadaTests(InvestFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Erro400DoModelBindingEDoService_TemMesmoFormato()
    {
        // Falha na DataAnnotation: nem chega ao service.
        var modelBinding = await _client.PostAsJsonAsync(Rota, new AtivoCreateRequest
        {
            Ticker = "",
            Nome = "Qualquer",
            Tipo = TipoAtivo.Acao,
            PrecoAtual = 10m,
        }, ApiJson.Options);

        // Request válido, mas o service rejeita: ticker já cadastrado (seed).
        var service = await _client.PostAsJsonAsync(Rota, new AtivoCreateRequest
        {
            Ticker = "PETR4",
            Nome = "Duplicado",
            Tipo = TipoAtivo.Acao,
            PrecoAtual = 10m,
        }, ApiJson.Options);

        modelBinding.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        service.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        service.Content.Headers.ContentType!.ToString()
            .Should().Be(modelBinding.Content.Headers.ContentType!.ToString())
            .And.Be("application/problem+json; charset=utf-8");

        var corpoModelBinding = await modelBinding.Content.ReadAsStringAsync();
        var corpoService = await service.Content.ReadAsStringAsync();

        using var jsonModelBinding = JsonDocument.Parse(corpoModelBinding);
        using var jsonService = JsonDocument.Parse(corpoService);
        var raizModelBinding = jsonModelBinding.RootElement;
        var raizService = jsonService.RootElement;

        // Mesmas propriedades, na mesma ordem.
        NomesDasPropriedades(raizService).Should().Equal(NomesDasPropriedades(raizModelBinding))
            .And.Equal("type", "title", "status", "instance", "errors", "traceId");

        foreach (var propriedade in new[] { "type", "title", "instance" })
            raizService.GetProperty(propriedade).GetString().Should().Be(raizModelBinding.GetProperty(propriedade).GetString());

        raizService.GetProperty("status").GetInt32().Should().Be(400);
        raizModelBinding.GetProperty("status").GetInt32().Should().Be(400);

        // Mesma serialização de texto (inclusive acentos sem escape) nos dois caminhos.
        corpoModelBinding.Should().Contain($"\"title\":\"{ValidationException.MensagemPadrao}\"");
        corpoService.Should().Contain($"\"title\":\"{ValidationException.MensagemPadrao}\"");

        raizModelBinding.GetProperty("errors").GetProperty("Ticker").EnumerateArray().Should().ContainSingle();
        raizService.GetProperty("errors").GetProperty("Ticker").EnumerateArray().Should().ContainSingle()
            .Which.GetString().Should().Contain("PETR4");
    }

    [Fact]
    public async Task JsonComValorInconversivel_Retorna400SemExporDetalhesInternos()
    {
        var conteudo = new StringContent(
            """{"ticker":"ABC1","nome":"Teste","tipo":"NaoExiste","precoAtual":1}""",
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync(Rota, conteudo);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadAsStringAsync();
        corpo.Should().NotContain("InvestFlow.Domain");

        using var json = JsonDocument.Parse(corpo);
        var errors = json.RootElement.GetProperty("errors");
        NomesDasPropriedades(errors).Should().Equal("$.tipo");
        errors.GetProperty("$.tipo")[0].GetString().Should().Be("Valor inválido.");
    }

    private static IEnumerable<string> NomesDasPropriedades(JsonElement elemento) =>
        elemento.EnumerateObject().Select(p => p.Name).ToList();
}
