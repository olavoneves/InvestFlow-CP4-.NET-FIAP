using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Filters;
using InvestFlow.Domain.Repositories;
using InvestFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestFlow.Infrastructure.Repositories;

public class OrdemRepository : IOrdemRepository
{
    private readonly AppDbContext _context;

    public OrdemRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Ordem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Ordens
            .AsNoTracking()
            .Include(o => o.Ativo)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Ordem?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Ordens.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<bool> ExistsByAtivoIdAsync(int ativoId, CancellationToken cancellationToken = default) =>
        _context.Ordens.AnyAsync(o => o.AtivoId == ativoId, cancellationToken);

    public async Task<(IReadOnlyList<Ordem> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        OrdemFiltro? filtro = null,
        CancellationToken cancellationToken = default)
    {
        Paginacao.Validar(pageNumber, pageSize);

        var query = _context.Ordens.AsNoTracking();

        if (filtro is not null)
        {
            if (filtro.AtivoId is not null)
                query = query.Where(o => o.AtivoId == filtro.AtivoId);

            if (filtro.Lado is not null)
                query = query.Where(o => o.Lado == filtro.Lado);

            if (filtro.Status is not null)
                query = query.Where(o => o.Status == filtro.Status);

            if (filtro.DataInicio is not null)
                query = query.Where(o => o.DataExecucao >= filtro.DataInicio);

            if (filtro.DataFim is not null)
                query = query.Where(o => o.DataExecucao <= filtro.DataFim);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Ordenação estável (DataExecucao + Id) para que Skip/Take não repita nem pule ordens entre páginas.
        var items = await query
            .Include(o => o.Ativo)
            .OrderByDescending(o => o.DataExecucao)
            .ThenByDescending(o => o.Id)
            .Paginar(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Ordem ordem, CancellationToken cancellationToken = default)
    {
        await _context.Ordens.AddAsync(ordem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ordem ordem, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(ordem).State == EntityState.Detached)
        {
            _context.Ordens.Update(ordem);

            // Update() percorre o grafo: sem isto o ativo carregado junto (GetByIdAsync) também
            // seria marcado como alterado e regravado.
            if (ordem.Ativo is not null)
                _context.Entry(ordem.Ativo).State = EntityState.Unchanged;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Ordem ordem, CancellationToken cancellationToken = default)
    {
        _context.Ordens.Remove(ordem);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
