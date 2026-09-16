namespace InvestFlow.Infrastructure.Repositories;

internal static class Paginacao
{
    /// <summary>
    /// Os limites de negócio (default 10, máximo 50) são aplicados na Application; aqui só se
    /// rejeitam valores que gerariam um Skip/Take inválido.
    /// </summary>
    public static void Validar(int pageNumber, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
    }

    public static IQueryable<T> Paginar<T>(this IQueryable<T> query, int pageNumber, int pageSize) =>
        query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
}
