namespace InvestFlow.Application.Exceptions;

/// <summary>
/// Dados de entrada inválidos, agrupados por campo. Convertida em 400 (ValidationProblemDetails)
/// pelo middleware global da Api.
/// </summary>
public class ValidationException : Exception
{
    public const string MensagemPadrao = "Um ou mais erros de validação ocorreram.";

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(MensagemPadrao)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    public ValidationException(string campo, string mensagem)
        : this(new Dictionary<string, string[]> { [campo] = new[] { mensagem } })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
