using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

namespace InvestFlow.Api.Configuration;

public static class ObservabilidadeExtensions
{
    public const string ChaveConnectionStringAppInsights = "ApplicationInsights:ConnectionString";
    public const string VariavelAmbienteAppInsights = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    private const string TemplateConsole =
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{RequestId}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Serilog com log estruturado: console legível e arquivo em JSON compacto (um evento por linha, com
    /// todas as propriedades), rotacionado por dia. Níveis mínimos vêm da seção "Serilog" da configuração.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, logger) => logger
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
            .WriteTo.Console(outputTemplate: TemplateConsole)
            .WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "investflow-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true));

        return builder;
    }

    /// <summary>
    /// Coloca o RequestId (TraceIdentifier) no LogContext, para que todo log da requisição — inclusive o
    /// resumo do UseSerilogRequestLogging — carregue a propriedade.
    /// </summary>
    public static IApplicationBuilder UseRequestIdLogContext(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
            {
                await next(context);
            }
        });

    /// <summary>
    /// Liga o Application Insights só quando há connection string (appsettings ou variável de ambiente
    /// APPLICATIONINSIGHTS_CONNECTION_STRING). Sem ela, registra um <see cref="TelemetryClient"/> desabilitado,
    /// para que quem emite métricas funcione igual e a API suba normalmente.
    /// </summary>
    /// <returns>true se a telemetria foi habilitada.</returns>
    public static bool AddTelemetria(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ObterConnectionStringAppInsights(configuration);

        if (connectionString is not null)
        {
            services.AddApplicationInsightsTelemetry(options => options.ConnectionString = connectionString);
            return true;
        }

        services.AddSingleton(_ => new TelemetryConfiguration { DisableTelemetry = true });
        services.AddSingleton(sp => new TelemetryClient(sp.GetRequiredService<TelemetryConfiguration>()));
        return false;
    }

    public static string? ObterConnectionStringAppInsights(IConfiguration configuration)
    {
        var valor = configuration[ChaveConnectionStringAppInsights];
        if (string.IsNullOrWhiteSpace(valor))
            valor = configuration[VariavelAmbienteAppInsights];

        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
