using InvestFlow.Api.Configuration;
using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ativos;
using InvestFlow.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace InvestFlow.Api.Controllers;

/// <summary>Cadastro e consulta de ativos negociáveis (ações, FIIs, renda fixa e derivativos).</summary>
[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting(RateLimitingExtensions.PoliticaFixa)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
[SwaggerResponse(StatusCodes.Status429TooManyRequests, "Limite de requisições por IP excedido. Consulte o header Retry-After.", typeof(ProblemDetails))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
[SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro inesperado.", typeof(ProblemDetails))]
public class AtivosController : ControllerBase
{
    private readonly IAtivoService _ativoService;

    public AtivosController(IAtivoService ativoService)
    {
        _ativoService = ativoService;
    }

    /// <param name="query">Paginação e filtros opcionais.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista ativos com paginação",
        Description = "Retorna os ativos ordenados por ticker. Filtros opcionais: Tipo e Busca (trecho do ticker ou do nome, " +
                      "sem diferenciar maiúsculas). PageNumber padrão 1; PageSize padrão 10, máximo 50.")]
    [ProducesResponseType(typeof(PagedResult<AtivoResponse>), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Página de ativos com os metadados de navegação.", typeof(PagedResult<AtivoResponse>))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Paginação ou filtros inválidos.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<PagedResult<AtivoResponse>>> Listar(
        [FromQuery] AtivoQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _ativoService.GetPagedAsync(query, cancellationToken));
    }

    /// <param name="id">Identificador do ativo.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Obtém um ativo pelo id", Description = "Retorna os dados do ativo.")]
    [ProducesResponseType(typeof(AtivoResponse), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Ativo encontrado.", typeof(AtivoResponse))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ativo inexistente.", typeof(ProblemDetails))]
    public async Task<ActionResult<AtivoResponse>> ObterPorId(int id, CancellationToken cancellationToken)
    {
        return Ok(await _ativoService.GetByIdAsync(id, cancellationToken));
    }

    /// <param name="request">Dados do novo ativo.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastra um ativo",
        Description = "O ticker é gravado sem espaços nas pontas e em maiúsculas, e deve ser único. " +
                      "Responde 201 com o header Location apontando para o ativo criado.")]
    [ProducesResponseType(typeof(AtivoResponse), StatusCodes.Status201Created)]
    [SwaggerResponse(StatusCodes.Status201Created, "Ativo criado.", typeof(AtivoResponse))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou ticker já cadastrado.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<AtivoResponse>> Criar(
        [FromBody] AtivoCreateRequest request, CancellationToken cancellationToken)
    {
        var ativo = await _ativoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = ativo.Id }, ativo);
    }

    /// <param name="id">Identificador do ativo.</param>
    /// <param name="request">Novos dados do ativo. O ticker não pode ser alterado.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Atualiza um ativo",
        Description = "Substitui nome, tipo e preço atual. O ticker é a identidade de mercado do ativo e não muda.")]
    [ProducesResponseType(typeof(AtivoResponse), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Ativo atualizado.", typeof(AtivoResponse))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos.", typeof(ValidationProblemDetails))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ativo inexistente.", typeof(ProblemDetails))]
    public async Task<ActionResult<AtivoResponse>> Atualizar(
        int id, [FromBody] AtivoUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _ativoService.UpdateAsync(id, request, cancellationToken));
    }

    /// <param name="id">Identificador do ativo.</param>
    /// <param name="cancellationToken">Cancelado quando o cliente encerra a requisição.</param>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Exclui um ativo",
        Description = "Só é possível excluir ativos sem ordens vinculadas.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Ativo excluído.")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ativo inexistente.", typeof(ProblemDetails))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [SwaggerResponse(StatusCodes.Status409Conflict, "O ativo possui ordens e não pode ser excluído.", typeof(ProblemDetails))]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        await _ativoService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
