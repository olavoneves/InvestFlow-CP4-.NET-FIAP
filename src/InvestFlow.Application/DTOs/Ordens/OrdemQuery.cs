using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Common;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ordens;

/// <summary>Paginação e filtros opcionais da listagem de ordens.</summary>
public class OrdemQuery : PageRequest, IValidatableObject
{
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "AtivoId deve ser maior que zero.")]
    public int? AtivoId { get; set; }

    /// <example>Executada</example>
    [EnumDataType(typeof(StatusOrdem), ErrorMessage = "Status inválido.")]
    public StatusOrdem? Status { get; set; }

    /// <summary>Início do período de execução (inclusivo).</summary>
    /// <example>2026-01-01T00:00:00Z</example>
    public DateTime? DataInicio { get; set; }

    /// <summary>Fim do período de execução (inclusivo).</summary>
    /// <example>2026-12-31T23:59:59Z</example>
    public DateTime? DataFim { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataInicio > DataFim)
        {
            yield return new ValidationResult(
                "DataFim deve ser maior ou igual a DataInicio.",
                new[] { nameof(DataFim) });
        }
    }
}
