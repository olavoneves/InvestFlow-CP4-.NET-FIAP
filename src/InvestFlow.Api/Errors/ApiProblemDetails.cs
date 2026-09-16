using System.Diagnostics;
using InvestFlow.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace InvestFlow.Api.Errors;

/// <summary>
/// Ponto único de criação e escrita dos corpos de erro da API. O middleware global de exceções, o 400
/// automático do model binding e a rejeição do rate limiting passam por aqui, o que garante o mesmo
/// formato de resposta em todos os casos.
/// </summary>
public static class ApiProblemDetails
{
    /// <summary>Mesmo Content-Type que o MVC usa ao serializar ProblemDetails.</summary>
    public const string ContentType = "application/problem+json; charset=utf-8";

    public const string TituloNaoEncontrado = "Recurso não encontrado.";
    public const string TituloConflito = "A operação conflita com o estado atual do recurso.";
    public const string TituloMuitasRequisicoes = "Muitas requisições.";
    public const string TituloErroInterno = "Erro interno no servidor.";
    public const string DetalheErroInterno = "Ocorreu um erro inesperado. Informe o traceId ao suporte.";

    private const string MensagemValorInvalido = "Valor inválido.";

    private static readonly Dictionary<int, string> Tipos = new()
    {
        [StatusCodes.Status400BadRequest] = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        [StatusCodes.Status404NotFound] = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        [StatusCodes.Status409Conflict] = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        [StatusCodes.Status429TooManyRequests] = "https://tools.ietf.org/html/rfc6585#section-4",
        [StatusCodes.Status500InternalServerError] = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
    };

    /// <summary>400 com os erros agrupados por campo.</summary>
    public static ValidationProblemDetails Validacao(
        HttpContext httpContext, IEnumerable<KeyValuePair<string, string[]>> errors)
    {
        var problem = new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ValidationException.MensagemPadrao,
        };

        return Completar(problem, httpContext);
    }

    /// <summary>400 a partir do ModelState inválido do model binding.</summary>
    public static ValidationProblemDetails Validacao(HttpContext httpContext, ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .Select(e => KeyValuePair.Create(
                e.Key,
                e.Value!.Errors
                    .Select(erro => string.IsNullOrWhiteSpace(erro.ErrorMessage) ? MensagemValorInvalido : erro.ErrorMessage)
                    .ToArray()));

        return Validacao(httpContext, errors);
    }

    public static ProblemDetails Criar(HttpContext httpContext, int status, string title, string? detail) =>
        Completar(new ProblemDetails { Status = status, Title = title, Detail = detail }, httpContext);

    /// <summary>Escreve o corpo com as mesmas opções de JSON dos controllers.</summary>
    public static Task EscreverAsync(HttpContext httpContext, ProblemDetails problem, CancellationToken cancellationToken = default)
    {
        var jsonOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return httpContext.Response.WriteAsJsonAsync(
            problem, problem.GetType(), jsonOptions.JsonSerializerOptions, ContentType, cancellationToken);
    }

    private static TProblem Completar<TProblem>(TProblem problem, HttpContext httpContext)
        where TProblem : ProblemDetails
    {
        if (problem.Status is { } status && Tipos.TryGetValue(status, out var tipo))
            problem.Type = tipo;

        problem.Instance = httpContext.Request.Path;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return problem;
    }
}
