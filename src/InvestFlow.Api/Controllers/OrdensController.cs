using InvestFlow.Api.Configuration;
using InvestFlow.Api.Observability;
using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ordens;
using InvestFlow.Application.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace InvestFlow.Api.Controllers;

/// <summary>Registro e acompanhamento de ordens de compra e venda de ativos.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting(RateLimitingExtensions.PoliticaFixa)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
[SwaggerResponse(StatusCodes.Status429TooManyRequests, "Limite de requisições por IP excedido. Consulte o header Retry-After.", typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
[SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro inesperado.", typeof(ProblemDetails))]
public class OrdensController : ControllerBase
{
    private readonly IOrdemService _ordemService;
    private readonly TelemetryClient _telemetryClient;

    public OrdensController(IOrdemService ordemService, TelemetryClient telemetryClient)
    {
        _ordemService = ordemService;
        _telemetryClient = telemetryClient;
    }

    /// <param name="query">Paginação e filtros opcionais.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista ordens com paginação",
        Description = "Retorna as ordens com o nome do ativo e o valor financeiro. Filtros opcionais: AtivoId, Status e " +
                      "período de execução (DataInicio/DataFim, inclusivos). PageNumber padrão 1; PageSize padrão 10, máximo 50.")]
    [ProducesResponseType(typeof(PagedResult<OrdemResponse>), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Página de ordens com os metadados de navegação.", typeof(PagedResult<OrdemResponse>))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Paginação ou filtros inválidos.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<PagedResult<OrdemResponse>>> Listar(
        [FromQuery] OrdemQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _ordemService.GetPagedAsync(query, cancellationToken));
    }

    /// <param name="id">Identificador da ordem.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Obtém uma ordem pelo id", Description = "Retorna a ordem com o nome do ativo e o valor financeiro.")]
    [ProducesResponseType(typeof(OrdemResponse), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Ordem encontrada.", typeof(OrdemResponse))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ordem inexistente.", typeof(ProblemDetails))]
    public async Task<ActionResult<OrdemResponse>> ObterPorId(int id, CancellationToken cancellationToken)
    {
        return Ok(await _ordemService.GetByIdAsync(id, cancellationToken));
    }

    /// <param name="request">Dados da nova ordem.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Registra uma ordem",
        Description = "A ordem nasce com status Pendente. Se DataExecucao for omitida, usa o momento do registro (UTC). " +
                      "Emite a métrica customizada \"ordens.criadas\" no Application Insights.")]
    [ProducesResponseType(typeof(OrdemResponse), StatusCodes.Status201Created)]
    [SwaggerResponse(StatusCodes.Status201Created, "Ordem criada.", typeof(OrdemResponse))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou ativo inexistente.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<OrdemResponse>> Criar(
        [FromBody] OrdemCreateRequest request, CancellationToken cancellationToken)
    {
        var ordem = await _ordemService.CreateAsync(request, cancellationToken);

        _telemetryClient
            .GetMetric(Metricas.OrdensCriadas, Metricas.DimensaoLado)
            .TrackValue(1, ordem.Lado.ToString());

        return CreatedAtAction(nameof(ObterPorId), new { id = ordem.Id }, ordem);
    }

    /// <param name="id">Identificador da ordem.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpPost("{id:int}/executar")]
    [SwaggerOperation(Summary = "Executa uma ordem", Description = "Transição Pendente -> Executada.")]
    [ProducesResponseType(typeof(OrdemResponse), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Ordem executada.", typeof(OrdemResponse))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ordem inexistente.", typeof(ProblemDetails))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status409Conflict, "A ordem não está pendente.", typeof(ProblemDetails))]
    public async Task<ActionResult<OrdemResponse>> Executar(int id, CancellationToken cancellationToken)
    {
        return Ok(await _ordemService.ExecutarAsync(id, cancellationToken));
    }

    /// <param name="id">Identificador da ordem.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpPost("{id:int}/cancelar")]
    [SwaggerOperation(Summary = "Cancela uma ordem", Description = "Transição Pendente -> Cancelada. Ordens executadas ou já canceladas não podem ser canceladas.")]
    [ProducesResponseType(typeof(OrdemResponse), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Ordem cancelada.", typeof(OrdemResponse))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ordem inexistente.", typeof(ProblemDetails))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status409Conflict, "A ordem já foi executada ou cancelada.", typeof(ProblemDetails))]
    public async Task<ActionResult<OrdemResponse>> Cancelar(int id, CancellationToken cancellationToken)
    {
        return Ok(await _ordemService.CancelarAsync(id, cancellationToken));
    }

    /// <param name="id">Identificador da ordem.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Exclui uma ordem", Description = "Remove a ordem definitivamente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Ordem excluída.")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ordem inexistente.", typeof(ProblemDetails))]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        await _ordemService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
