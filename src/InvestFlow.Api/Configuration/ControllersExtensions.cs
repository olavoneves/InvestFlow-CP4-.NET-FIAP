using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using InvestFlow.Api.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace InvestFlow.Api.Configuration;

public static class ControllersExtensions
{
    /// <summary>
    /// Controllers com enums serializados por nome e o 400 automático do model binding no mesmo formato
    /// do 400 gerado pela ValidationException (ambos via <see cref="ApiProblemDetails"/>).
    /// </summary>
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                // Os DTOs usam [Required] explícito. Sem isto, um JSON malformado gera também um erro
                // "The request field is required." para o parâmetro, além do erro real de conversão.
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

                // [controller] em minúsculas: vale para o roteamento, a geração de links (Location) e o Swagger.
                options.Conventions.Add(new RouteTokenTransformerConvention(new MinusculasParameterTransformer()));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                // O formatter do MVC troca um Encoder nulo por este internamente; fixá-lo aqui faz o
                // ApiProblemDetails (que escreve fora do MVC) produzir exatamente o mesmo JSON.
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

                // JSON malformado vira "Valor inválido." no campo, sem expor nomes de tipos internos.
                options.AllowInputFormatterExceptionMessages = false;
            });

        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                new BadRequestObjectResult(ApiProblemDetails.Validacao(context.HttpContext, context.ModelState))
                {
                    ContentTypes = { "application/problem+json" },
                });

        return services;
    }

    private sealed class MinusculasParameterTransformer : IOutboundParameterTransformer
    {
        public string? TransformOutbound(object? value) => value?.ToString()?.ToLowerInvariant();
    }
}
