using System.ComponentModel.DataAnnotations;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ativos;

/// <summary>Dados alteráveis de um ativo. O ticker é a identidade de mercado e não muda.</summary>
public class AtivoUpdateRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(Ativo.NomeMaxLength, ErrorMessage = "Nome deve ter no máximo {1} caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [EnumDataType(typeof(TipoAtivo), ErrorMessage = "Tipo inválido.")]
    public TipoAtivo Tipo { get; set; }

    [Range(typeof(decimal), Limites.PrecoMinimo, Limites.PrecoMaximo, ErrorMessage = Limites.MensagemPreco,
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal PrecoAtual { get; set; }
}
