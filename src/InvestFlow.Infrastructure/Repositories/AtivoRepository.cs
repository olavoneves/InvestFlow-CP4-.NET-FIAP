using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Filters;
using InvestFlow.Domain.Repositories;
using InvestFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestFlow.Infrastructure.Repositories;

public class AtivoRepository : IAtivoRepository
{
    private readonly AppDbContext _context;

    public AtivoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Ativo?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Ativos
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Ativo?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Ativos.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Ativo?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var normalizado = NormalizarTicker(ticker);
        return _context.Ativos
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Ticker == normalizado, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Ativos.AnyAsync(a => a.Id == id, cancellationToken);

    public Task<bool> TickerExistsAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var normalizado = NormalizarTicker(ticker);
        return _context.Ativos.AnyAsync(a => a.Ticker == normalizado, cancellationToken);
    }

    public async Task<(IReadOnlyList<Ativo> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        AtivoFiltro? filtro = null,
        CancellationToken cancellationToken = default)
    {
        Paginacao.Validar(pageNumber, pageSize);

        var query = _context.Ativos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro?.Ticker))
        {
            var ticker = NormalizarTicker(filtro.Ticker);
            query = query.Where(a => a.Ticker.Contains(ticker));
        }

        if (filtro?.Tipo is not null)
            query = query.Where(a => a.Tipo == filtro.Tipo);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.Ticker)
            .ThenBy(a => a.Id)
            .Paginar(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Ativo ativo, CancellationToken cancellationToken = default)
    {
        await _context.Ativos.AddAsync(ativo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ativo ativo, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(ativo).State == EntityState.Detached)
            _context.Ativos.Update(ativo);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Ativo ativo, CancellationToken cancellationToken = default)
    {
        _context.Ativos.Remove(ativo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Mesma normalização aplicada pela entidade Ativo ao gravar o ticker.
    private static string NormalizarTicker(string ticker) => ticker.Trim().ToUpperInvariant();
}
