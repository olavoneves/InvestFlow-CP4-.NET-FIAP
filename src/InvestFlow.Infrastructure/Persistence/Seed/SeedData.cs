using InvestFlow.Domain.Enums;

namespace InvestFlow.Infrastructure.Persistence.Seed;

/// <summary>
/// Dados iniciais aplicados pela migration (HasData): 5 ativos e 60 ordens, volume suficiente
/// para demonstrar a paginação (6 páginas com o PageSize padrão de 10).
/// Todos os valores são determinísticos — nada de DateTime.Now ou Random — para que o EF não
/// detecte mudança no modelo a cada nova migration.
/// </summary>
public static class SeedData
{
    public const int TotalOrdens = 60;

    private static readonly DateTime DataBase = new(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);

    private static readonly SeedAtivo[] AtivosBase =
    {
        new(1, "PETR4", "Petrobras PN", TipoAtivo.Acao, 38.1200m, 100),
        new(2, "VALE3", "Vale ON", TipoAtivo.Acao, 61.4500m, 100),
        new(3, "HGLG11", "CSHG Logística FII", TipoAtivo.FII, 162.3500m, 10),
        new(4, "IPCA2035", "Tesouro IPCA+ 2035", TipoAtivo.RendaFixa, 2150.7800m, 1),
        new(5, "WINZ26", "Mini Índice Futuro Dez/26", TipoAtivo.Derivativo, 131250.0000m, 1),
    };

    public static IEnumerable<object> Ativos() =>
        AtivosBase.Select(a => new
        {
            a.Id,
            a.Ticker,
            a.Nome,
            a.Tipo,
            a.PrecoAtual,
            CriadoEm = DataBase,
        });

    public static IEnumerable<object> Ordens()
    {
        for (var i = 1; i <= TotalOrdens; i++)
        {
            var ativo = AtivosBase[(i - 1) % AtivosBase.Length];

            // Variação de -1,0% a +1,0% sobre o preço atual, em passos de 0,1%.
            var variacao = ((i * 37 % 21) - 10) / 1000m;
            var preco = Math.Round(ativo.PrecoAtual * (1 + variacao), 4);

            yield return new
            {
                Id = i,
                AtivoId = ativo.Id,
                Lado = i % 3 == 0 ? LadoOrdem.Venda : LadoOrdem.Compra,
                Quantidade = ((i % 7) + 1) * ativo.LoteMinimo,
                PrecoExecucao = preco,
                DataExecucao = DataBase.AddDays(i * 2).AddMinutes(i * 17),
                Status = StatusDa(i),
            };
        }
    }

    // A cada 10 ordens uma foi cancelada; as mais recentes ainda estão pendentes; o restante foi executado.
    private static StatusOrdem StatusDa(int i) => i switch
    {
        _ when i % 10 == 0 => StatusOrdem.Cancelada,
        >= 51 => StatusOrdem.Pendente,
        _ => StatusOrdem.Executada,
    };

    private sealed record SeedAtivo(int Id, string Ticker, string Nome, TipoAtivo Tipo, decimal PrecoAtual, int LoteMinimo);
}
