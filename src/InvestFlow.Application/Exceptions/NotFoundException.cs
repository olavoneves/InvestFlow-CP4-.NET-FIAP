namespace InvestFlow.Application.Exceptions;

/// <summary>Recurso solicitado não existe. Convertida em 404 pelo middleware global da Api.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
