namespace InvestFlow.Application.Validation;

/// <summary>Limites usados nas DataAnnotations dos requests.</summary>
internal static class Limites
{
    // Colunas monetárias são decimal(18,4): até 14 dígitos inteiros e 4 casas decimais.
    public const string PrecoMinimo = "0.0001";
    public const string PrecoMaximo = "99999999999999.9999";
    public const string MensagemPreco = "{0} deve estar entre {1} e {2}.";

    public const int BuscaMaxLength = 100;
}
