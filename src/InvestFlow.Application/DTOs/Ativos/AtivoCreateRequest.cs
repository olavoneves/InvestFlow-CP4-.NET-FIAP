using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ativos;

public class AtivoCreateRequest
{
    /// <summary>Código de negociação. Gravado sem espaços nas pontas e em maiúsculas.</summary>
    /// <example>ITUB4</example>
    [Required(ErrorMessage = "Ticker é obrigatório.")]
    [StringLength(Ativo.TickerMaxLength, ErrorMessage = "Ticker deve ter no máximo {1} caracteres.")]
    public string Ticker { get; set; } = string.Empty;

    /// <example>Itaú Unibanco PN</example>
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(Ativo.NomeMaxLength, ErrorMessage = "Nome deve ter no máximo {1} caracteres.")]
    public string Nome { get; set; } = string.Empty;

    /// <example>Acao</example>
    [EnumDataType(typeof(TipoAtivo), ErrorMessage = "Tipo inválido.")]
    public TipoAtivo Tipo { get; set; }

    /// <example>33.47</example>
    [Range(typeof(decimal), Limites.PrecoMinimo, Limites.PrecoMaximo, ErrorMessage = Limites.MensagemPreco,
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal PrecoAtual { get; set; }
}
