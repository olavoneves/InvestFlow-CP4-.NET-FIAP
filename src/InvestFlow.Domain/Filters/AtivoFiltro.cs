using InvestFlow.Domain.Enums;

namespace InvestFlow.Domain.Filters;

/// <summary>Filtros opcionais da listagem de ativos. Propriedades nulas são ignoradas.</summary>
public sealed record AtivoFiltro(string? Ticker = null, TipoAtivo? Tipo = null);
