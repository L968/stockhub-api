# Stockhub architecture

## Purpose

Stockhub is a portfolio backend that simulates an asset market inspired by stock exchanges and the Steam Community Market. Users inspect assets, submit buy and sell orders, and track the order book, trades, and portfolio.

## Overview

```text
Client
  |
Stockhub.Api
  |-- Users  -> schema users
  |-- Stocks -> schema stocks
  `-- Orders -> schema orders + Outbox
                         |
                      RabbitMQ
                         |
                  Matching Engine
                         |
                     PostgreSQL
```

.NET Aspire is the only local orchestrator. It starts PostgreSQL, RabbitMQ, the Migration Service, the API, and the Matching Engine.

## Main projects

- `src/Api/Stockhub.Api`: HTTP entry point, configuration, and module registration.
- `src/Modules`: Users, Stocks, and Orders modules organized into Domain, Application, Infrastructure, and Presentation layers.
- `src/Consumers/Stockhub.Consumers.MatchingEngine`: consumes order partitions, keeps local order books in memory, and executes trades.
- `src/MigrationService`: applies migrations before the services start.
- `src/Common`: contracts and components that are genuinely shared.
- `aspire/Stockhub.Aspire`: orchestrates the complete application locally.

## Order flow

1. The API receives an order and uses `X-User-Id` as intentional lightweight identification.
2. Orders validates and stores the order and its `OrderPlaced` event in the same transaction.
3. A lightweight dispatcher publishes the event to a RabbitMQ Super Stream.
4. `StockId` determines the partition, and only one worker in the group processes each partition.
5. The order enters the asset order book using price-time priority.
6. When the highest bid reaches the lowest offer, one transaction updates the trade, orders, balances, and portfolio.

PostgreSQL is the source of truth. In-memory order books are rebuilt from open orders at startup.

## Persistence across modules

PostgreSQL uses separate schemas:

- `users`: users and balances;
- `stocks`: assets and snapshots;
- `orders`: orders, trades, portfolio, and Outbox tables.

Orders reads the minimum required Users and Stocks data through the PostgreSQL views `orders.user_view` and `orders.stock_view`. These views replace the former replicated tables and do not store data copies.

## Messaging

- RabbitMQ Super Streams is the broker and preserves order within each partition.
- Orders are partitioned by `StockId`, keeping each asset assigned to one worker at a time.
- Single Active Consumer distributes partitions and handles failover as workers join or leave.
- Transactional Outbox prevents losing an event after its order has been stored.
- `OrderPlaced` carries all required data, so matching does not query the database for every received order.

## State and current limitations

- Each worker rebuilds only the order books for its assigned partitions at startup.
- PostgreSQL remains the source of truth and validates settlement when a match exists.
- The model does not yet reserve balance or position when an order is created; a later insufficiency cancels the order during settlement.
- Real authentication is intentionally outside the portfolio scope.
- A complete order cancellation flow has not been implemented.

## Assessment

The architecture demonstrates modularity, Outbox, partitioning, in-memory processing, horizontal scaling, and transactional settlement without infrastructure that is disproportionate to the project.
