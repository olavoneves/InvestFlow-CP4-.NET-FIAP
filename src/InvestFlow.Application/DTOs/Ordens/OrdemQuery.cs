using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Common;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ordens;

/// <summary>Paginação e filtros opcionais da listagem de ordens.</summary>
public class OrdemQuery : PageRequest, IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "AtivoId deve ser maior que zero.")]
    public int? AtivoId { get; set; }

    [EnumDataType(typeof(StatusOrdem), ErrorMessage = "Status inválido.")]
    public StatusOrdem? Status { get; set; }

    /// <summary>Início do período de execução (inclusivo).</summary>
    public DateTime? DataInicio { get; set; }

    /// <summary>Fim do período de execução (inclusivo).</summary>
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
