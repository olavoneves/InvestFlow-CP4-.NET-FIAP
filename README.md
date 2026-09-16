# InvestFlow — CP4 .NET (FIAP)

API REST de investimentos: cadastro de ativos e registro de ordens de compra e venda.

## Configuração

### Application Insights

A telemetria só é habilitada quando existe uma connection string. Sem ela, a API sobe normalmente
e a telemetria (inclusive a métrica customizada `ordens.criadas`) é descartada.

**Nunca coloque a connection string real no `appsettings.json`** — o valor versionado é um placeholder vazio.
Configure por variável de ambiente:

```powershell
# PowerShell
$env:APPLICATIONINSIGHTS_CONNECTION_STRING = "InstrumentationKey=...;IngestionEndpoint=https://..."
dotnet run --project src/InvestFlow.Api
```

```bash
# Bash
APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=...;IngestionEndpoint=https://..." dotnet run --project src/InvestFlow.Api
```

A chave `ApplicationInsights__ConnectionString` (equivalente a `ApplicationInsights:ConnectionString`)
também é aceita e tem prioridade sobre `APPLICATIONINSIGHTS_CONNECTION_STRING`.

### Rate limiting

Janela fixa por IP do cliente, padrão de 5 requisições a cada 10 segundos. Ajustável pela seção
`RateLimiting` (`PermitLimit`, `WindowSeconds`) ou pelas variáveis `RateLimiting__PermitLimit` e
`RateLimiting__WindowSeconds`.
