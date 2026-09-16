using System.Text.Json;
using FluentAssertions;
using InvestFlow.Api.Errors;
using InvestFlow.Api.Middlewares;
using InvestFlow.Application.Exceptions;
using InvestFlow.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvestFlow.IntegrationTests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _logger = new();

    public static TheoryData<Exception, int, string> ExcecoesMapeadas => new()
    {
        { new ValidationException("Ticker", "Ticker é obrigatório."), 400, ValidationException.MensagemPadrao },
        { new NotFoundException("Ativo com id 7 não foi encontrado."), 404, ApiProblemDetails.TituloNaoEncontrado },
        { new DomainException("A ordem já está cancelada."), 409, ApiProblemDetails.TituloConflito },
        { new InvalidOperationException("detalhe interno"), 500, ApiProblemDetails.TituloErroInterno },
    };

    [Theory]
    [MemberData(nameof(ExcecoesMapeadas))]
    public async Task Excecao_ConvertidaNoStatusEProblemDetailsCorrespondentes(Exception excecao, int status, string titulo)
    {
        var context = CriarContexto();

        await CriarMiddleware(_ => throw excecao).InvokeAsync(context);

        context.Response.StatusCode.Should().Be(status);
        context.Response.ContentType.Should().Be(ApiProblemDetails.ContentType);

        using var json = await LerCorpoAsync(context);
        json.RootElement.GetProperty("status").GetInt32().Should().Be(status);
        json.RootElement.GetProperty("title").GetString().Should().Be(titulo);
        json.RootElement.GetProperty("instance").GetString().Should().Be("/api/v1/teste");
        json.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ValidationException_ErrosAgrupadosPorCampo()
    {
        var context = CriarContexto();
        var excecao = new ValidationException(new Dictionary<string, string[]>
        {
            ["Ticker"] = new[] { "Ticker é obrigatório." },
            ["PrecoAtual"] = new[] { "PrecoAtual inválido.", "PrecoAtual fora do limite." },
        });

        await CriarMiddleware(_ => throw excecao).InvokeAsync(context);

        using var json = await LerCorpoAsync(context);
        var errors = json.RootElement.GetProperty("errors");
        errors.GetProperty("Ticker").GetArrayLength().Should().Be(1);
        errors.GetProperty("PrecoAtual").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task ExcecaoInesperada_Retorna500GenericoSemVazarDetalhesELogaComoError()
    {
        var context = CriarContexto();
        var excecao = new InvalidOperationException("senha do banco: segredo");

        await CriarMiddleware(_ => throw excecao).InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Position = 0;
        var corpo = await new StreamReader(context.Response.Body).ReadToEndAsync();
        corpo.Should().NotContain("segredo")
            .And.NotContain(nameof(InvalidOperationException))
            .And.NotContain(nameof(ExceptionHandlingMiddlewareTests))
            .And.Contain(ApiProblemDetails.DetalheErroInterno);

        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            excecao,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Theory]
    [MemberData(nameof(ExcecoesDeCliente))]
    public async Task ExcecaoDeCliente_LogadaComoWarningSemStackTrace(Exception excecao)
    {
        await CriarMiddleware(_ => throw excecao).InvokeAsync(CriarContexto());

        _logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    public static TheoryData<Exception> ExcecoesDeCliente => new()
    {
        new ValidationException("Ticker", "Ticker é obrigatório."),
        new NotFoundException("Não encontrado."),
        new DomainException("Regra violada."),
    };

    [Fact]
    public async Task SemExcecao_NaoAlteraAResposta()
    {
        var context = CriarContexto();

        await CriarMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        context.Response.StatusCode.Should().Be(204);
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task CancelamentoPeloCliente_NaoEscreveResposta()
    {
        using var cancelamento = new CancellationTokenSource();
        cancelamento.Cancel();
        var context = CriarContexto();
        context.RequestAborted = cancelamento.Token;

        await CriarMiddleware(ctx => throw new OperationCanceledException(ctx.RequestAborted)).InvokeAsync(context);

        context.Response.Body.Length.Should().Be(0);
    }

    private ExceptionHandlingMiddleware CriarMiddleware(RequestDelegate next) => new(next, _logger.Object);

    private static DefaultHttpContext CriarContexto()
    {
        var services = new ServiceCollection();
        services.AddControllers();

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/teste";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> LerCorpoAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
