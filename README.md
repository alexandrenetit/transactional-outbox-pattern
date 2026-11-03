# SalesSample — API de Vendas com Clean Architecture, DDD, CQRS e Transactional Outbox

API backend em **.NET 10** que implementa uma **arquitetura em camadas (Clean Architecture)**, **DDD**, **CQRS com MediatR** e o padrão **Transactional Outbox** para publicar eventos no **Kafka** de forma confiável — com **consumidor idempotente (Inbox)** e **observabilidade completa** (traces, métricas e logs) via **OpenTelemetry**.

> Projeto de estudo/portfólio construído do zero, simulando um sistema de vendas (o mesmo domínio de plataformas de dados para crédito).

---

## 📌 Visão geral

O sistema recebe vendas via API REST, persiste no **PostgreSQL** e publica eventos de domínio no **Kafka** sem perder mensagens — mesmo que o broker esteja fora do ar no momento da escrita. A garantia vem do padrão **Transactional Outbox**: o evento é gravado na **mesma transação** da venda e um worker o entrega ao Kafka depois.

O consumidor é **idempotente** (padrão **Inbox**): mensagens duplicadas — inevitáveis em entrega *at-least-once* — são detectadas e ignoradas.

---

## 🧱 Stack

| Camada | Tecnologia |
|---|---|
| Linguagem / Runtime | C# 13 · .NET 10 |
| Web | ASP.NET Core (Controllers) · Swagger (Swashbuckle) |
| Aplicação | MediatR (CQRS) · FluentValidation (pipeline) |
| Persistência | EF Core 10 · PostgreSQL 17 |
| Mensageria | Apache Kafka 4.0 (KRaft) · Confluent.Kafka |
| Observabilidade | OpenTelemetry → Grafana Tempo (traces) + Prometheus (métricas) + Grafana (UI) |
| Infra local | Docker Compose (Postgres, Kafka, Kafka UI, Tempo, Prometheus, Grafana) |
| Testes | xUnit · Moq · FluentAssertions · Bogus (em evolução) |

---

## 🏗️ Arquitetura

Clean Architecture com a **regra de dependência**: as camadas internas não conhecem as externas.

```
┌─────────────────────────────────────────────────────────────┐
│                        SalesSample.WebApi                    │
│   Controllers · ProblemDetails · OpenTelemetry · Swagger     │
└───────────────────────────┬─────────────────────────────────┘
                            │ depende de
┌───────────────────────────▼─────────────────────────────────┐
│                        SalesSample.Domain                    │
│  Entidades · Value Objects · Eventos · Repositórios (portas) │
│  Specifications · Strategies · Domain Service                │
│                                                              │
│           ⚠️ ZERO dependência de projeto                     │
└───────────────────────────▲─────────────────────────────────┘
                            │ implementa / usa
        ┌───────────────────┴───────────────────┐
        │                                       │
┌───────▼────────────┐              ┌───────────▼───────────────┐
│  SalesSample.      │              │  SalesSample.             │
│  Application       │              │  Infrastructure           │
│  CQRS · Validação  │              │  EF Core · Repositórios   │
│                    │              │  Outbox · Kafka · Workers │
└────────────────────┘              └───────────────────────────┘
```

### Projetos

| Projeto | Responsabilidade | Depende de |
|---|---|---|
| `SalesSample.Domain` | Núcleo: entidades, VOs, eventos, portas, regras | **nada** |
| `SalesSample.Application` | Casos de uso (commands/queries), validadores | Domain |
| `SalesSample.Infrastructure` | EF Core, repositórios, Outbox, Kafka, workers | Domain + Application |
| `SalesSample.WebApi` | Controllers, DTOs, ProblemDetails, OTel | Application + Infrastructure |
| `SalesSample.UnitTests` | Testes unitários | Domain + Application |
| `SalesSample.IntegrationTests` | Testes de integração | WebApi |

---

## 🧠 Modelo de domínio (DDD)

### Agregado `Sale`

`Sale` é o **Aggregate Root**: a única porta de mutação do agregado, dona das invariantes.

```
Sale (AggregateRoot)
├── SaleNumber, SaleDate, Status
├── Customer (entidade)         CustomerId
├── Branch (entidade)           BranchId
└── SaleItem[] (itens)          SaleId
      └── Product, Quantity, UnitPrice (Money), Discount
```

**Invariantes protegidas pelo agregado:**
- Não vender mais de **20 itens iguais** (validado **dentro** de `Sale.AddItem`)
- Não adicionar itens a venda **cancelada**
- Não cancelar venda **já cancelada**
- Total com **uma única moeda**

### Value Objects

- **`Money`** — `sealed record` imutável com `Amount` + `Currency` e operadores (`+`, `-`, `*`) que garantem **mesma moeda**. Mapeado como **EF Core Complex Type** (não é entidade, não tem Id).

### Padrões aplicados

| Padrão | Onde |
|---|---|
| **Strategy** | `IDiscountStrategy` / `StandardDiscountStrategy` — desconto por faixa de quantidade |
| **Factory** | `DiscountStrategyFactory` — promoção nova = classe nova |
| **Specification** | `ISpecification<T>` / `ActiveBranchSpecification` |
| **Domain Service** | `SaleService` — orquestra criação/cancelamento |
| **Repository** | portas no Domain, adapters no Infrastructure |
| **Unit of Work** | `IUnitOfWork` implementado pelo `DbContext` |
| **Domain Event** | `SaleCreatedEvent`, `SaleCancelledEvent` acumulados no agregado |

### Regra de desconto

```
quantidade 0–3   →  0%
quantidade 4–9   → 10%
quantidade 10–20 → 20%
quantidade > 20  →  bloqueado pela invariante do agregado
```

---

## 🔁 CQRS

**Commands (escrita):** `CreateSale`, `CancelSale`
**Queries (leitura):** `GetSale`

Cada comando/query passa pelo **pipeline do MediatR**, com um `ValidationBehavior` (Chain of Responsibility) que valida **antes** do handler. Os handlers **orquestram** (carregam, delegam ao domínio, persistem) — sem regra de negócio.

---

## 📤 Transactional Outbox (o coração)

### Problema

Salvar no Postgres **e** publicar no Kafka são dois sistemas: não existe transação atômica entre eles (*dual-write problem*). Publicar direto traz dois riscos:

- salva no banco, Kafka cai → **evento perdido**;
- publica no Kafka, banco falha → **evento fantasma**.

### Solução

```
POST /api/sales
      │
      ▼
CreateSaleHandler
      │
      ├─ Sale + itens (em memória)
      └─ SaveChanges()
             │
             ▼
      [MESMA transação Postgres]
        ├─ INSERT Sales / SaleItems
        └─ INSERT OutboxMessages   ← evento serializado (JSON), ProcessedOn = NULL

OutboxPublisherWorker (BackgroundService, polling a cada 5s)
      ├─ SELECT ... FROM OutboxMessages WHERE ProcessedOn IS NULL
      ├─ publish no Kafka (tópico = tipo do evento)
      └─ UPDATE ProcessedOn = agora

Kafka ──► KafkaConsumer (BackgroundService, idempotente)
              ├─ deduplica por EventId (tabela InboxMessages)
              └─ processa
```

**Garantia:** atomicidade entre estado e evento + entrega **at-least-once**.

### Idempotência (Inbox)

Duplicatas são esperadas (*at-least-once*). O consumidor grava cada `EventId` processado numa tabela **`InboxMessages`** (chave primária = `EventId`). Se o evento já existe → **ignora**.

> **Outbox** (produtor) garante "não perde"; **Inbox** (consumidor) garante "não duplica". Juntos: **efetivamente uma vez**.

---

## 📊 Observabilidade

Instrumentação em **padrões abertos** (OpenTelemetry): trocar de fornecedor = trocar a URL do exporter, **zero código de negócio**.

| Sinal | Implementação | Destino |
|---|---|---|
| **Traces** | `AddAspNetCoreInstrumentation` + `AddHttpClientInstrumentation` | OTLP → Tempo |
| **Métricas** | `AddAspNetCoreInstrumentation` + `AddRuntimeInstrumentation` | `/metrics` → Prometheus |
| **Logs** | logs estruturados no console | (a evoluir: Loki) |

Cada erro retornado pela API carrega o **`traceId`** (W3C), permitindo correlacionar o erro ao trace completo no Tempo.

---

## 🌐 API REST

Base: `/api/sales`

| Método | Rota | Descrição | Status |
|---|---|---|---|
| `POST` | `/api/sales` | Cria venda | `201 Created` + `Location` |
| `GET` | `/api/sales/{id}` | Consulta venda | `200 OK` / `404 Not Found` |
| `POST` | `/api/sales/{id}/cancel` | Cancela venda | `204 No Content` / `409 Conflict` |

**Erros** seguem **ProblemDetails (RFC 9457)**:

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Business rule violation",
  "status": 409,
  "detail": "Sale is already cancelled.",
  "instance": "/api/sales/{id}/cancel",
  "traceId": "00-f06d34e270e245e19eaa2ffe32ddaf32-..."
}
```

### Exemplo

```bash
curl -X POST http://localhost:5207/api/sales \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "00000000-0000-0000-0000-000000000000",
    "branchId":   "00000000-0000-0000-0000-000000000000",
    "items": [
      { "productId": "00000000-0000-0000-0000-000000000000", "quantity": 4  },
      { "productId": "00000000-0000-0000-0000-000000000000", "quantity": 10 }
    ]
  }'
```

> Quantidade 4 → 10% de desconto; quantidade 10 → 20%.

---

## ▶️ Como rodar

### Pré-requisitos

- .NET 10 SDK
- Docker + Docker Compose

### 1. Subir a infraestrutura

```bash
docker compose up -d
```

Serviços: `postgres`, `kafka`, `kafka-ui`, `tempo`, `prometheus`, `grafana`.

### 2. Criar o banco (migrations)

```bash
dotnet ef database update \
  --project src/SalesSample.Infrastructure \
  --startup-project src/SalesSample.Infrastructure
```

### 3. Criar os tópicos do Kafka

```bash
for t in SaleCreatedEvent SaleCancelledEvent; do
  docker exec salessample-kafka /opt/kafka/bin/kafka-topics.sh \
    --bootstrap-server localhost:9092 --create --if-not-exists \
    --topic $t --partitions 3 --replication-factor 1
done
```

### 4. Carga inicial (3 filiais, 10 clientes, 100 produtos)

```bash
docker exec -i salessample-postgres psql -U sales -d salessample < carga_inicial.sql
```

### 5. Rodar a API

```bash
cd src/SalesSample.WebApi
dotnet run
```

- **Swagger:** http://localhost:5207/swagger
- **Métricas:** http://localhost:5207/metrics

### Portas

| Serviço | URL |
|---|---|
| API | `http://localhost:5207` |
| Postgres | `localhost:5433` |
| Kafka | `localhost:29092` |
| Kafka UI | `http://localhost:8083` |
| Prometheus | `http://localhost:9091` |
| Grafana | `http://localhost:3002` |
| Tempo (OTLP gRPC) | `localhost:4320` |

---

## 🧪 Testes (em evolução)

Estratégia em **pirâmide**:

| Tipo | Pergunta | Ferramentas | Status |
|---|---|---|---|
| Unitário | A regra está certa, isolada? | xUnit, Moq, FluentAssertions, Bogus | 🔄 |
| Integração | Funciona com banco real? | Testcontainers (Postgres) | 🔄 |
| API | A requisição HTTP inteira funciona? | `WebApplicationFactory` | 🔄 |
| Mutação | Meus testes pegam erro? | Stryker.NET | 🔄 |

---

## 🧭 Decisões de arquitetura e trade-offs

- **Outbox em vez de publicar direto:** +1 tabela e +1 worker em troca de **não perder eventos** e atomicidade. Vale para dinheiro.
- **TotalAmount calculado, não persistido:** evita dado redundante dessincronizado; custo é recalcular na leitura.
- **`Money` como Complex Type (não owned/entity):** é valor, não entidade; sem Id, sem tabela.
- **Idempotência no consumidor (Inbox):** a entrega é *at-least-once* por natureza; a responsabilidade de "não duplicar" é do consumidor.
- **Invariantes no agregado (e não em Specification):** a regra "máx 20 itens" vive em `Sale.AddItem`, junto do estado — evita o clássico bug de validar antes de aplicar.
- **Lifetimes do DI:** `DbContext`/repositórios `Scoped` (não thread-safe, unidade de trabalho); produtor Kafka `Singleton` (conexão cara e thread-safe); workers criam escopo próprio via `IServiceScopeFactory`.

---

## 🚧 Próximos passos

- [ ] Testes unitários (domínio, handlers, validadores)
- [ ] Testes de integração com Testcontainers
- [ ] Teste de mutação (Stryker.NET)
- [ ] Resiliência: retry/timeout/circuit breaker + DLQ no Kafka
- [ ] Métrica de negócio (`sales_created_total`)
- [ ] Logs estruturados (Serilog → Loki)
- [ ] Health checks (`/health`, `/health/ready`)
- [ ] CI/CD (GitHub Actions: build, testes, cobertura, mutação)

---

## 📂 Estrutura

```
SalesSample/
├── src/
│   ├── SalesSample.Domain/
│   │   ├── Common/            BaseEntity, AggregateRoot, DomainEvent
│   │   ├── Entities/          Sale, SaleItem, Product, Customer, Branch
│   │   ├── ValueObjects/      Money
│   │   ├── Events/            SaleCreatedEvent, SaleCancelledEvent
│   │   ├── Strategies/        IDiscountStrategy, StandardDiscountStrategy
│   │   ├── Specifications/    ISpecification, ActiveBranchSpecification
│   │   ├── Repositories/      portas (interfaces)
│   │   └── Services/          ISaleService, SaleService
│   ├── SalesSample.Application/
│   │   ├── Sales/Commands/    CreateSale, CancelSale
│   │   ├── Sales/Queries/     GetSale
│   │   └── Common/Behaviors/  ValidationBehavior
│   ├── SalesSample.Infrastructure/
│   │   ├── Persistence/       SalesDbContext, Mapping, Repositories, Outbox, Inbox
│   │   └── Messaging/         KafkaEventPublisher, OutboxPublisherWorker, KafkaConsumer
│   └── SalesSample.WebApi/
│       ├── Controllers/       SalesController
│       └── Middleware/        GlobalExceptionHandler
├── tests/
│   ├── SalesSample.UnitTests/
│   └── SalesSample.IntegrationTests/
├── carga_inicial.sql
└── docker-compose.yml
```

---

## ✍️ Autor

Desenvolvedor backend .NET sênior — estudo de arquitetura, mensageria confiável e observabilidade aplicada a um domínio real de vendas.
