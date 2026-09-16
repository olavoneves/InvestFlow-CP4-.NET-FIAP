using System.Reflection;
using InvestFlow.Application.Services;
using Microsoft.OpenApi.Models;

namespace InvestFlow.Api.Configuration;

public static class SwaggerExtensions
{
    private const string Descricao = """
        API REST de investimentos: cadastro de **ativos** e registro de **ordens** de compra e venda.

        - **Paginação:** toda listagem aceita `PageNumber` (padrão 1) e `PageSize` (padrão 10, máximo 50) e
          devolve `items`, `totalCount`, `totalPages`, `hasNext` e `hasPrevious`.
        - **Erros:** respostas no formato ProblemDetails (RFC 9457). Validação → 400 com `errors` agrupados por
          campo; recurso inexistente → 404; regra de negócio violada → 409; erro inesperado → 500.
        - **Rate limiting:** limite global de 30 requisições a cada 10 segundos por IP; `GET /api/v1/ativos` tem
          ainda a política estrita de 5 requisições a cada 10 segundos. Acima do limite → 429 com header `Retry-After`.
          `/swagger` e os health checks ficam fora do limite.
        - **Saúde:** `/health` (detalhado, inclui o banco) e `/health/live` (processo no ar).
        """;

    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "InvestFlow API",
                Version = "v1",
                Description = Descricao,
                Contact = new OpenApiContact
                {
                    Name = "InvestFlow — CP4 .NET FIAP",
                    Url = new Uri("https://github.com/olavoneves/InvestFlow-CP4-.NET-FIAP"),
                },
            });

            options.EnableAnnotations();

            // XML da Api (controllers) e da Application (DTOs, com os <example>).
            foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), typeof(IAtivoService).Assembly })
            {
                var xml = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xml))
                    options.IncludeXmlComments(xml, includeControllerXmlComments: true);
            }
        });

        return services;
    }

    public static WebApplication UseApiSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "InvestFlow API v1");
            options.DocumentTitle = "InvestFlow API";
        });

        return app;
    }
}
