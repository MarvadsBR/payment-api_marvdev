# Diário de Evolução — Payment API

> Registro contínuo de decisões, implementações e estado do projeto.  
> Atualizar a cada sessão de desenvolvimento antes de commitar.

---

## Estado do Repositório

| Item | Estado |
|---|---|
| Repositório Git local | ✅ Inicializado (`main`) |
| Remote GitHub | ✅ Configurado (`MarvadsBR/payment-api_marvdev`) |
| Último commit local | `6b94c82` — `test: add unit tests for PaymentService and PaymentsController` (01/05/2026 às 20:35) |
| Push para GitHub | ✅ **Realizado em 01/05/2026** |

---

## Sessão 1 — 30/04/2026 às 17:21 · Commit `40ddbb3`

### O que foi implementado

**Estrutura base do projeto**
- Projeto ASP.NET Core 8 (`PaymentApi.csproj`) com nullable enable, implicit usings e geração de documentação XML
- Dependências: `Microsoft.EntityFrameworkCore.SqlServer 8.0.0`, `Microsoft.EntityFrameworkCore.Tools 8.0.0`, `Swashbuckle.AspNetCore 6.5.0`

**Models**
- `Payment.cs` — entidade principal com `Id` (Guid), `Amount`, `Currency`, `Status`, `Method`, `Description`, `ExternalReference`, `CreatedAt`, `UpdatedAt`
- `PaymentStatus.cs` — enum: `Pending | Completed | Failed | Refunded`
- `PaymentMethod.cs` — enum: `CreditCard | DebitCard | Pix | BankTransfer`

**DTOs**
- `CreatePaymentDto.cs` — validações com Data Annotations (`[Required]`, `[Range]`, `[StringLength]`)
- `PaymentResponseDto.cs` — contrato de saída da API
- `UpdatePaymentStatusDto.cs` — DTO para o PATCH de status

**Data**
- `AppDbContext.cs` — EF Core DbContext com configuração explícita de precisão decimal (18,2), MaxLength por coluna, enums persistidos como string para legibilidade

**Services**
- `IPaymentService.cs` — interface com 5 operações assíncronas
- `PaymentService.cs` — implementação com LINQ, ordenação por `CreatedAt DESC`, filtro case-insensitive por status, `DeleteResult` enum para retornos precisos

**Controllers**
- `PaymentsController.cs` — 5 endpoints RESTful com `ProducesResponseType` documentados e XML summary comments

**Endpoints implementados**

| Método | Rota | Comportamento especial |
|---|---|---|
| `GET` | `/api/payments` | Filtro opcional `?status=` |
| `GET` | `/api/payments/{id}` | 404 se não encontrado |
| `POST` | `/api/payments` | 201 + Location header |
| `PATCH` | `/api/payments/{id}/status` | 404 se não encontrado |
| `DELETE` | `/api/payments/{id}` | 404 se não existe · 409 se não for Pending |

**Infraestrutura**
- `Dockerfile` — build multi-stage (sdk → publish → aspnet runtime), expõe porta 8080
- `docker-compose.yml` — API + SQL Server 2022 Express com healthcheck e volume persistente
- `Program.cs` — Swagger com XML comments, `EnsureCreated()` no startup, Swagger na raiz `/`
- `appsettings.json` / `appsettings.Development.json` — connection string configurada

**Documentação**
- `README.md` — stack, estrutura, endpoints, instruções Docker e local, exemplos HTTP, decisões de design
- `.gitignore` — padrão .NET (bin, obj, .vs, .idea, publish, NuGet)

### Arquivos incluídos no commit (18 arquivos · 643 linhas)

```
.gitignore
Controllers/PaymentsController.cs
DTOs/CreatePaymentDto.cs
DTOs/PaymentResponseDto.cs
DTOs/UpdatePaymentStatusDto.cs
Data/AppDbContext.cs
Dockerfile
Models/Payment.cs
Models/PaymentMethod.cs
Models/PaymentStatus.cs
PaymentApi.csproj
Program.cs
README.md
Services/IPaymentService.cs
Services/PaymentService.cs
appsettings.Development.json
appsettings.json
docker-compose.yml
```

### Decisões técnicas registradas

- **Camada de serviço**: separa regras de negócio das responsabilidades HTTP
- **DTOs**: evitam over-posting e desacoplam o contrato da API da entidade
- **Enum → string no banco**: mais legível em queries e logs
- **Enum `DeleteResult`**: permite ao controller retornar 404 vs 409 sem lançar exceções
- **`EnsureCreated()`**: mantém o demo sem configuração; em produção, substituir por migrations

---

## Pendências e Próximas Funcionalidades

### 🔴 Antes do próximo push

- [ ] **Segurança — credenciais em texto puro no `docker-compose.yml`**  
  `SA_PASSWORD` e connection string com senha exposta.  
  **Solução:** criar `.env` com as credenciais, referenciar via `${VAR}` no compose e adicionar `.env` ao `.gitignore`. Versionar apenas o `.env.example`.

- [ ] **`UpdatePaymentStatusDto` sem validação de enum**  
  Aceita qualquer string; um valor inválido causa exceção não tratada.  
  **Solução:** adicionar `[EnumDataType(typeof(PaymentStatus))]` ou `[Required]` + validação explícita.

- [ ] **Ausência de tratamento global de erros**  
  Exceções não tratadas expõem stack trace em ambiente Development.  
  **Solução:** adicionar `app.UseExceptionHandler` ou middleware `ProblemDetails`.

### 🟡 Próximas funcionalidades (por prioridade)

- [ ] Testes unitários com **xUnit + Moq** — cobrir `PaymentService` (alto impacto no portfólio)
- [ ] **GitHub Actions** — pipeline de CI com `dotnet build` + `dotnet test`
- [ ] Paginação no `GET /api/payments` — parâmetros `?page=` e `?pageSize=`
- [ ] EF Core Migrations — substituir `EnsureCreated()` por `dotnet ef migrations`
- [ ] Endpoint de health check (`/health`) via `AddHealthChecks()`
- [ ] Respostas de erro padronizadas com `ProblemDetails` (RFC 7807)

---

## Sessão 2 — 01/05/2026 às 20:08 · Commit `4b810ce`

### Problema resolvido: OWASP A02 — Exposição de Dados Sensíveis

As credenciais do SQL Server estavam em texto puro nos arquivos versionados (`docker-compose.yml`, `appsettings.json`, `appsettings.Development.json`), expostas a qualquer pessoa com acesso ao repositório.

### Alterações realizadas

| Arquivo | Tipo | Mudança |
|---|---|---|
| `.env` | **Novo** (ignorado pelo git) | Credenciais reais (`SA_PASSWORD`, `DB_NAME`) |
| `.env.example` | **Novo** (versionado) | Template com valores placeholder para novos devs |
| `docker-compose.yml` | **Modificado** | `Your_password123` → `${SA_PASSWORD}`, `PaymentDb` → `${DB_NAME}` nas 3 ocorrências |
| `appsettings.json` | **Modificado** | `Your_password123` → `CHANGE_ME` |
| `appsettings.Development.json` | **Modificado** | `Your_password123` → `CHANGE_ME` |
| `.gitignore` | **Modificado** | Adicionado `!.env.example` para exceção da regra `.env.*` |

### Como funciona agora

- `docker-compose` lê o arquivo `.env` automaticamente ao executar `docker-compose up`
- `.env` está no `.gitignore` — nunca será versionado
- `.env.example` é o template que novos colaboradores copiam e preenchem
- `appsettings` mantém `CHANGE_ME` explícito para desenvolvimento local sem Docker

### Commits

- [x] `4b810ce` — `fix: remove hardcoded credentials, use .env for secrets` (01/05/2026 às 20:08)
- [x] `0e6ec33` — `docs: update DIARIO with commit hash for session 2` (01/05/2026 às 20:08)

### Push realizado: ✅

## Sessão 3 — 01/05/2026 às 20:12 · Commit `a1f2caf`

### Correções realizadas

**Correção 1 — Validação de enum no `UpdatePaymentStatusDto`**

Antes, enviar `"status": 999` ou `"status": "Invalido"` gerava um `500` por exceção não tratada.

| Arquivo | Mudança |
|---|---|
| `DTOs/UpdatePaymentStatusDto.cs` | Adicionado `[EnumDataType(typeof(PaymentStatus))]` com mensagem de erro explícita |
| `Program.cs` | Registrado `JsonStringEnumConverter` globalmente — o deserializador rejeita inteiros e strings inválidas com `400` antes de chegar ao handler |

**Correção 2 — Tratamento global de erros (RFC 7807 ProblemDetails)**

Antes, exceções não tratadas retornavam stack trace em Development e resposta vazia em Production.

| Arquivo | Mudança |
|---|---|
| `Program.cs` | `AddProblemDetails()` registrado no contêiner de serviços |
| `Program.cs` | `app.UseExceptionHandler()` — retorna `application/problem+json` em exceções não tratadas |
| `Program.cs` | `app.UseStatusCodePages()` — formata respostas 4xx/5xx sem corpo |

**Efeito combinado das duas correções**

| Cenário | Antes | Depois |
|---|---|---|
| `PATCH` com `"status": "Invalido"` | `500` (exceção) | `400` com ProblemDetails |
| `PATCH` com `"status": 999` | `500` (exceção) | `400` com ProblemDetails |
| Exceção não tratada em qualquer endpoint | `500` com stack trace | `500` com ProblemDetails sem detalhes internos |
| Rota inexistente | `404` sem corpo | `404` com ProblemDetails |

### Build: ✅ 0 erros · 0 avisos

### Commits

- [x] `a1f2caf` — `fix: add enum validation and global error handling` (01/05/2026 às 20:12)
- [x] `f0b1168` — `docs: update DIARIO - session 3 fixes registered` (01/05/2026 às 20:13)

### Push realizado: ✅

---

## Sessão 4 — 01/05/2026 às 20:35 · Commit `6b94c82`

### O que foi implementado: Testes Unitários

Criação do projeto `PaymentApi.Tests` com cobertura completa de `PaymentService` e `PaymentsController`.

**Ferramentas utilizadas**

| Ferramenta | Finalidade |
|---|---|
| xUnit | Framework de testes |
| Moq 4.20.70 | Criação de objetos dublê (mocks) para o `IPaymentService` |
| EF Core InMemory 8.0.0 | Banco em memória para testar queries e persistência reais |

**Arquivos criados**

| Arquivo | Descrição |
|---|---|
| `PaymentApi.sln` | Arquivo de solução agrupando os dois projetos |
| `PaymentApi.Tests/PaymentApi.Tests.csproj` | Projeto de testes com referência ao projeto principal |
| `PaymentApi.Tests/Helpers/DbContextFactory.cs` | Fábrica de `AppDbContext` isolado por teste (banco InMemory com nome único) |
| `PaymentApi.Tests/Services/PaymentServiceTests.cs` | 14 testes do `PaymentService` com EF InMemory |
| `PaymentApi.Tests/Controllers/PaymentsControllerTests.cs` | 12 testes do `PaymentsController` com Moq |

**Cobertura dos testes**

*PaymentServiceTests — 14 testes (xUnit + EF InMemory)*

| Método | Cenário testado |
|---|---|
| `GetAllAsync` | Sem filtro, filtro válido, filtro inválido, filtro sem distinção de maiúsculas |
| `GetByIdAsync` | Id existente, id inexistente |
| `CreateAsync` | DTO válido, moeda normalizada para maiúsculas, persistência no banco |
| `UpdateStatusAsync` | Id existente, id inexistente |
| `DeleteAsync` | Pagamento Pending (sucesso), id não encontrado, status não-Pending × 3 via `[Theory]` |

*PaymentsControllerTests — 12 testes (xUnit + Moq)*

| Endpoint | Cenário testado |
|---|---|
| `GET /api/payments` | Retorna 200 com lista, repassa filtro ao service |
| `GET /api/payments/{id}` | Retorna 200 com DTO, retorna 404 |
| `POST /api/payments` | Retorna 201 CreatedAtAction |
| `PATCH /api/payments/{id}/status` | Retorna 200 atualizado, retorna 404 |
| `DELETE /api/payments/{id}` | Retorna 204, retorna 404, retorna 409 |

**Técnicas didáticas aplicadas**

- `[Theory] + [InlineData]` — mesmo teste executado com Completed, Failed e Refunded sem duplicar código
- `mock.Verify()` — garante que o controller repassa o filtro ao service
- Banco isolado por teste — evita vazamento de estado entre testes
- Comentários explicativos em todos os arquivos de teste

**Alteração no projeto principal**

- `PaymentApi.csproj`: adicionado bloco `<ItemGroup>` para excluir `PaymentApi.Tests/**` do glob do projeto principal (evitava conflito de compilação)

### Resultado: ✅ 26/26 testes aprovados · 0 erros · 0 avisos

### Commits

- [x] `6b94c82` — `test: add unit tests for PaymentService and PaymentsController` (01/05/2026 às 20:35)

### Push realizado: ✅

---

## Pendências e Próximas Funcionalidades (atualizado em 01/05/2026 às 20:35)

### ✅ Resolvido

- [x] ~~Credenciais em texto puro no `docker-compose.yml`~~ — resolvido em 01/05/2026 às 20:08
- [x] ~~`UpdatePaymentStatusDto` sem validação de enum~~ — resolvido em 01/05/2026 às 20:12
- [x] ~~Ausência de tratamento global de erros~~ — resolvido em 01/05/2026 às 20:12
- [x] ~~Testes unitários~~ — resolvido em 01/05/2026 às 20:35
- [x] ~~Push para o GitHub~~ — realizado em 01/05/2026

### 🟡 Próximas funcionalidades (por prioridade)

- [ ] **GitHub Actions** — pipeline de CI com `dotnet build` + `dotnet test` em cada push/PR
- [ ] Paginação no `GET /api/payments` — parâmetros `?page=` e `?pageSize=`
- [ ] EF Core Migrations — substituir `EnsureCreated()` por `dotnet ef migrations`
- [ ] Endpoint de health check (`/health`) via `AddHealthChecks()`

---

*Última atualização: 01/05/2026 às 20:35*
