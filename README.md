# cms-microservices

Two .NET 8 microservices (**user-service**, **content-service**) that show how I build services that stay correct when the network does not: transactional outbox, event-driven cross-service cascade, idempotent writes, resilience pipeline, and end-to-end observability. Everything runs locally with one `docker compose up`.

The deployment side lives in [cms-swarm-ops](https://github.com/Buraktskl/cms-swarm-ops) (Docker Swarm stacks, CI/CD with image scanning, Helm + Chaos Mesh); the admin UI in [cms-admin-web](https://github.com/Buraktskl/cms-admin-web).

```
                     Kong (:8000)  /users  /contents
                          │            │
                ┌─────────▼──┐   ┌─────▼────────┐   HTTP + Polly (timeout → retry → circuit breaker)
                │ user-service│◄──│content-service│   content-service validates the author on create
                │  :5002      │   │   :5001       │
                └──┬──────┬───┘   └──────┬──▲─────┘
                   │      │ outbox        │  │ UserDeleted consumer
              Postgres    └──► RabbitMQ ──┘  │
              (user_db)                    Postgres (content_db)      Redis: cache + idempotency
```

## Run it

```bash
git clone https://github.com/Buraktskl/cms-microservices && cd cms-microservices
docker compose up --build
```

Eleven containers start; migrations and seed data run on boot. Then:

| | URL |
|---|---|
| API through the gateway | http://localhost:8000/users · http://localhost:8000/contents |
| Swagger | http://localhost:5002/swagger · http://localhost:5001/swagger |
| Jaeger (traces) | http://localhost:16686 |
| Kibana (logs) | http://localhost:5601 |
| RabbitMQ | http://localhost:15673 |
| Portainer | http://localhost:9000 |

Development credentials are the image defaults set in `docker-compose.yml`; copy `.env.example` to `.env` to change them.

## The end-to-end scenario

```
1. POST /users      { "username": "alice", ... }            Idempotency-Key: <uuid>   → 201
2. POST /contents   { "title": "Hello", "authorId": <alice> }                         → 201
      content-service calls user-service over HTTP to validate the author (Polly pipeline)
3. DELETE /users/<alice>                                                               → 204
      user.Status = PendingDeletion  +  OutboxMessage("UserDeleted")   ← one DB transaction
      OutboxDispatcher publishes to RabbitMQ (retries, dead-letter after 5 attempts)
      UserDeletedConsumer in content-service soft-deletes every content by alice
      user.Status = Deleted
4. GET /contents    → alice's contents are gone
```

Send the same `POST` twice with the same `Idempotency-Key` and you get the same `201` with the same id; the handler runs once.

## What to look at

| Concern | Where | Why it is built this way |
|---|---|---|
| **Transactional outbox** | `UserService.Domain/Outbox/OutboxMessage.cs`, `UserService.Infrastructure/Outbox/OutboxDispatcher.cs` | `SaveChanges()` then `Publish()` loses the event if the process dies in between. The event row is written in the same transaction as the state change; a background dispatcher publishes it. Broker down = events queue up, nothing is lost. At-least-once, no 2PC. |
| **Choreographed saga** | `ContentService.Infrastructure/Messaging/UserDeletedConsumer.cs` | No central orchestrator: user-service emits a fact, content-service reacts. Each side owns its own data and its own retry. |
| **Idempotent writes** | `*.API/Middleware/IdempotencyMiddleware.cs` | Mutations with an `Idempotency-Key` store their first response in Redis (24 h). A client retry after a timeout cannot create a second record. |
| **Resilience pipeline** | `ContentService.Infrastructure/HttpClients/UserServiceClient.cs` | Polly v8: 3 s timeout → 3 retries with exponential backoff and jitter → circuit breaker (50 % failures over 5 min opens for 30 s). An open circuit returns `503` immediately instead of piling requests onto a struggling dependency. |
| **Caching** | `*.Infrastructure/Cache/RedisCacheService.cs` | Read endpoints cached 5 min; every write invalidates exactly the keys it affects (`users:list`, `user:{id}`), not the whole cache. |
| **CQRS + MediatR** | `*.Application/Commands`, `*.Application/Queries` | One handler per use case, validated with FluentValidation; controllers only dispatch. |
| **Clean Architecture** | `Domain → Application → Infrastructure → API` | Domain has no dependencies and carries the rules (`User.MarkAsPendingDeletion()`, `Content.SoftDelete()`); no anemic models. |
| **Database per service** | `docker-compose.yml` | Two PostgreSQL instances. content-service never reads `user_db`; it asks over HTTP or reacts to events. |
| **Observability** | `*.API/Program.cs`, `*.API/Middleware/CorrelationIdMiddleware.cs` | Serilog structured JSON → Elasticsearch/Kibana; OpenTelemetry traces → Jaeger; `X-Correlation-Id` accepted or generated and carried through HTTP calls, logs and the RabbitMQ hop, so one id finds a request everywhere. |
| **Gateway** | `docker/kong/kong.yml` | Kong DB-less: rate limiting, CORS, `/users` → `/api/v1/users` path rewrite so the API can be versioned without changing public routes. |
| **Configuration** | `*.API/Program.cs` | Env vars in compose; `/run/secrets` files in Swarm/Kubernetes (`AddKeyPerFile`). Same binary, no code change between environments. |
| **Tests** | `*.Tests/` | Unit tests for handlers (xUnit, Moq, FluentAssertions) and integration tests over the real HTTP surface with a real PostgreSQL and Redis via Testcontainers, including the idempotency replay case. |

## Tests

```bash
dotnet test src/UserService/UserService.Tests --filter "FullyQualifiedName!~Integration"   # unit, no Docker
dotnet test src/ContentService/ContentService.Tests
dotnet test src/UserService/UserService.Tests --filter "FullyQualifiedName~Integration"    # Testcontainers, needs Docker
```

## Deliberately not in scope

Kept out to keep the repo readable; each is a straightforward addition on top of what is here:

* **Authentication.** Both services are behind the gateway; adding JWT bearer (or Keycloak) is a middleware registration plus `[Authorize]`. The admin UI repo shows the client side of a cookie-based JWT flow.
* **Distributed locking (Redlock)** — single Redis node is enough here.
* **Metrics.** Prometheus/Grafana, alerting and dashboards are in cms-swarm-ops, where they belong.

## Stack

.NET 8 · ASP.NET Core · EF Core 8 (code-first, migrations) · PostgreSQL 16 · RabbitMQ 3.13 · Redis 7 · MediatR · Polly v8 · Serilog · OpenTelemetry · Kong 3.6 · Swashbuckle · Asp.Versioning · xUnit · Moq · FluentAssertions · Testcontainers

## License

MIT
