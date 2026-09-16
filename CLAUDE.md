# InvestFlow — CP4 .NET (FIAP)

API REST de investimentos. Entidades: Ativo (1) -> Ordem (N).

## Stack
.NET 8, Controllers, EF Core 8 (SQLite default), xUnit, Moq, FluentAssertions 6.12.0,
AutoMapper 12.0.1, Swashbuckle 6.6.x + Annotations, Serilog.AspNetCore,
Microsoft.ApplicationInsights.AspNetCore, HealthChecks EF Core.

## Estrutura
```
src/InvestFlow.Domain             -> entidades, interfaces de repositório, exceptions. ZERO dependências externas.
src/InvestFlow.Application        -> DTOs, interfaces de service, services, AutoMapper profiles, PagedResult<T>
src/InvestFlow.Infrastructure     -> DbContext, EntityTypeConfigurations, repositories, migrations
src/InvestFlow.Api                -> Controllers, Program.cs, middlewares
tests/InvestFlow.UnitTests        -> regras de negócio, services, mapeamentos
tests/InvestFlow.IntegrationTests -> WebApplicationFactory, ciclo HTTP completo
```

Direção das referências entre projetos:
- Domain -> nenhum projeto
- Application -> Domain
- Infrastructure -> Domain (+ Application, se precisar de contratos dela)
- Api -> Application, Infrastructure (Infrastructure só para registro de DI)

## Regras
- Controller NUNCA acessa DbContext. Controller -> Service -> Repository.
- Toda listagem é paginada: PageNumber (default 1), PageSize (default 10, máx 50).
- Todo endpoint tem [SwaggerOperation] + [ProducesResponseType] para todos os status retornados.
- Exceções de domínio tratadas em middleware global, resposta em ProblemDetails.
- Nada de Minimal API. Nada de código gerado sem teste correspondente.
- Após qualquer alteração: `dotnet build` e `dotnet test` devem passar. Se quebrar, conserte antes de finalizar.

## Workflow de Git — obrigatório
O repositório é PÚBLICO e o histórico é avaliado. Nunca faça commit direto na main.

- Sempre parta de: `git checkout main && git pull`
- Crie uma branch por tarefa, nomeada por escopo:
  feat/scaffold, feat/domain-infra, feat/application, feat/api-observabilidade,
  test/suites, docs/readme
- Conventional Commits, em português, no imperativo:
  feat: | fix: | test: | docs: | refactor: | chore:
  Exemplo: "feat: adiciona paginação e índices nas consultas de ordem"
- Commits pequenos e temáticos. NUNCA um commit único gigante no fim.
- Antes de qualquer push: `dotnet build && dotnet test` verdes.
- Ao concluir uma tarefa: commit + push da branch e me informe o comando de PR.
  NÃO faça merge na main sozinho — o merge é feito via Pull Request revisado.
- .gitignore deve cobrir bin/, obj/, *.user, appsettings.Development.json com segredos,
  e o arquivo .db do SQLite.
- NUNCA commite connection string real do Application Insights. Use placeholder
  no appsettings.json e documente a variável de ambiente no README.

Comando de PR (informar ao final da tarefa):
```
gh pr create --base main --head <branch> --title "<tipo>: <resumo>" --body "<descrição>"
```

## Comandos
```
dotnet build
dotnet test
dotnet run --project src/InvestFlow.Api
```

## Ambiente
- Máquina de desenvolvimento Windows. No Windows PowerShell 5.1 o operador `&&` não existe:
  use `A; if ($?) { B }` ou rode a sequência pelo Git Bash.
- Remote: https://github.com/olavoneves/InvestFlow-CP4-.NET-FIAP.git
