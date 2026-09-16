using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ativos;

public class AtivoCreateRequest
{
    /// <summary>Código de negociação. Gravado sem espaços nas pontas e em maiúsculas.</summary>
    [Required(ErrorMessage = "Ticker é obrigatório.")]
    [StringLength(Ativo.TickerMaxLength, ErrorMessage = "Ticker deve ter no máximo {1} caracteres.")]
    public string Ticker { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(Ativo.NomeMaxLength, ErrorMessage = "Nome deve ter no máximo {1} caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [EnumDataType(typeof(TipoAtivo), ErrorMessage = "Tipo inválido.")]
    public TipoAtivo Tipo { get; set; }

    [Range(typeof(decimal), Limites.PrecoMinimo, Limites.PrecoMaximo, ErrorMessage = Limites.MensagemPreco,
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal PrecoAtual { get; set; }
}
