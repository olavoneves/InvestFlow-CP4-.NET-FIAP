using InvestFlow.Domain.Enums;
using InvestFlow.Domain.Exceptions;

namespace InvestFlow.Domain.Entities;

public class Ativo
{
    public const int TickerMaxLength = 10;
    public const int NomeMaxLength = 100;

    private readonly List<Ordem> _ordens = new();

    // Construtor sem parâmetros exigido pelo EF Core para materialização.
    private Ativo()
    {
        Ticker = string.Empty;
        Nome = string.Empty;
    }

    public Ativo(string ticker, string nome, TipoAtivo tipo, decimal precoAtual)
    {
        Ticker = ValidarTicker(ticker);
        Nome = ValidarNome(nome);
        Tipo = ValidarTipo(tipo);
        PrecoAtual = ValidarPreco(precoAtual);
        CriadoEm = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string Ticker { get; private set; }
    public string Nome { get; private set; }
    public TipoAtivo Tipo { get; private set; }
    public decimal PrecoAtual { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public IReadOnlyCollection<Ordem> Ordens => _ordens.AsReadOnly();

    public void Atualizar(string nome, TipoAtivo tipo, decimal precoAtual)
    {
        Nome = ValidarNome(nome);
        Tipo = ValidarTipo(tipo);
        PrecoAtual = ValidarPreco(precoAtual);
    }

    public void AtualizarPreco(decimal novoPreco)
    {
        PrecoAtual = ValidarPreco(novoPreco);
    }

    private static string ValidarTicker(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new DomainException("O ticker do ativo é obrigatório.");

        var normalizado = ticker.Trim().ToUpperInvariant();
        if (normalizado.Length > TickerMaxLength)
            throw new DomainException($"O ticker deve ter no máximo {TickerMaxLength} caracteres.");

        return normalizado;
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do ativo é obrigatório.");

        var normalizado = nome.Trim();
        if (normalizado.Length > NomeMaxLength)
            throw new DomainException($"O nome deve ter no máximo {NomeMaxLength} caracteres.");

        return normalizado;
    }

    private static TipoAtivo ValidarTipo(TipoAtivo tipo)
    {
        if (!Enum.IsDefined(tipo))
            throw new DomainException("Tipo de ativo inválido.");

        return tipo;
    }

    private static decimal ValidarPreco(decimal preco)
    {
        if (preco <= 0)
            throw new DomainException("O preço atual do ativo deve ser maior que zero.");

        return preco;
    }
}
