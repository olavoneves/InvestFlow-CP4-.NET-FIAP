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

    /// <summary>Limite global, aplicado a toda requisição (exceto as rotas isentas).</summary>
    public JanelaFixaSettings Global { get; set; } = new() { PermitLimit = 30, WindowSeconds = 10 };

    /// <summary>Limite da política nomeada "estrito", aplicada endpoint a endpoint.</summary>
    public JanelaFixaSettings Estrito { get; set; } = new() { PermitLimit = 5, WindowSeconds = 10 };
}

public sealed class JanelaFixaSettings
{
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; }

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; }
}

public static class RateLimitingExtensions
{
    /// <summary>Política nomeada mais restritiva, usada via [EnableRateLimiting] em endpoints específicos.</summary>
    public const string PoliticaEstrita = "estrito";

    /// <summary>
    /// Rate limiting nativo com janela fixa por IP do cliente: um limite global para toda a API e a política
    /// nomeada "estrito" para os endpoints que a declararem. Numa requisição a um endpoint com a política,
    /// os dois limites são consumidos; basta um deles se esgotar para a resposta ser 429.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingSettings>()
            .Bind(configuration.GetSection(RateLimitingSettings.Secao))
            .Validate(settings => JanelaValida(settings.Global) && JanelaValida(settings.Estrito),
                "RateLimiting: PermitLimit e WindowSeconds devem ser maiores que zero em Global e Estrito.")
            .ValidateOnStart();

        services.AddRateLimiter(_ => { });

        // Configurado via IOptions para ler os limites já com os overrides de configuração aplicados.
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingSettings>>((options, settingsOptions) =>
            {
                var settings = settingsOptions.Value;

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RotaIsenta(context.Request.Path)
                        ? RateLimitPartition.GetNoLimiter("isenta")
                        : JanelaFixaPorIp(context, settings.Global));

                options.AddPolicy(PoliticaEstrita, context => JanelaFixaPorIp(context, settings.Estrito));

                options.OnRejected = EscreverRejeicaoAsync;
            });

        return services;
    }

    /// <summary>
    /// Rotas fora do limite global. A decisão é tomada só pelo path da própria requisição, então as
    /// chamadas de API disparadas pelo "Execute" do Swagger UI (que vão para /api/...) continuam limitadas.
    /// </summary>
    public static bool RotaIsenta(PathString path) =>
        // Assets e swagger.json: só abrir a página do Swagger UI já dispara várias requisições.
        path.StartsWithSegments("/swagger") ||
        // Health checks são sondagens de infraestrutura, consultadas em alta frequência por orquestradores e
        // monitores. Limitá-las faria o monitor receber 429 e considerar a aplicação fora do ar.
        path.Equals(HealthChecksExtensions.RotaDetalhada, StringComparison.OrdinalIgnoreCase) ||
        path.Equals(HealthChecksExtensions.RotaLiveness, StringComparison.OrdinalIgnoreCase);

    private static bool JanelaValida(JanelaFixaSettings janela) =>
        janela is { PermitLimit: > 0, WindowSeconds: > 0 };

    private static RateLimitPartition<string> JanelaFixaPorIp(HttpContext context, JanelaFixaSettings janela) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = janela.PermitLimit,
                Window = TimeSpan.FromSeconds(janela.WindowSeconds),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true,
            });

    // Compartilhado pelas duas políticas: o OnRejectedContext não informa qual limite rejeitou, então o
    // detalhe é genérico e o tempo de espera vem do próprio lease rejeitado.
    private static async ValueTask EscreverRejeicaoAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var tempo)
            ? tempo
            : TimeSpan.FromSeconds(1);
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
            $"Limite de requisições excedido para este cliente. Tente novamente em {segundos}s.");

        await ApiProblemDetails.EscreverAsync(httpContext, problem, cancellationToken);
    }
}
