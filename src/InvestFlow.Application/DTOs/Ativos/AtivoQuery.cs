using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Common;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ativos;

/// <summary>Paginação e filtros opcionais da listagem de ativos.</summary>
public class AtivoQuery : PageRequest
{
    /// <example>Acao</example>
    [EnumDataType(typeof(TipoAtivo), ErrorMessage = "Tipo inválido.")]
    public TipoAtivo? Tipo { get; set; }

    /// <summary>Trecho do ticker ou do nome, sem diferenciar maiúsculas de minúsculas.</summary>
    /// <example>petr</example>
    [StringLength(Limites.BuscaMaxLength, ErrorMessage = "Busca deve ter no máximo {1} caracteres.")]
    public string? Busca { get; set; }
}
