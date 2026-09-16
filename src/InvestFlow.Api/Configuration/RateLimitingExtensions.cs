using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using InvestFlow.Api.Errors;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace InvestFlow.Api.Configuration;

/// <summary>Limites do rate limiting (seção "RateLimiting" da configuração).</summary>
public sealed class RateLimitingSettings
{
    public const string Secao = "RateLimiting";

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; } = 10;
}

public static class RateLimitingExtensions
{
    /// <summary>Política nomeada aplicada nos controllers.</summary>
    public const string PoliticaFixa = "fixed";

    /// <summary>
    /// Rate limiting nativo: janela fixa por IP do cliente, aplicado globalmente e também exposto como a
    /// política nomeada "fixed". Os assets do Swagger UI ficam de fora: só abrir a página já dispara mais
    /// requisições do que o limite permite.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingSettings>()
            .Bind(configuration.GetSection(RateLimitingSettings.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(_ => { });

        // Configurado via IOptions para ler os limites já com os overrides de configuração aplicados.
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingSettings>>((options, settingsOptions) =>
            {
                var settings = settingsOptions.Value;

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    context.Request.Path.StartsWithSegments("/swagger")
                        ? RateLimitPartition.GetNoLimiter("swagger")
                        : JanelaFixaPorIp(context, settings));

                options.AddPolicy(PoliticaFixa, context => JanelaFixaPorIp(context, settings));

                options.OnRejected = (context, cancellationToken) =>
                    EscreverRejeicaoAsync(context, settings, cancellationToken);
            });

        return services;
    }

    private static RateLimitPartition<string> JanelaFixaPorIp(HttpContext context, RateLimitingSettings settings) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true,
            });

    private static async ValueTask EscreverRejeicaoAsync(
        OnRejectedContext context, RateLimitingSettings settings, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var tempo)
            ? tempo
            : TimeSpan.FromSeconds(settings.WindowSeconds);
        var segundos = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

        httpContext.Response.Headers.RetryAfter = segundos.ToString(CultureInfo.InvariantCulture);

        httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimitingExtensions).FullName!)
            .LogWarning("Rate limit excedido para {ClientIp} em {Method} {Path}. Retry-After: {RetryAfter}s.",
                httpContext.Connection.RemoteIpAddress?.ToString(), httpContext.Request.Method,
                httpContext.Request.Path, segundos);

        var problem = ApiProblemDetails.Criar(
            httpContext,
            StatusCodes.Status429TooManyRequests,
            ApiProblemDetails.TituloMuitasRequisicoes,
            $"Limite de {settings.PermitLimit} requisições a cada {settings.WindowSeconds}s excedido. " +
            $"Tente novamente em {segundos}s.");

        await ApiProblemDetails.EscreverAsync(httpContext, problem, cancellationToken);
    }
}
