using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ordens;

public class OrdemResponse
{
    public int Id { get; set; }
    public int AtivoId { get; set; }

    /// <summary>Achatado de Ordem.Ativo.Nome.</summary>
    public string NomeAtivo { get; set; } = string.Empty;

    public LadoOrdem Lado { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoExecucao { get; set; }

    /// <summary>Quantidade x PrecoExecucao, calculado pela entidade.</summary>
    public decimal ValorFinanceiro { get; set; }

    public DateTime DataExecucao { get; set; }
    public StatusOrdem Status { get; set; }
}
