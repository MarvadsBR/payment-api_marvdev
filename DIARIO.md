# Diário de Evolução — Payment API

> Registro contínuo de decisões, implementações e estado do projeto.  
> Atualizar a cada sessão de desenvolvimento antes de commitar.

---

## Estado do Repositório

| Item | Estado |
|---|---|
| Repositório Git local | ✅ Inicializado (`main`) |
| Remote GitHub | ✅ Configurado (`MarvadsBR/payment-api`) |
| Último commit local | `40ddbb3` — `feat: initial Payment API` (30/04/2026) |
| Push para GitHub | ❌ **Ainda não realizado** |

---

## Sessão 1 — 30/04/2026 · Commit `40ddbb3`

### O que foi implementado

**Estrutura base do projeto**
- Projeto ASP.NET Core 8 (`PaymentApi.csproj`) com nullable enable, implicit usings e geração de XML doc
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

### Arquivos no commit (18 arquivos · 643 linhas)

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

- **Service layer**: separa regras de negócio das preocupações HTTP
- **DTOs**: evitam over-posting e desacoplam o contrato da entidade
- **Enum → string no banco**: mais legível em queries e logs
- **`DeleteResult` enum**: permite ao controller retornar 404 vs 409 sem lançar exceções
- **`EnsureCreated()`**: mantém o demo zero-config; em produção, substituir por migrations

---

## Pendências e Próximos Passos

### 🔴 Antes do próximo push

- [ ] **Segurança — credenciais em plaintext no `docker-compose.yml`**  
  `SA_PASSWORD` e connection string com senha exposta.  
  **Solução:** criar `.env` com as credenciais, referenciar via `${VAR}` no compose e adicionar `.env` ao `.gitignore`. Commitar apenas `.env.example`.

- [ ] **`UpdatePaymentStatusDto` sem validação de enum**  
  Aceita qualquer string; uma entrada inválida causa exceção não tratada.  
  **Solução:** adicionar `[EnumDataType(typeof(PaymentStatus))]` ou `[Required]` + validação explícita.

- [ ] **Ausência de tratamento global de erros**  
  Exceções não tratadas expõem stack trace em ambiente Development.  
  **Solução:** adicionar `app.UseExceptionHandler` ou middleware `ProblemDetails`.

### 🟡 Próximas features (por prioridade)

- [ ] Testes unitários com **xUnit + Moq** — cobrir `PaymentService` (alto impacto no portfólio)
- [ ] **GitHub Actions** — pipeline CI com `dotnet build` + `dotnet test`
- [ ] Paginação no `GET /api/payments` — parâmetros `?page=` e `?pageSize=`
- [ ] EF Core Migrations — substituir `EnsureCreated()` por `dotnet ef migrations`
- [ ] Health check endpoint (`/health`) via `AddHealthChecks()`
- [ ] Respostas de erro padronizadas com `ProblemDetails` (RFC 7807)

---

## Sessão 2 — 01/05/2026 · Pendente de commit

### Problema resolvido: OWASP A02 — Sensitive Data Exposure

Credenciais do SQL Server estavam em plaintext nos arquivos versionados (`docker-compose.yml`, `appsettings.json`, `appsettings.Development.json`), expostas a qualquer pessoa com acesso ao repositório.

### Alterações realizadas

| Arquivo | Tipo | Mudança |
|---|---|---|
| `.env` | **Novo** (gitignored) | Credenciais reais (`SA_PASSWORD`, `DB_NAME`) |
| `.env.example` | **Novo** (commitado) | Template com valores placeholder para novos devs |
| `docker-compose.yml` | **Modificado** | `Your_password123` → `${SA_PASSWORD}`, `PaymentDb` → `${DB_NAME}` nas 3 ocorrências |
| `appsettings.json` | **Modificado** | `Your_password123` → `CHANGE_ME` |
| `appsettings.Development.json` | **Modificado** | `Your_password123` → `CHANGE_ME` |

### Como funciona agora

- `docker-compose` lê `.env` automaticamente ao executar `docker-compose up`
- `.env` está no `.gitignore` (já estava) — nunca será commitado
- `.env.example` é o template que novos colaboradores copiam e preenchem
- `appsettings` mantém `CHANGE_ME` explícito para desenvolvimento local sem Docker → usar `dotnet user-secrets`

### Commit(s)

- [x] `4b810ce` — `fix: remove hardcoded credentials, use .env for secrets` (01/05/2026)

### Push realizado: ❌

## Sessão 3 — 01/05/2026 · Commit `a1f2caf`

### Pendências resolvidas

**Fix 1 — Validação de enum no `UpdatePaymentStatusDto`**

Antes, enviar `"status": 999` ou `"status": "Invalido"` gerava um `500` por exceção não tratada.

| Arquivo | Mudança |
|---|---|
| `DTOs/UpdatePaymentStatusDto.cs` | Adicionado `[EnumDataType(typeof(PaymentStatus))]` com mensagem de erro explícita |
| `Program.cs` | Registrado `JsonStringEnumConverter` globalmente — deserializador rejeita inteiros e strings inválidas com `400` antes de chegar ao handler |

**Fix 2 — Tratamento global de erros (RFC 7807 ProblemDetails)**

Antes, exceções não tratadas retornavam stack trace em Development e resposta vazia em Production.

| Arquivo | Mudança |
|---|---|
| `Program.cs` | `AddProblemDetails()` no container de serviços |
| `Program.cs` | `app.UseExceptionHandler()` — retorna `application/problem+json` em exceções não tratadas |
| `Program.cs` | `app.UseStatusCodePages()` — formata respostas 4xx/5xx sem corpo |

**Efeito combinado dos dois fixes**

| Cenário | Antes | Depois |
|---|---|---|
| `PATCH` com `"status": "Invalido"` | `500` (exceção) | `400` com ProblemDetails |
| `PATCH` com `"status": 999` | `500` (exceção) | `400` com ProblemDetails |
| Exceção não tratada em qualquer endpoint | `500` com stack trace | `500` com ProblemDetails sem detalhes internos |
| Rota inexistente | `404` sem corpo | `404` com ProblemDetails |

### Build: ✅ 0 erros · 0 warnings

### Commit(s)

- [x] `a1f2caf` — `fix: add enum validation and global error handling` (01/05/2026)

### Push realizado: ❌

---

### 🔴 Antes do próximo push

- [x] ~~Credenciais em plaintext no `docker-compose.yml`~~ — resolvido em 01/05/2026
- [x] ~~`UpdatePaymentStatusDto` sem validação de enum~~ — resolvido em 01/05/2026
- [x] ~~Ausência de tratamento global de erros~~ — resolvido em 01/05/2026
- [ ] **Fazer `git push` para o GitHub** — 3 commits locais aguardando

### 🟡 Próximas features (por prioridade)

- [ ] Testes unitários com **xUnit + Moq** — cobrir `PaymentService` (alto impacto no portfólio)
- [ ] **GitHub Actions** — pipeline CI com `dotnet build` + `dotnet test`
- [ ] Paginação no `GET /api/payments` — parâmetros `?page=` e `?pageSize=`
- [ ] EF Core Migrations — substituir `EnsureCreated()` por `dotnet ef migrations`
- [ ] Health check endpoint (`/health`) via `AddHealthChecks()`

---

*Última atualização: 01/05/2026*
