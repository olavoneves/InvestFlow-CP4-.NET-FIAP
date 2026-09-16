using InvestFlow.Domain.Enums;

namespace InvestFlow.Domain.Filters;

/// <summary>Filtros opcionais da listagem de ativos. Propriedades nulas são ignoradas.</summary>
/// <param name="Busca">Trecho procurado no ticker ou no nome do ativo, sem diferenciar maiúsculas de minúsculas.</param>
/// <param name="Tipo">Tipo exato do ativo.</param>
public sealed record AtivoFiltro(string? Busca = null, TipoAtivo? Tipo = null);
