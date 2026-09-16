using FluentAssertions;
using InvestFlow.Domain.Repositories;
using InvestFlow.Infrastructure;
using InvestFlow.Infrastructure.Persistence;
using InvestFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestFlow.IntegrationTests.Infrastructure;

public class DependencyInjectionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sqlite")]
    [InlineData("sqlite")]
    public void AddInfrastructure_ProviderSqliteOuAusente_RegistraContextoSqlite(string? provider)
    {
        using var serviceProvider = Construir(new() { ["Database:Provider"] = provider });
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Should().BeOfType<SqliteAppDbContext>();
        context.Database.IsSqlite().Should().BeTrue();
    }

    [Fact]
    public void AddInfrastructure_ProviderSqlServer_RegistraContextoSqlServer()
    {
        using var serviceProvider = Construir(new()
        {
            ["Database:Provider"] = "SqlServer",
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=InvestFlowTeste;Trusted_Connection=True",
        });
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Should().BeOfType<SqlServerAppDbContext>();
        context.Database.IsSqlServer().Should().BeTrue();
    }

    [Fact]
    public void AddInfrastructure_RegistraRepositoriosComoScoped()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(Configuracao(new()));

        services.Should().Contain(d => d.ServiceType == typeof(IAtivoRepository)
            && d.ImplementationType == typeof(AtivoRepository) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IOrdemRepository)
            && d.ImplementationType == typeof(OrdemRepository) && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_SqlServerSemConnectionString_LancaInvalidOperation()
    {
        var acao = () => new ServiceCollection().AddInfrastructure(Configuracao(new() { ["Database:Provider"] = "SqlServer" }));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*DefaultConnection*");
    }

    [Theory]
    [InlineData("Oracle")]
    [InlineData("1")]
    public void AddInfrastructure_ProviderInvalido_LancaInvalidOperation(string provider)
    {
        var acao = () => new ServiceCollection().AddInfrastructure(Configuracao(new() { ["Database:Provider"] = provider }));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*Database:Provider*");
    }

    private static ServiceProvider Construir(Dictionary<string, string?> valores)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(Configuracao(valores));
        return services.BuildServiceProvider();
    }

    private static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
}
