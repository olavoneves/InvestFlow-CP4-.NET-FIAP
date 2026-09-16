using System.Collections.Concurrent;
using Microsoft.ApplicationInsights.Channel;

namespace InvestFlow.IntegrationTests.Api;

/// <summary>Canal do Application Insights que guarda a telemetria em memória em vez de enviá-la.</summary>
public sealed class TelemetriaCapturadaChannel : ITelemetryChannel
{
    private readonly ConcurrentQueue<ITelemetry> _itens = new();

    public IReadOnlyCollection<ITelemetry> Itens => _itens.ToArray();

    public bool? DeveloperMode { get; set; }

    public string? EndpointAddress { get; set; }

    public void Send(ITelemetry item) => _itens.Enqueue(item);

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}
