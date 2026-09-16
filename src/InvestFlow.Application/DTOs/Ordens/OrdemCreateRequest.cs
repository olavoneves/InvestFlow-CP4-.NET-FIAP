using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ordens;

public class OrdemCreateRequest
{
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "AtivoId deve ser maior que zero.")]
    public int AtivoId { get; set; }

    /// <example>Compra</example>
    [EnumDataType(typeof(LadoOrdem), ErrorMessage = "Lado inválido.")]
    public LadoOrdem Lado { get; set; }

    /// <example>100</example>
    [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
    public int Quantidade { get; set; }

    /// <example>38.15</example>
    [Range(typeof(decimal), Limites.PrecoMinimo, Limites.PrecoMaximo, ErrorMessage = Limites.MensagemPreco,
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal PrecoExecucao { get; set; }

    /// <summary>Data/hora da execução (UTC). Se omitida, usa o momento do registro.</summary>
    /// <example>2026-09-16T14:30:00Z</example>
    public DateTime? DataExecucao { get; set; }
}
