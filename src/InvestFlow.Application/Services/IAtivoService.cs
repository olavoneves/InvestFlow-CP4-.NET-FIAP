using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ativos;

namespace InvestFlow.Application.Services;

public interface IAtivoService
{
    /// <exception cref="Exceptions.ValidationException">Paginação ou filtros inválidos.</exception>
    Task<PagedResult<AtivoResponse>> GetPagedAsync(AtivoQuery query, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.NotFoundException">Ativo inexistente.</exception>
    Task<AtivoResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.ValidationException">Dados inválidos ou ticker já cadastrado.</exception>
    Task<AtivoResponse> CreateAsync(AtivoCreateRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.ValidationException">Dados inválidos.</exception>
    /// <exception cref="Exceptions.NotFoundException">Ativo inexistente.</exception>
    Task<AtivoResponse> UpdateAsync(int id, AtivoUpdateRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.NotFoundException">Ativo inexistente.</exception>
    /// <exception cref="Domain.Exceptions.DomainException">Ativo possui ordens.</exception>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
