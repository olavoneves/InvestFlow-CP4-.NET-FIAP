using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ativos;

/// <summary>Dados alteráveis de um ativo. O ticker é a identidade de mercado e não muda.</summary>
public class AtivoUpdateRequest
{
    /// <example>Petrobras PN</example>
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(Ativo.NomeMaxLength, ErrorMessage = "Nome deve ter no máximo {1} caracteres.")]
    public string Nome { get; set; } = string.Empty;

    /// <example>Acao</example>
    [EnumDataType(typeof(TipoAtivo), ErrorMessage = "Tipo inválido.")]
    public TipoAtivo Tipo { get; set; }

    /// <example>39.05</example>
    [Range(typeof(decimal), Limites.PrecoMinimo, Limites.PrecoMaximo, ErrorMessage = Limites.MensagemPreco,
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal PrecoAtual { get; set; }
}
