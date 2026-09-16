using System.ComponentModel.DataAnnotations;
using ValidationException = InvestFlow.Application.Exceptions.ValidationException;

namespace InvestFlow.Application.Validation;

/// <summary>
/// Executa as DataAnnotations (e <see cref="IValidatableObject"/>) de um request. Os services validam
/// por conta própria para não depender de a Api ter feito a validação de modelo antes.
/// </summary>
public static class RequestValidator
{
    public static void Validar(object request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(request);

        if (Validator.TryValidateObject(request, contexto, resultados, validateAllProperties: true))
            return;

        var errors = resultados
            .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : new[] { string.Empty })
                .Select(campo => (Campo: campo, Mensagem: r.ErrorMessage ?? "Valor inválido.")))
            .GroupBy(e => e.Campo)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Mensagem).ToArray());

        throw new ValidationException(errors);
    }
}
