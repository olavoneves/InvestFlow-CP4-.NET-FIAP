using System.Globalization;
using InvestFlow.Infrastructure.Persistence;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestFlow.IntegrationTests.Api;

/// <summary>
/// Sobe a API real (Program.cs completo) em ambiente Development — logo, as migrations e o seed são
/// aplicados na subida — sobre um SQLite em memória isolado por instância. O TelemetryClient é trocado
/// por um que captura a telemetria em memória.
/// </summary>
public class InvestFlowApiFactory : WebApplicationFactory<Program>
{
    // Limite alto: os testes funcionais não devem esbarrar no rate limiting.
    private const int PermitLimitSemRestricao = 100_000;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly int _globalPermitLimit;
    private readonly int _estritoPermitLimit;
    private readonly int _windowSeconds;

    public InvestFlowApiFactory() : this(PermitLimitSemRestricao, PermitLimitSemRestricao, windowSeconds: 10)
    {
    }

    protected InvestFlowApiFactory(int globalPermitLimit, int estritoPermitLimit, int windowSeconds)
    {
        _globalPermitLimit = globalPermitLimit;
        _estritoPermitLimit = estritoPermitLimit;
        _windowSeconds = windowSeconds;

        // O banco em memória existe enquanto esta conexão estiver aberta.
        _connection.Open();
    }

    public TelemetriaCapturadaChannel Telemetria { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] = _globalPermitLimit.ToString(CultureInfo.InvariantCulture),
                ["RateLimiting:Global:WindowSeconds"] = _windowSeconds.ToString(CultureInfo.InvariantCulture),
                ["RateLimiting:Estrito:PermitLimit"] = _estritoPermitLimit.ToString(CultureInfo.InvariantCulture),
                ["RateLimiting:Estrito:WindowSeconds"] = _windowSeconds.ToString(CultureInfo.InvariantCulture),
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<SqliteAppDbContext>>();
            services.AddDbContext<AppDbContext, SqliteAppDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TelemetryClient>();
            services.RemoveAll<TelemetryConfiguration>();
            services.AddSingleton(_ => new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
                TelemetryChannel = Telemetria,
            });
            services.AddSingleton(sp => new TelemetryClient(sp.GetRequiredService<TelemetryConfiguration>()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}

/// <summary>
/// API com os limites de produção (global 30, estrito 5), numa janela longa para o teste não depender de tempo.
/// </summary>
public sealed class RateLimitedApiFactory : InvestFlowApiFactory
{
    public const int GlobalPermitLimit = 30;
    public const int EstritoPermitLimit = 5;
    public const int WindowSeconds = 60;

    public RateLimitedApiFactory() : this(GlobalPermitLimit, EstritoPermitLimit)
    {
    }

    public RateLimitedApiFactory(int globalPermitLimit, int estritoPermitLimit)
        : base(globalPermitLimit, estritoPermitLimit, WindowSeconds)
    {
    }
}
