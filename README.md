# fcs-audit-logs

Worker de **Auditoria Centralizada** da plataforma **Conexão Solidária**. Consome eventos explícitos de auditoria publicados pelas aplicações no Kafka e persiste os registros em MongoDB.

> Microsserviço que compõe o MVP da Conexão Solidária junto a `fcs-identity`, `fcs-campaigns`, `fcs-donations`, `fcs-donation-worker`, `fcs-solidarity-web` e `fcs-solidarity-infra`.

---

## Responsabilidades

- Consumir eventos `AuditLogRequestedEvent` do tópico Kafka `audit-log-requested`.
- Validar campos obrigatórios do evento de auditoria.
- Sanitizar metadados sensíveis antes da persistência.
- Persistir registros de auditoria no MongoDB (`AuditLogsDb.audit_logs`).
- Garantir idempotência por `eventId` com índice único.
- Confirmar o offset Kafka apenas após persistência bem-sucedida, duplicidade idempotente ou descarte controlado de payload inválido.
- Expor endpoints operacionais `/health` e `/metrics` quando configurados no ambiente de execução.

O `fcs-audit-logs` **não decide o que deve ser auditado**. Cada aplicação publica seus próprios eventos de negócio ou segurança nos casos de uso relevantes.

Documentação completa da arquitetura: [group10-tc-01/fcs-fase05-docs](https://github.com/group10-tc-01/fcs-fase05-docs).

Referências diretas:

- [Visão geral da arquitetura](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/architecture/overview.md)
- [Modelo de banco de dados](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/architecture/database-model.md)
- [Endpoints consolidados](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/architecture/endpoints.md)
- [Fluxos dos endpoints e workers](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/architecture/endpoint-flows.md)

ADRs relevantes:

- [ADR 0030 - Auditoria explícita centralizada](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0030-use-explicit-business-audit-logs.md)
- [ADR 0018 - Kafka dentro do Kubernetes](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0018-run-kafka-inside-kubernetes.md)
- [ADR 0023 - Estrutura interna .NET da fase 04](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0023-use-phase-04-dotnet-service-structure.md)
- [ADR 0026 - Namespaces Kubernetes separados](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0026-use-separated-kubernetes-namespaces.md)

---

## Fluxo de Auditoria

```text
fcs-identity
fcs-campaigns
fcs-donations
fcs-donation-worker
        |
        | Kafka topic audit-log-requested
        v
fcs-audit-logs
        |
        v
MongoDB AuditLogsDb.audit_logs
```

Regras importantes:

- Auditoria não fica nos databases relacionais dos serviços.
- Este fluxo não usa outbox.
- Falhas de publicação nas aplicações podem causar perda de auditoria, conforme decisão arquitetural.
- O worker trata duplicidade de `eventId` como sucesso.
- Senhas, tokens, refresh tokens e segredos não devem ser publicados em `metadata`.

---

## Contrato Kafka

Tópico:

```text
audit-log-requested
```

Evento:

```text
AuditLogRequestedEvent
```

Payload mínimo:

```json
{
  "eventId": "3c03f6e3-7c8d-43b8-8f94-4c4ef3b6b4e6",
  "occurredAt": "2026-05-18T20:00:00Z",
  "serviceName": "fcs-identity",
  "action": "DonorRegistered",
  "entityName": "DonorProfile",
  "entityId": "22222222-2222-2222-2222-222222222222",
  "actorId": "22222222-2222-2222-2222-222222222222",
  "actorType": "Doador",
  "correlationId": "correlation-id",
  "ipAddress": "127.0.0.1",
  "userAgent": "example-client",
  "metadata": {
    "source": "manual-test"
  }
}
```

Campos obrigatórios:

| Campo | Descrição |
|-------|-----------|
| `eventId` | Identificador único usado para idempotência |
| `occurredAt` | Data/hora UTC em que o evento ocorreu na aplicação de origem |
| `serviceName` | Nome do serviço que publicou o evento |
| `action` | Evento de negócio ou segurança auditado |
| `entityName` | Nome da entidade afetada |

Campos opcionais: `entityId`, `actorId`, `actorType`, `correlationId`, `ipAddress`, `userAgent`, `metadata`.

---

## Estrutura do Projeto

```text
src/
  fcs.Audit.Logs.Application/      # Consumer Kafka, validação, persistência MongoDB
    Common/
    Features/
      AuditLogRequested/
  fcs.Audit.Logs.Worker/           # Host .NET Worker, configuração, Dockerfile
tests/
  fcs.Audit.Logs.Application.Tests/
```

Estrutura interna alinhada ao padrão da fase 04 ([ADR 0023](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0023-use-phase-04-dotnet-service-structure.md)).

---

## Persistência

- Engine: **MongoDB**.
- Database: `AuditLogsDb`.
- Coleção: `audit_logs`.

Documento persistido:

| Campo | Obrigatório | Descrição |
|-------|-------------|-----------|
| `_id` | Sim | Identificador interno do MongoDB |
| `eventId` | Sim | Idempotência do evento recebido |
| `occurredAt` | Sim | Data/hora UTC do evento original |
| `receivedAt` | Sim | Data/hora UTC de persistência pelo worker |
| `serviceName` | Sim | Serviço que publicou o evento |
| `action` | Sim | Evento auditado |
| `entityName` | Sim | Entidade afetada |
| `entityId` | Não | Identificador da entidade afetada |
| `actorId` | Não | Perfil ou usuário que executou a ação |
| `actorType` | Não | Ex.: `Public`, `Doador`, `GestorONG`, `System` |
| `correlationId` | Não | Correlação da requisição/processamento |
| `ipAddress` | Não | IP de origem quando houver contexto HTTP |
| `userAgent` | Não | User-agent quando houver contexto HTTP |
| `metadata` | Não | Metadados sem segredos |

Índices criados na inicialização:

| Nome | Tipo |
|------|------|
| `UX_audit_logs_eventId` | Único em `eventId` |
| `IX_audit_logs_serviceName_action` | Composto em `serviceName`, `action` |
| `IX_audit_logs_entityName_entityId` | Composto em `entityName`, `entityId` |
| `IX_audit_logs_occurredAt` | Índice em `occurredAt` |
| `IX_audit_logs_correlationId` | Índice em `correlationId` |
| `IX_audit_logs_actorId` | Índice em `actorId` |

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) e Docker Compose
- Portas livres no host: `9092` (Kafka), `8081` (Kafka UI), `27017` (MongoDB), `5341` (Seq).

---

## Subindo o Ambiente Local

O `docker-compose.yml` deste repositório sobe apenas as dependências deste worker (Kafka, Kafka UI, MongoDB, Seq) e, opcionalmente, o próprio worker. Para o ambiente completo integrado da Conexão Solidária utilize o repositório `fcs-solidarity-infra`.

### 1. Subir dependências

```bash
docker compose up -d zookeeper kafka kafka-ui mongodb seq
```

URLs úteis:

- Kafka UI: http://localhost:8081
- MongoDB: `mongodb://localhost:27017`
- Seq: http://localhost:5341

### 2. Rodar o worker localmente

```bash
dotnet restore
dotnet build
dotnet run --project src/fcs.Audit.Logs.Worker
```

### 2b. Rodar o worker também em container

```bash
docker compose up -d --build fcs-audit-logs
```

### 3. Publicar evento manual para teste

```bash
docker exec -i kafka-fcs-audit-logs kafka-console-producer \
  --bootstrap-server kafka:29092 \
  --topic audit-log-requested
```

Payload de exemplo:

```json
{"eventId":"11111111-1111-1111-1111-111111111111","occurredAt":"2026-05-18T20:00:00Z","serviceName":"fcs-identity","action":"DonorRegistered","entityName":"DonorProfile","entityId":"22222222-2222-2222-2222-222222222222","actorId":"22222222-2222-2222-2222-222222222222","actorType":"Doador","correlationId":"manual-test","ipAddress":"127.0.0.1","userAgent":"manual","metadata":{"source":"manual-test"}}
```

Consultar no MongoDB:

```bash
docker exec -it mongodb-fcs-audit-logs mongosh AuditLogsDb \
  --eval "db.audit_logs.find({ eventId: '11111111-1111-1111-1111-111111111111' }).pretty()"
```

---

## Testes

```bash
# Todos os testes
dotnet test

# Projeto de testes da Application
dotnet test tests/fcs.Audit.Logs.Application.Tests
```

Cobertura mínima exigida pela esteira: **80%** ([ADR 0025](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0025-test-strategy-for-apis-and-worker.md)).

---

## Observabilidade

- Logs estruturados com **Serilog** enviados ao **Seq** em ambiente local.
- Consumo Kafka com logs de descarte de payload inválido, erro de processamento e retry por não commit de offset.
- Endpoints operacionais esperados no ambiente de execução:
  - `/health`
  - `/metrics`

Esses endpoints **não** são publicados no Azure API Management ([ADR 0027](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0027-keep-internal-apis-cluster-private.md)). Em ambiente local são consumidos pelo `Prometheus`/`Grafana` que rodam em `fcs-solidarity-infra`.

---

## CI/CD

A esteira fica em `.github/workflows/` reutilizando os workflows reutilizáveis do repositório `fcs-pipelines` ([ADR 0022](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0022-reuse-fcs-pipelines-for-ci-cd.md)):

- `branch-name-check.yml` - política de nomes de branch
- `dotnet-service-ci.yml` - build .NET, testes, SonarCloud, Trivy, build da imagem Docker
- `dotnet-service-delivery.yml` - push da imagem para Azure Container Registry e deploy em AKS

Gates principais: secret scan (Gitleaks), dependency scan, restore/build, testes com cobertura mínima de 80%, SonarCloud, Docker build, Trivy, deploy condicional, healthcheck pós-rollout.

---

## Kubernetes

Manifests Kubernetes deste worker (Deployment, Service, ConfigMap, Secret) ficam em `k8s/` (ou diretório equivalente neste repositório). Para o ambiente integrado (Kind local ou AKS), com Kafka, MongoDB, Prometheus e Grafana compartilhados, consulte o repositório `fcs-solidarity-infra` ([ADR 0026](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0026-use-separated-kubernetes-namespaces.md)).

Namespace alvo: `fcs-audit-logs`.

---

## Como este Worker Atende ao Hackathon

| Requisito do hackathon | Onde é atendido |
|------------------------|-----------------|
| Microsserviço distinto | `fcs-audit-logs` separado dos serviços de negócio |
| Mensageria assíncrona | Consumo do tópico Kafka `audit-log-requested` |
| Persistência NoSQL | MongoDB `AuditLogsDb.audit_logs` |
| Observabilidade | Logs estruturados, `/health` e `/metrics` |
| Imagem Docker e pipeline | `Dockerfile`, `docker-compose.yml` e workflows em `.github/workflows` |

Os eventos auditáveis são definidos pelos serviços de origem. O `fcs-audit-logs` centraliza persistência, idempotência e consulta futura dos logs de auditoria.
