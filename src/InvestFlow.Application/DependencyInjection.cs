using InvestFlow.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InvestFlow.Application;

public static class DependencyInjection
{
    /// <summary>Registra os profiles do AutoMapper e os services da camada Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAtivoService, AtivoService>();
        services.AddScoped<IOrdemService, OrdemService>();

        return services;
    }
}
