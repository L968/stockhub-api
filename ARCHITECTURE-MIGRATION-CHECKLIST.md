# Checklist temporário — simplificação da arquitetura

## Concluído

- [x] Preservar a arquitetura anterior na branch `archive/kafka-debezium`.
- [x] Migrar `Stockhub.sln` para `Stockhub.slnx`.
- [x] Remover Redis.
- [x] Remover Docker Compose, Kafka, Debezium, Kafka Connect, Zookeeper e Kafka UI.
- [x] Usar Aspire como único orquestrador local.
- [x] Adicionar PostgreSQL e RabbitMQ ao Aspire.
- [x] Substituir Kafka por RabbitMQ Super Streams.
- [x] Criar o contrato explícito `OrderPlaced`.
- [x] Implementar Transactional Outbox no módulo Orders.
- [x] Particionar ordens por `StockId` e usar Single Active Consumer.
- [x] Substituir as réplicas `mv_user` e `mv_stock` por views PostgreSQL.
- [x] Manter `X-User-Id` como simplificação intencional.
- [x] Atualizar README e documentação arquitetural.
- [x] Validar restore, builds principais e testes automatizados.

## Matching e carteira

- [x] Reduzir e clarificar as responsabilidades do Matching Engine.
- [x] Manter books em memória com prioridade por preço e tempo.
- [x] Tornar trade, preenchimentos, saldos e carteira atomicamente consistentes.
- [x] Atualizar carteira dentro da liquidação e remover o `PortfolioUpdater` vazio.
- [x] Remover caches, filas e abstrações internas sem uso.
- [x] Adicionar testes unitários e integração com Testcontainers para os fluxos principais.
- [ ] Avaliar reservas de saldo e posição ao criar ordens.
- [ ] Avaliar uma inbox persistente caso a semântica de redelivery precise ser endurecida.

## Fora do escopo atual

- [ ] Implementar cancelamento de ordens e o evento `OrderCancelled`.
- [ ] Implementar autenticação real.
- [ ] Remover este checklist ao concluir a remodelagem.
