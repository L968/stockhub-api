# Temporary architecture simplification checklist

## Completed

- [x] Preserve the previous architecture in the `archive/kafka-debezium` branch.
- [x] Migrate `Stockhub.sln` to `Stockhub.slnx`.
- [x] Remove Redis.
- [x] Remove Docker Compose, Kafka, Debezium, Kafka Connect, Zookeeper, and Kafka UI.
- [x] Use Aspire as the only local orchestrator.
- [x] Add PostgreSQL and RabbitMQ to Aspire.
- [x] Replace Kafka with RabbitMQ Super Streams.
- [x] Create the explicit `OrderPlaced` contract.
- [x] Implement Transactional Outbox in the Orders module.
- [x] Partition orders by `StockId` and use Single Active Consumer.
- [x] Replace the `mv_user` and `mv_stock` replicas with PostgreSQL views.
- [x] Keep `X-User-Id` as an intentional simplification.
- [x] Update the README and architecture documentation.
- [x] Validate restore, primary builds, and automated tests.

## Matching and portfolio

- [x] Reduce and clarify Matching Engine responsibilities.
- [x] Keep order books in memory with price-time priority.
- [x] Make trades, fills, balances, and portfolio updates atomically consistent.
- [x] Update the portfolio during settlement and remove the empty `PortfolioUpdater`.
- [x] Remove unused caches, queues, and internal abstractions.
- [x] Add unit and Testcontainers integration tests for the primary flows.
- [ ] Evaluate balance and position reservations when orders are created.
- [ ] Evaluate a persistent inbox if stronger redelivery semantics become necessary.

## Outside the current scope

- [ ] Implement order cancellation and the `OrderCancelled` event.
- [ ] Implement real authentication.
- [ ] Remove this checklist when the redesign is complete.
