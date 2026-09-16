using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ativos;

public class AtivoResponse
{
    /// <example>1</example>
    public int Id { get; set; }
    /// <example>PETR4</example>
    public string Ticker { get; set; } = string.Empty;
    /// <example>Petrobras PN</example>
    public string Nome { get; set; } = string.Empty;
    /// <example>Acao</example>
    public TipoAtivo Tipo { get; set; }
    /// <example>38.12</example>
    public decimal PrecoAtual { get; set; }
    /// <example>2026-01-02T10:00:00Z</example>
    public DateTime CriadoEm { get; set; }
}
