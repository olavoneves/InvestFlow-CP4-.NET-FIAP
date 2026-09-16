using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Filters;

namespace InvestFlow.Domain.Repositories;

public interface IAtivoRepository
{
    /// <summary>Consulta somente leitura (sem rastreamento).</summary>
    Task<Ativo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Retorna a entidade rastreada, para alteração seguida de <see cref="UpdateAsync"/>.</summary>
    Task<Ativo?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Consulta somente leitura (sem rastreamento). O ticker é comparado normalizado em maiúsculas.</summary>
    Task<Ativo?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> TickerExistsAsync(string ticker, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Ativo> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        AtivoFiltro? filtro = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Ativo ativo, CancellationToken cancellationToken = default);

    Task UpdateAsync(Ativo ativo, CancellationToken cancellationToken = default);

    Task DeleteAsync(Ativo ativo, CancellationToken cancellationToken = default);
}
