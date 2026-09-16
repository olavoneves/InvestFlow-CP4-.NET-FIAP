using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestFlow.IntegrationTests.Api;

internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<T> LerAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Options))!;

    /// <summary>Ticker único por teste (máximo 10 caracteres), para os testes não colidirem no mesmo banco.</summary>
    public static string NovoTicker() => "T" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
