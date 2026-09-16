namespace InvestFlow.Domain.Exceptions;

/// <summary>
/// Violação de regra de negócio do domínio. Tratada pelo middleware global da Api e
/// convertida em ProblemDetails.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
