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

Janela fixa por IP do cliente, com duas políticas:

| Política | Onde se aplica | Padrão |
|---|---|---|
| Global | Todas as rotas, exceto `/swagger/*`, `/health` e `/health/live` | 30 requisições / 10 s |
| `estrito` | Somente `GET /api/v1/ativos` (além da global) | 5 requisições / 10 s |

Acima do limite a resposta é 429 em ProblemDetails, com o header `Retry-After`. Os health checks ficam fora
porque são sondados em alta frequência por orquestradores e monitores; as chamadas de API feitas pelo
Swagger UI continuam limitadas, pois a isenção vale apenas para os arquivos do próprio Swagger.

Ajustável pelas seções `RateLimiting:Global` e `RateLimiting:Estrito` (`PermitLimit`, `WindowSeconds`) ou
por variáveis de ambiente como `RateLimiting__Estrito__PermitLimit`.
