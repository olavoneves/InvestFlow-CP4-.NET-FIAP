using InvestFlow.Api.Configuration;
using InvestFlow.Api.Middlewares;
using InvestFlow.Application;
using InvestFlow.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog: log estruturado em console + arquivo, enriquecido com RequestId.
builder.AddSerilogLogging();

// Camadas da aplicação.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers + 400 do model binding no mesmo formato do 400 da ValidationException.
builder.Services.AddApiControllers();

// 2. Response Compression: Brotli + Gzip.
builder.Services.AddApiResponseCompression();

// 3. Rate Limiting nativo: janela fixa por IP, global (30 req/10s) + política nomeada "estrito" (5 req/10s em GET /api/v1/ativos).
builder.Services.AddApiRateLimiting(builder.Configuration);

// 4. Health Checks, incluindo o banco via DbContext.
builder.Services.AddApiHealthChecks();

// 5. Application Insights apenas se houver connection string; TelemetryClient sempre disponível.
var telemetriaHabilitada = builder.Services.AddTelemetria(builder.Configuration);

// 6. Swagger com XML comments, annotations e exemplos.
builder.Services.AddApiSwagger();

var app = builder.Build();

app.Logger.LogInformation(telemetriaHabilitada
    ? "Application Insights habilitado."
    : "Application Insights desabilitado: connection string não configurada.");

// 8. Migrations aplicadas na subida em Development.
app.AplicarMigrationsEmDesenvolvimento();

app.UseRequestIdLogContext();
app.UseSerilogRequestLogging();

// 7. Middleware global de exceção -> ProblemDetails.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseResponseCompression();
app.UseHttpsRedirection();

// UseRouting explícito antes do rate limiter, para que a política do endpoint ("estrito") seja conhecida.
app.UseRouting();
app.UseRateLimiter();

app.UseApiSwagger();

app.MapControllers();
app.MapApiHealthChecks();

app.Run();

// Exposto para o WebApplicationFactory dos testes de integração.
public partial class Program
{
}
