using HealthChecks.UI.Client;
using InvestFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace InvestFlow.Api.Configuration;

public static class HealthChecksExtensions
{
    public const string RotaDetalhada = "/health";
    public const string RotaLiveness = "/health/live";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database");

        return services;
    }

    /// <summary>
    /// /health executa todos os checks e responde em JSON detalhado; /health/live não executa nenhum
    /// (Predicate = false) e só confirma que o processo está respondendo.
    /// </summary>
    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(RotaDetalhada, new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        app.MapHealthChecks(RotaLiveness, new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        return app;
    }
}
