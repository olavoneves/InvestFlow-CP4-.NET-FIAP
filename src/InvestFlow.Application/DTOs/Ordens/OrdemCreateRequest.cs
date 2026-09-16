using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ordens;

public class OrdemCreateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "AtivoId deve ser maior que zero.")]
    public int AtivoId { get; set; }

    [EnumDataType(typeof(LadoOrdem), ErrorMessage = "Lado inválido.")]
    public LadoOrdem Lado { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
    public int Quantidade { get; set; }

    [Range(typeof(decimal), Limites.PrecoMinimo, Limites.PrecoMaximo, ErrorMessage = Limites.MensagemPreco,
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal PrecoExecucao { get; set; }

    /// <summary>Data/hora da execução (UTC). Se omitida, usa o momento do registro.</summary>
    public DateTime? DataExecucao { get; set; }
}
