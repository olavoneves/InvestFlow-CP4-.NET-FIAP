using FluentAssertions;
using InvestFlow.Api.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestFlow.IntegrationTests.Api;

public class ObservabilidadeExtensionsTests
{
    private const string ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example.invalid/";

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", "")]
    public void ObterConnectionString_Ausente_RetornaNull(string? appSettings, string? variavelAmbiente)
    {
        var configuration = Configuracao(appSettings, variavelAmbiente);

        ObservabilidadeExtensions.ObterConnectionStringAppInsights(configuration).Should().BeNull();
    }

    [Theory]
    [InlineData(ConnectionString, null)]
    [InlineData("", ConnectionString)]
    [InlineData(ConnectionString, "InstrumentationKey=outra")]
    public void ObterConnectionString_AppSettingsTemPrioridadeSobreVariavelDeAmbiente(string? appSettings, string? variavelAmbiente)
    {
        var configuration = Configuracao(appSettings, variavelAmbiente);

        ObservabilidadeExtensions.ObterConnectionStringAppInsights(configuration).Should().Be(ConnectionString);
    }

    [Fact]
    public void AddTelemetria_SemConnectionString_RegistraTelemetryClientDesabilitado()
    {
        var services = new ServiceCollection();

        var habilitada = services.AddTelemetria(Configuracao(null, null));

        habilitada.Should().BeFalse();
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<TelemetryClient>();
        client.IsEnabled().Should().BeFalse();

        // Emitir métrica sem Application Insights não pode falhar.
        var emitir = () =>
        {
            client.GetMetric("ordens.criadas").TrackValue(1);
            client.Flush();
        };
        emitir.Should().NotThrow();
    }

    [Fact]
    public void AddTelemetria_ComConnectionString_HabilitaApplicationInsights()
    {
        var services = new ServiceCollection();

        var habilitada = services.AddTelemetria(Configuracao(ConnectionString, null));

        habilitada.Should().BeTrue();
        services.Should().Contain(d => d.ServiceType == typeof(TelemetryClient));
    }

    private static IConfiguration Configuracao(string? appSettings, string? variavelAmbiente) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ObservabilidadeExtensions.ChaveConnectionStringAppInsights] = appSettings,
                [ObservabilidadeExtensions.VariavelAmbienteAppInsights] = variavelAmbiente,
            })
            .Build();
}
