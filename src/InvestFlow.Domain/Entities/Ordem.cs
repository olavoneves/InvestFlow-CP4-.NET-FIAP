using InvestFlow.Domain.Enums;
using InvestFlow.Domain.Exceptions;

namespace InvestFlow.Domain.Entities;

public class Ordem
{
    // Construtor sem parâmetros exigido pelo EF Core para materialização.
    private Ordem()
    {
    }

    public Ordem(int ativoId, LadoOrdem lado, int quantidade, decimal precoExecucao, DateTime dataExecucao)
    {
        if (ativoId <= 0)
            throw new DomainException("A ordem deve estar vinculada a um ativo válido.");

        if (!Enum.IsDefined(lado))
            throw new DomainException("Lado da ordem inválido.");

        AtivoId = ativoId;
        Lado = lado;
        Quantidade = ValidarQuantidade(quantidade);
        PrecoExecucao = ValidarPrecoExecucao(precoExecucao);
        DataExecucao = dataExecucao;
        Status = StatusOrdem.Pendente;
    }

    public int Id { get; private set; }
    public int AtivoId { get; private set; }
    public LadoOrdem Lado { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoExecucao { get; private set; }
    public DateTime DataExecucao { get; private set; }
    public StatusOrdem Status { get; private set; }

    public Ativo? Ativo { get; private set; }

    /// <summary>Valor financeiro da ordem (Quantidade x PrecoExecucao). Calculado, não é persistido.</summary>
    public decimal ValorFinanceiro => Quantidade * PrecoExecucao;

    public void Executar()
    {
        if (Status != StatusOrdem.Pendente)
            throw new DomainException($"Apenas ordens pendentes podem ser executadas. Status atual: {Status}.");

        Status = StatusOrdem.Executada;
    }

    public void Cancelar()
    {
        if (Status == StatusOrdem.Executada)
            throw new DomainException("Não é possível cancelar uma ordem já executada.");

        if (Status == StatusOrdem.Cancelada)
            throw new DomainException("A ordem já está cancelada.");

        Status = StatusOrdem.Cancelada;
    }

    public void AlterarQuantidade(int quantidade)
    {
        GarantirPendente();
        Quantidade = ValidarQuantidade(quantidade);
    }

    public void AlterarPrecoExecucao(decimal precoExecucao)
    {
        GarantirPendente();
        PrecoExecucao = ValidarPrecoExecucao(precoExecucao);
    }

    private void GarantirPendente()
    {
        if (Status != StatusOrdem.Pendente)
            throw new DomainException($"Apenas ordens pendentes podem ser alteradas. Status atual: {Status}.");
    }

    private static int ValidarQuantidade(int quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("A quantidade da ordem deve ser maior que zero.");

        return quantidade;
    }

    private static decimal ValidarPrecoExecucao(decimal precoExecucao)
    {
        if (precoExecucao <= 0)
            throw new DomainException("O preço de execução da ordem deve ser maior que zero.");

        return precoExecucao;
    }
}
