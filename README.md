# SalesSample — API de Vendas com Clean Architecture, DDD, CQRS e Transactional Outbox

API backend em **.NET 10** que implementa **Clean Architecture + DDD + CQRS (MediatR)** e **elimina o dual-write problem** — o maior fantasma de sistemas com **EDA (Event-Driven Architecture)** — usando **Transactional Outbox**: os eventos chegam ao **Kafka** sem nunca serem perdidos, com **consumidor idempotente (Inbox)** e **observabilidade completa** via **OpenTelemetry**.

> Projeto de estudo/portfólio construído do zero, simulando um sistema de vendas do agronegócio (o mesmo domínio de plataformas de dados para crédito rural e monitoramento de safras).

---

## 📌 Visão geral

O sistema recebe vendas via API REST, persiste no **PostgreSQL** e publica eventos de domínio no **Kafka** sem perder mensagens — mesmo que o broker esteja fora do ar no momento da escrita. A garantia vem do padrão **Transactional Outbox**: o evento é gravado na **mesma transação** da venda e um worker o entrega ao Kafka depois.

O consumidor é **idempotente** (padrão **Inbox**): mensagens duplicadas — inevitáveis em entrega *at-least-once* — são detectadas e ignoradas.

---

## 🚨 O FANTASMA DAS ARQUITETURAS ORIENTADAS A EVENTOS: **DUAL WRITE**

> **Se você ler uma única seção deste README, leia esta.**

Em qualquer sistema com **EDA (Event-Driven Architecture)**, toda operação que muda estado precisa fazer **DUAS escritas**:

1. **no banco de dados** — persistir o estado;
2. **no broker de mensagens** (Kafka / RabbitMQ / SQS) — publicar o evento.

O problema é fatal: **Postgres e Kafka são sistemas separados e NÃO compartilham transação.** Não existe `BEGIN ... COMMIT` que abranja os dois. Esse é o **dual-write problem** — o maior fantasma de sistemas com EDA, porque **não aparece em teste feliz** e gera inconsistência silenciosa em produção:

| Cenário | Resultado |
|---|---|
| Salva no banco ✅ → **Kafka cai** ❌ | Venda existe, **evento perdido** — nenhum consumidor nunca fica sabendo |
| Publica no Kafka ✅ → **banco falha** ❌ | Evento publicado, **venda não existe** — consumidores agem sobre um fantasma |
| Processo morre **entre** as duas escritas | Estado e evento **divergem para sempre** |

E **não dá pra resolver com retry**, porque não existe ordem "segura": retry de um lado duplica, retry do outro perde. O dual-write é uma armadilha estrutural, não um bug de código.

### ✅ O que o Transactional Outbox faz: mata o dual-write

O Transactional Outbox **transforma duas escritas em UMA só**, no mesmo banco:

> O evento **não vai para o Kafka na hora da operação**. Ele é gravado numa tabela **`OutboxMessages`**, **na mesma transação** que grava a venda. É o Postgres quem garante a atomicidade — e depois um **worker** entrega o evento ao Kafka.

```
❌ SEM Outbox (dual write)               ✅ COM Outbox (uma escrita, atômica)
   BEGIN                                    BEGIN
     INSERT Sale                              INSERT Sale
   COMMIT                                   INSERT OutboxMessages
   ─────── ⚠️ gap entre sistemas ─────────    COMMIT   ← um só sistema, atômico
   PRODUCE Kafka
                                          worker → Kafka (depois)
```

**Resultado:** ou a venda **e** o evento existem, ou **nenhum** dos dois. O Kafka pode ficar fora do ar por horas — o evento espera na tabela e é entregue quando voltar. **"Evento perdido" deixa de ser uma possibilidade.**

> **Frase de entrevista:** *"O Outbox troca uma escrita em dois sistemas por uma escrita atômica em um sistema; o broker passa a ser entregue por um worker, com at-least-once. O dual-write vira um não-problema."*

---

## 🧱 Stack

| Camada | Tecnologia |
|---|---|
| Linguagem / Runtime | C# 13 · .NET 10 |
| Web | ASP.NET Core (Controllers) · Swagger (Swashbuckle) |
| Aplicação | MediatR (CQRS) · FluentValidation (pipeline) |
| Persistência | EF Core 10 · PostgreSQL 17 |
| Mensageria | Apache Kafka 4.0 (KRaft) · Confluent.Kafka |
| Observabilidade | OpenTelemetry → Grafana Tempo (traces) + Prometheus (métricas) + Loki (logs) + Grafana (UI) |
| Resiliência | `EnableRetryOnFailure` · producer idempotente · retry + DLQ |
| Infra local | Docker Compose (API, Postgres, Kafka, Kafka UI, Tempo, Prometheus, Loki, Grafana) |
| Testes | xunit.v3 (MTP) · Moq · Bogus · Shouldly · Testcontainers |

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
| `SalesSample.IntegrationTests` | Testes de integração | Infrastructure |

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

## 🛡️ Resiliência

| Camada | Política | Por quê |
|---|---|---|
| **Postgres** | `EnableRetryOnFailure(5, até 10s)` | retenta erro transitório (connection reset, deadlock) |
| **Producer Kafka** | `EnableIdempotence + Acks.All + retries` | retry **sem duplicar** (PID + sequence do broker) |
| **Consumer Kafka** | retry 3x com backoff exponencial + **DLQ** | erro transitório se resolve; mensagem "venenosa" vai pra `<tópico>-dlq` e **libera a partição** |

```
Consumer ──consome──► Processa (retry 3x, backoff 2s/4s)
                              │
                              └─ falhou 3x ──► DLQ (<tópico>-dlq) ──► Commit (não trava)
```

**Por que DLQ:** sem ela, uma mensagem que sempre falha trava a partição para sempre. Com ela, a mensagem é **preservada** para investigação e o fluxo segue.

---

## 📊 Observabilidade

Instrumentação em **padrões abertos** (OpenTelemetry): trocar de fornecedor = trocar a URL do exporter, **zero código de negócio**.

| Sinal | Implementação | Destino |
|---|---|---|
| **Traces** | `AddAspNetCoreInstrumentation` + `AddHttpClientInstrumentation` | OTLP → Tempo |
| **Métricas de infra** | `AddAspNetCoreInstrumentation` + `AddRuntimeInstrumentation` | `/metrics` → Prometheus |
| **Métrica de negócio** | `Counter` `sales_created_total` (`Meter` "SalesSample") | `/metrics` → Prometheus |
| **Logs** | Serilog (JSON) com labels `app`/`level` | Loki |
| **Health** | `/health` (liveness) + `/health/ready` (DB + Kafka) | orquestrador |

- Cada erro da API carrega o **`traceId`** (W3C) → correlaciona o erro ao trace no Tempo.
- **Health checks:** liveness não checa dependências (não reinicia o pod à toa); readiness checa Postgres e Kafka com timeout de 3s e devolve o detalhe por check.

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
    "customerId": "<uuid>",
    "branchId":   "<uuid>",
    "items": [
      { "productId": "<uuid>", "quantity": 4  },
      { "productId": "<uuid>", "quantity": 10 }
    ]
  }'
```

> Quantidade 4 → 10% de desconto; quantidade 10 → 20%.

---

## ▶️ Como rodar

### Pré-requisitos

- Docker + Docker Compose
- .NET 10 SDK (só pra migrations/CLI)

### 1. Subir tudo (API + infra)

```bash
docker compose up -d
docker compose ps
```

Serviços: `api`, `postgres`, `kafka`, `kafka-ui`, `tempo`, `prometheus`, `loki`, `grafana`.

### 2. Criar o banco (migrations)

```bash
dotnet ef database update \
  --project src/SalesSample.Infrastructure \
  --startup-project src/SalesSample.Infrastructure
```

### 3. Criar os tópicos do Kafka (eventos + DLQ)

```bash
for t in SaleCreatedEvent SaleCancelledEvent SaleCreatedEvent-dlq SaleCancelledEvent-dlq; do
  docker exec salessample-kafka /opt/kafka/bin/kafka-topics.sh \
    --bootstrap-server localhost:9092 --create --if-not-exists \
    --topic $t --partitions 3 --replication-factor 1
done
```

### 4. Carga inicial (3 filiais, 10 clientes, 100 produtos)

```bash
docker exec -i salessample-postgres psql -U sales -d salessample < carga_inicial.sql
```

### 5. Acessar

- **Swagger:** http://localhost:5207/swagger
- **Métricas:** http://localhost:5207/metrics
- **Health:** http://localhost:5207/health (liveness) · `/health/ready` (readiness)

### Portas

| Serviço | URL |
|---|---|
| API | `http://localhost:5207` |
| Postgres | `localhost:5433` |
| Kafka | `localhost:29092` |
| Kafka UI | `http://localhost:8083` |
| Prometheus | `http://localhost:9091` |
| Grafana | `http://localhost:3002` |
| Loki | `localhost:3101` |
| Tempo (OTLP gRPC) | `localhost:4320` |

---

## 🧪 Testes

Estratégia em **pirâmide**:

| Tipo | Pergunta | Ferramentas | Status |
|---|---|---|---|
| **Unitário** | A regra está certa, isolada? | xunit.v3 · Moq · Bogus · Shouldly | ✅ 35 testes |
| **Integração** | Funciona com banco real? | Testcontainers (Postgres) · `IClassFixture` | ✅ 3 testes |
| **API** | A requisição HTTP inteira funciona? | `WebApplicationFactory` + Testcontainers | ✅ 7 testes |
| **Mutação** | Meus testes pegam erro? | Stryker.NET | ⏸️ aguardando suporte a MTP |

**Total: 45 testes** (35 unitários + 10 de integração/API).

**Cobertura atual:**
- Domínio: `Money`, `Sale` (invariantes), `SaleItem`, `StandardDiscountStrategy` (bordas 3/4/9/10/20/21)
- Application: `CreateSaleHandler` (Moq), `CreateSaleValidator` (FluentValidation + Shouldly)
- Integração: atomicidade do Outbox, não-duplicação, **rollback** (constraint violada → nenhum evento vaza)
- **API:** POST `201` + `Location`, GET `200`/`404`, cancel `204`, cancel 2x `409`, payload inválido `400`, `/health` `200` — API em memória (`WebApplicationFactory`) + **Postgres real** (Testcontainers)
- Padrão AAA em todos os testes; `DisplayName` legível; Bogus com semente fixa (determinístico)

> **Mutação:** o Stryker.NET 5.0 ainda não suporta `Microsoft.Testing.Platform` ([issue #3094](https://github.com/stryker-mutator/stryker-net/issues/3094)). O conceito é demonstrado manualmente: quebrar a borda do desconto (`>= 4` → `> 4`) faz o teste falhar.

---

## 🧭 Decisões de arquitetura e trade-offs

- **Outbox em vez de publicar direto:** +1 tabela e +1 worker em troca de **não perder eventos** e atomicidade. Vale para dinheiro.
- **TotalAmount calculado, não persistido:** evita dado redundante dessincronizado; custo é recalcular na leitura.
- **`Money` como Complex Type (não owned/entity):** é valor, não entidade; sem Id, sem tabela.
- **Idempotência no consumidor (Inbox):** a entrega é *at-least-once* por natureza; a responsabilidade de "não duplicar" é do consumidor.
- **Invariantes no agregado (e não em Specification):** a regra "máx 20 itens" vive em `Sale.AddItem`, junto do estado — evita o clássico bug de validar antes de aplicar.
- **Lifetimes do DI:** `DbContext`/repositórios `Scoped` (não thread-safe, unidade de trabalho); produtor Kafka `Singleton` (conexão cara e thread-safe); workers criam escopo próprio via `IServiceScopeFactory`.
- **Liveness × readiness:** separadas; liveness não checa dependências (Kubernetes não reinicia à toa), readiness checa DB/Kafka.
- **Retry só onde é seguro:** producer idempotente (`EnableIdempotence`) para retry sem duplicar; consumer com limite + DLQ.

---

## 🚧 Próximos passos

- [ ] CI/CD (GitHub Actions: build, testes, cobertura)
- [ ] Teste de mutação (quando o Stryker suportar MTP)
- [ ] Logs com `traceId` em cada linha (`Serilog.Enrichers.Span`)
- [ ] Dashboards no Grafana + alertas (Alertmanager)
- [ ] `Idempotency-Key` no POST de venda

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
│   │   ├── Common/Behaviors/  ValidationBehavior
│   │   └── Common/Telemetry/  SalesMetrics (métrica de negócio)
│   ├── SalesSample.Infrastructure/
│   │   ├── Persistence/       SalesDbContext, Mapping, Repositories, Outbox, Inbox
│   │   └── Messaging/         KafkaEventPublisher, OutboxPublisherWorker, KafkaConsumer
│   └── SalesSample.WebApi/
│       ├── Controllers/       SalesController
│       ├── Middleware/        GlobalExceptionHandler
│       └── Dockerfile         multi-stage (build + runtime)
├── tests/
│   ├── SalesSample.UnitTests/          35 testes
│   └── SalesSample.IntegrationTests/   10 testes (Outbox + API)
├── carga_inicial.sql
└── docker-compose.yml
```

---

## ✍️ Autor

Desenvolvedor backend .NET sênior — estudo de arquitetura, mensageria confiável e observabilidade aplicada a um domínio real de vendas.
