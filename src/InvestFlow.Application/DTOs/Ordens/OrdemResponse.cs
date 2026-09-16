using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ordens;

public class OrdemResponse
{
    /// <example>51</example>
    public int Id { get; set; }
    /// <example>1</example>
    public int AtivoId { get; set; }

    /// <summary>Achatado de Ordem.Ativo.Nome.</summary>
    /// <example>Petrobras PN</example>
    public string NomeAtivo { get; set; } = string.Empty;

    /// <example>Compra</example>
    public LadoOrdem Lado { get; set; }
    /// <example>200</example>
    public int Quantidade { get; set; }
    /// <example>38.15</example>
    public decimal PrecoExecucao { get; set; }

    /// <summary>Quantidade x PrecoExecucao, calculado pela entidade.</summary>
    /// <example>7630.00</example>
    public decimal ValorFinanceiro { get; set; }

    /// <example>2026-05-15T00:27:00Z</example>
    public DateTime DataExecucao { get; set; }
    /// <example>Pendente</example>
    public StatusOrdem Status { get; set; }
}
