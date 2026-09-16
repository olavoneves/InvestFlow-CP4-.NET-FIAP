using InvestFlow.Api.Errors;
using InvestFlow.Application.Exceptions;
using InvestFlow.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace InvestFlow.Api.Middlewares;

/// <summary>
/// Converte exceções não tratadas em ProblemDetails:
/// ValidationException -> 400, NotFoundException -> 404, DomainException -> 409 e qualquer outra -> 500
/// genérico, sem expor mensagem nem stack trace.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // O cliente desconectou: não há para quem responder.
            _logger.LogInformation("Requisição {Method} {Path} cancelada pelo cliente.",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception,
                    "Erro após o início da resposta de {Method} {Path}; não é possível enviar ProblemDetails.",
                    context.Request.Method, context.Request.Path);
                throw;
            }

            var problem = Mapear(context, exception);

            context.Response.Clear();
            await ApiProblemDetails.EscreverAsync(context, problem);
        }
    }

    private ProblemDetails Mapear(HttpContext context, Exception exception)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;

        switch (exception)
        {
            case ValidationException validation:
                _logger.LogWarning("Validação falhou em {Method} {Path}: {@Errors}", method, path, validation.Errors);
                return ApiProblemDetails.Validacao(context, validation.Errors);

            case NotFoundException:
                _logger.LogWarning("Recurso não encontrado em {Method} {Path}: {Mensagem}", method, path, exception.Message);
                return ApiProblemDetails.Criar(
                    context, StatusCodes.Status404NotFound, ApiProblemDetails.TituloNaoEncontrado, exception.Message);

            case DomainException:
                _logger.LogWarning("Regra de negócio violada em {Method} {Path}: {Mensagem}", method, path, exception.Message);
                return ApiProblemDetails.Criar(
                    context, StatusCodes.Status409Conflict, ApiProblemDetails.TituloConflito, exception.Message);

            default:
                _logger.LogError(exception, "Erro não tratado em {Method} {Path}.", method, path);
                return ApiProblemDetails.Criar(
                    context, StatusCodes.Status500InternalServerError,
                    ApiProblemDetails.TituloErroInterno, ApiProblemDetails.DetalheErroInterno);
        }
    }
}
