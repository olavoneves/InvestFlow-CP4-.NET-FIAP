using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace InvestFlow.IntegrationTests.Api;

public class RateLimitingTests
{
    [Fact]
    public async Task RequisicaoAcimaDoLimiteNaJanela_Retorna429ComRetryAfterEProblemDetails()
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < RateLimitedApiFactory.PermitLimit; i++)
            (await client.GetAsync("/api/v1/ativos")).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync("/api/v1/ativos");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
        response.Headers.RetryAfter!.Delta.Should().BeGreaterThan(TimeSpan.Zero)
            .And.BeLessOrEqualTo(TimeSpan.FromSeconds(RateLimitedApiFactory.WindowSeconds));
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.LerAsync<ProblemDetails>();
        problem.Status.Should().Be(429);
        problem.Detail.Should().Contain($"{RateLimitedApiFactory.PermitLimit} requisições");
    }

    [Fact]
    public async Task LimiteGlobal_ValeTambemParaEndpointsForaDosControllers()
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < RateLimitedApiFactory.PermitLimit; i++)
            (await client.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Swagger_NaoConsomeOLimite()
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < RateLimitedApiFactory.PermitLimit * 2; i++)
            (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.GetAsync("/api/v1/ativos")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
