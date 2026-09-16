using AutoMapper;
using FluentAssertions;
using InvestFlow.Application;
using InvestFlow.Application.Services;
using InvestFlow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace InvestFlow.UnitTests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistraServicesEMapperResolviveis()
    {
        var services = new ServiceCollection()
            .AddScoped(_ => Mock.Of<IAtivoRepository>())
            .AddScoped(_ => Mock.Of<IOrdemRepository>())
            .AddApplication();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAtivoService>().Should().BeOfType<AtivoService>();
        scope.ServiceProvider.GetRequiredService<IOrdemService>().Should().BeOfType<OrdemService>();
        scope.ServiceProvider.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void AddApplication_RegistraServicesComoScoped()
    {
        var services = new ServiceCollection().AddApplication();

        services.Should().Contain(d => d.ServiceType == typeof(IAtivoService) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IOrdemService) && d.Lifetime == ServiceLifetime.Scoped);
    }
}
