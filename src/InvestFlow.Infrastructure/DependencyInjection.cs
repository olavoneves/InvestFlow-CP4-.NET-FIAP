using InvestFlow.Domain.Repositories;
using InvestFlow.Infrastructure.Persistence;
using InvestFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestFlow.Infrastructure;

public static class DependencyInjection
{
    public const string ProviderConfigKey = "Database:Provider";
    public const string ConnectionStringName = "DefaultConnection";
    public const string DefaultSqliteConnectionString = "Data Source=investflow.db";

    /// <summary>
    /// Registra o <see cref="AppDbContext"/> com o provider definido em "Database:Provider"
    /// ("Sqlite" | "SqlServer", default Sqlite) e os repositórios.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = ObterProvider(configuration);
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        // Os repositórios dependem de AppDbContext; a implementação registrada é o contexto do
        // provider escolhido, para que Database.Migrate() aplique as migrations corretas.
        switch (provider)
        {
            case DatabaseProvider.SqlServer:
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        $"A connection string '{ConnectionStringName}' é obrigatória para o provider SqlServer.");

                services.AddDbContext<AppDbContext, SqlServerAppDbContext>(options =>
                    options.UseSqlServer(connectionString));
                break;

            default:
                services.AddDbContext<AppDbContext, SqliteAppDbContext>(options =>
                    options.UseSqlite(string.IsNullOrWhiteSpace(connectionString)
                        ? DefaultSqliteConnectionString
                        : connectionString));
                break;
        }

        services.AddScoped<IAtivoRepository, AtivoRepository>();
        services.AddScoped<IOrdemRepository, OrdemRepository>();

        return services;
    }

    public static DatabaseProvider ObterProvider(IConfiguration configuration)
    {
        var valor = configuration[ProviderConfigKey];

        if (string.IsNullOrWhiteSpace(valor))
            return DatabaseProvider.Sqlite;

        // Compara só com os nomes: Enum.TryParse aceitaria valores numéricos como "1".
        var nome = Enum.GetNames<DatabaseProvider>()
            .FirstOrDefault(n => string.Equals(n, valor.Trim(), StringComparison.OrdinalIgnoreCase));

        if (nome is not null)
            return Enum.Parse<DatabaseProvider>(nome);

        throw new InvalidOperationException(
            $"Valor inválido em '{ProviderConfigKey}': '{valor}'. Use 'Sqlite' ou 'SqlServer'.");
    }
}
