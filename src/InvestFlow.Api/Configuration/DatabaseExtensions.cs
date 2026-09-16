using InvestFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestFlow.Api.Configuration;

public static class DatabaseExtensions
{
    /// <summary>Aplica as migrations pendentes na subida, apenas em Development.</summary>
    public static WebApplication AplicarMigrationsEmDesenvolvimento(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.Migrate();

        app.Logger.LogInformation("Migrations aplicadas no banco de desenvolvimento.");
        return app;
    }
}
