using InvestFlow.Domain.Enums;

namespace InvestFlow.Application.DTOs.Ativos;

public class AtivoResponse
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public TipoAtivo Tipo { get; set; }
    public decimal PrecoAtual { get; set; }
    public DateTime CriadoEm { get; set; }
}
