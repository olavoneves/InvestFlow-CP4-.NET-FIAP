# InvestFlow — CP4 .NET (FIAP)

API REST de investimentos: cadastro de ativos e registro de ordens de compra e venda.

## Domínio

O InvestFlow modela duas entidades com relacionamento **1:N**: um **Ativo** possui várias **Ordens**.

**Ativo** — instrumento negociável.

| Campo | Regra |
|---|---|
| `Ticker` | Obrigatório, até 10 caracteres, gravado sem espaços nas pontas e em maiúsculas. Único e imutável (é a identidade de mercado). |
| `Nome` | Obrigatório, até 100 caracteres. |
| `Tipo` | `Acao`, `FII`, `RendaFixa` ou `Derivativo`. |
| `PrecoAtual` | Maior que zero, até 4 casas decimais. |
| `CriadoEm` | Preenchido na criação (UTC). |

Um ativo que já possui ordens não pode ser excluído, para preservar o histórico.

**Ordem** — compra ou venda de um ativo.

| Campo | Regra |
|---|---|
| `AtivoId` | Deve referenciar um ativo existente. |
| `Lado` | `Compra` ou `Venda`. |
| `Quantidade` | Maior que zero. |
| `PrecoExecucao` | Maior que zero, até 4 casas decimais. |
| `DataExecucao` | Informada pelo cliente; se omitida, usa o momento do registro (UTC). |
| `Status` | Toda ordem nasce `Pendente`. |
| `ValorFinanceiro` | `Quantidade × PrecoExecucao`, calculado pela entidade (não é persistido). |

Ciclo de vida da ordem:

```mermaid
stateDiagram-v2
    [*] --> Pendente: POST /ordens
    Pendente --> Executada: POST /ordens/{id}/executar
    Pendente --> Cancelada: POST /ordens/{id}/cancelar
    Executada --> [*]
    Cancelada --> [*]
```

Qualquer outra transição (executar uma ordem cancelada, cancelar uma executada, cancelar duas vezes) é
rejeitada pela própria entidade com `DomainException`, que a API devolve como **409**.

O banco é criado com um seed determinístico de **5 ativos** (um de cada tipo, mais uma segunda ação) e
**60 ordens** nos três status, volume suficiente para demonstrar a paginação.

## Arquitetura

Arquitetura em camadas, com as dependências apontando sempre para dentro, em direção ao domínio.

```mermaid
flowchart TB
    Tests["tests/<br/>UnitTests · IntegrationTests"]

    subgraph API["InvestFlow.Api"]
        direction LR
        C["Controllers"]
        M["Middlewares<br/>(ProblemDetails)"]
        P["Program.cs<br/>(composição, DI)"]
    end

    subgraph APP["InvestFlow.Application"]
        direction LR
        S["Services"]
        D["DTOs · PagedResult"]
        MP["AutoMapper Profiles"]
    end

    subgraph INFRA["InvestFlow.Infrastructure"]
        direction LR
        R["Repositories"]
        DB["AppDbContext<br/>Configurations · Migrations"]
    end

    subgraph DOM["InvestFlow.Domain — zero dependências externas"]
        direction LR
        E["Entidades · Enums"]
        IR["Interfaces de repositório"]
        EX["DomainException"]
    end

    API -->|usa services e DTOs| APP
    API -.->|somente registro de DI| INFRA
    APP -->|usa entidades e interfaces| DOM
    INFRA -->|implementa interfaces| DOM
    INFRA -.->|referência permitida| APP
    Tests --> API
    Tests --> APP
```

**Regra de dependência**

| Projeto | Pode referenciar | Não pode |
|---|---|---|
| `InvestFlow.Domain` | nenhum projeto, nenhum pacote NuGet | qualquer coisa |
| `InvestFlow.Application` | Domain | Infrastructure, Api, EF Core |
| `InvestFlow.Infrastructure` | Domain (+ Application, se precisar de contratos) | Api |
| `InvestFlow.Api` | Application; Infrastructure apenas para registrar DI | acessar `DbContext` nos controllers |

Fluxo de uma requisição: **Controller → Service → Repository → DbContext**. O controller conhece apenas
interfaces de service e DTOs; o service conhece apenas interfaces de repositório declaradas no Domain; só
a Infrastructure conhece o EF Core.

```
src/InvestFlow.Domain             -> entidades, enums, filtros, interfaces de repositório, DomainException
src/InvestFlow.Application        -> DTOs, services, AutoMapper profiles, PagedResult<T>, validação
src/InvestFlow.Infrastructure     -> AppDbContext, IEntityTypeConfiguration, repositories, migrations, seed
src/InvestFlow.Api                -> controllers, middleware de exceções, Program.cs, configuração
tests/InvestFlow.UnitTests        -> entidades, services (Moq), mapeamentos, validação, PagedResult
tests/InvestFlow.IntegrationTests -> WebApplicationFactory (HTTP completo) e repositórios sobre SQLite
```

## Decisões técnicas

| Decisão | Por quê |
|---|---|
| **Entidades ricas** (setters privados, construtor e métodos que validam) | A regra de negócio fica num único lugar. Não existe `Ordem` com quantidade zero nem transição de status inválida, venha a chamada de onde vier. |
| **Interfaces de repositório no Domain** | A Application depende de abstrações, não do EF Core. Os services são testados com Moq, sem banco. |
| **DTOs separados das entidades** (`*CreateRequest`, `*UpdateRequest`, `*Response`, `*Query`) | O contrato HTTP não vaza o modelo de persistência, e o `Ticker` pode ficar fora do `AtivoUpdateRequest` por ser imutável. |
| **AutoMapper só no sentido entidade → response** | A criação e a alteração passam pelo construtor e pelos métodos da entidade, que validam; mapear request → entidade contornaria essas regras. `NomeAtivo` e `ValorFinanceiro` são mapeados explicitamente e cobertos por `AssertConfigurationIsValid`. |
| **Validação no service, além do model binding** | `RequestValidator` executa as DataAnnotations dentro do service, então a regra vale mesmo fora do pipeline MVC (testes, outros consumidores). Os dois caminhos geram o mesmo 400. |
| **Paginação obrigatória com `PagedResult<T>`** | Nenhuma listagem devolve a tabela inteira. `PageSize` máximo de 50 limita o custo por requisição; `totalCount`, `totalPages`, `hasNext` e `hasPrevious` permitem navegar sem cálculo no cliente. |
| **Ordenação estável** (`DataExecucao DESC, Id DESC`; `Ticker, Id`) | Sem desempate, `Skip/Take` pode repetir ou pular registros entre páginas. |
| **Índices declarados explicitamente** | `IX_Ativos_Ticker` (único) garante a unicidade mesmo sob concorrência e acelera a busca por ticker. `IX_Ordens_AtivoId` atende o filtro por ativo, o JOIN e a checagem antes de excluir um ativo — o SQL Server não indexa FK sozinho. `IX_Ordens_DataExecucao_Status` serve a ordenação da listagem e o filtro por período e status. |
| **`decimal(18,4)` nos campos monetários** | Cotações e preços de execução têm até 4 casas. Os limites de `Range` nos DTOs acompanham a precisão da coluna. |
| **FK `Restrict`** | Apagar um ativo não pode apagar o histórico de ordens em cascata. O service verifica antes e responde 409 com mensagem clara. |
| **`AsNoTracking` nas leituras, `GetByIdForUpdateAsync` nas escritas** | Leitura sem custo de change tracking; a escrita trabalha sobre a entidade rastreada. |
| **Um `DbContext` derivado por provider** (`SqliteAppDbContext`, `SqlServerAppDbContext`) | Os tipos de coluna gerados diferem entre providers; cada um tem seu próprio conjunto de migrations e o provider é escolhido por configuração. |
| **SQLite como padrão** | Roda sem instalar nada: o avaliador clona, executa `dotnet run` e já tem banco com dados. |
| **Middleware global de exceções → ProblemDetails** | Controllers sem `try/catch`. `ValidationException` → 400, `NotFoundException` → 404, `DomainException` → 409, qualquer outra → 500 genérico, sem stack trace. O 400 do model binding e o 429 usam o mesmo `ApiProblemDetails`, então todo erro tem o mesmo formato e carrega `traceId`. |
| **Ativo inexistente ao criar ordem → 400, não 404** | O id vem no corpo: é dado inválido da requisição, não recurso da URL ausente. |
| **Rate limiting nativo** (`Microsoft.AspNetCore.RateLimiting`) | Sem pacote extra. Janela fixa por IP, limites configuráveis e validados na subida (`ValidateOnStart`). |
| **Brotli + Gzip com nível `Fastest`** | Listagens JSON comprimem muito; `Fastest` reduz o tamanho sem gastar CPU demais por requisição. Habilitado também para HTTPS e para `application/problem+json`. |
| **Serilog** (console + arquivo JSON compacto) | Log estruturado: cada propriedade (`Method`, `Path`, `RequestId`, `Errors`) é consultável, não só texto. `RequestId` entra no `LogContext` para correlacionar todos os logs de uma requisição. |
| **Application Insights opcional** | Sem connection string a API sobe normalmente com um `TelemetryClient` desabilitado, então o código que emite a métrica `ordens.criadas` não precisa de `if`. Nenhum segredo é versionado. |
| **Health checks em duas rotas** | `/health` verifica o banco e responde JSON detalhado; `/health/live` não executa checks e serve de liveness barato. Ambos ficam fora do rate limiting. |
| **Swagger com Annotations e XML comments** | `[SwaggerOperation]`, `[SwaggerResponse]` e `[ProducesResponseType]` documentam todos os status; os `<example>` dos DTOs preenchem os exemplos da UI. |
| **Testes de integração com `WebApplicationFactory` + SQLite em memória** | Exercitam o `Program.cs` real (middlewares, rate limiting, compressão, Swagger, migrations e seed), isolados por instância e sem dependência externa. |

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
