using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Filters;

namespace InvestFlow.Domain.Repositories;

public interface IOrdemRepository
{
    /// <summary>Consulta somente leitura (sem rastreamento), com o ativo carregado.</summary>
    Task<Ordem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Retorna a entidade rastreada, para alteração seguida de <see cref="UpdateAsync"/>.</summary>
    Task<Ordem?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByAtivoIdAsync(int ativoId, CancellationToken cancellationToken = default);

    /// <summary>Listagem paginada somente leitura, com o ativo carregado, da mais recente para a mais antiga.</summary>
    Task<(IReadOnlyList<Ordem> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        OrdemFiltro? filtro = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Ordem ordem, CancellationToken cancellationToken = default);

    Task UpdateAsync(Ordem ordem, CancellationToken cancellationToken = default);

    Task DeleteAsync(Ordem ordem, CancellationToken cancellationToken = default);
}
