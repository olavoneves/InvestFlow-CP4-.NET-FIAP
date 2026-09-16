using InvestFlow.Domain.Enums;

namespace InvestFlow.Domain.Filters;

/// <summary>Filtros opcionais da listagem de ordens. Propriedades nulas são ignoradas.</summary>
public sealed record OrdemFiltro(
    int? AtivoId = null,
    LadoOrdem? Lado = null,
    StatusOrdem? Status = null,
    DateTime? DataInicio = null,
    DateTime? DataFim = null);
