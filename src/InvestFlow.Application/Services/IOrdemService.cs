using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ordens;

namespace InvestFlow.Application.Services;

public interface IOrdemService
{
    /// <exception cref="Exceptions.ValidationException">Paginação ou filtros inválidos.</exception>
    Task<PagedResult<OrdemResponse>> GetPagedAsync(OrdemQuery query, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.NotFoundException">Ordem inexistente.</exception>
    Task<OrdemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.ValidationException">Dados inválidos ou ativo inexistente.</exception>
    Task<OrdemResponse> CreateAsync(OrdemCreateRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.NotFoundException">Ordem inexistente.</exception>
    /// <exception cref="Domain.Exceptions.DomainException">Ordem não está pendente.</exception>
    Task<OrdemResponse> ExecutarAsync(int id, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.NotFoundException">Ordem inexistente.</exception>
    /// <exception cref="Domain.Exceptions.DomainException">Ordem já executada ou cancelada.</exception>
    Task<OrdemResponse> CancelarAsync(int id, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.NotFoundException">Ordem inexistente.</exception>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
