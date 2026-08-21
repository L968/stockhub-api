# Stockhub

Asset market simulator built as a portfolio project. The application lets users create accounts and assets, submit bids and offers, inspect the order book, and track trades and positions.

## Stack

- .NET 10 and ASP.NET Core
- PostgreSQL
- RabbitMQ Super Streams and Transactional Outbox
- .NET Aspire for local orchestration
- Entity Framework Core and Dapper
- xUnit and architecture tests

See [ARCHITECTURE.md](ARCHITECTURE.md) for the system map and current decisions.

## Run

Requirements: .NET 10 SDK and a Docker runtime compatible with .NET Aspire.

```bash
dotnet run --project aspire/Stockhub.Aspire/Stockhub.Aspire.AppHost
```

Aspire starts PostgreSQL, RabbitMQ, migrations, the API, and the Matching Engine. Endpoints and logs are available in the Aspire dashboard.

The Migration Service then applies [`database/seed.sql`](database/seed.sql). The script is transactional and idempotent, and creates the `demo@stockhub.dev` account, eight stocks, market-maker liquidity, portfolio positions, open orders, and historical trades.

## Test

```bash
dotnet test Stockhub.slnx
```

Integration tests use Testcontainers and require Docker to be running.

## User identification

Authenticated endpoints use the `X-User-Id` header. This is an intentional simplification for the project and is not production-ready authentication.

## License

[MIT](../LICENSE.txt)
