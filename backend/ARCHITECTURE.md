# Stockhub — arquitetura

## O que é

O Stockhub é um backend de portfólio que simula um mercado de ativos, inspirado em bolsas de valores e no mercado da Steam. Usuários consultam ativos, enviam ordens de compra e venda, acompanham o livro de ofertas, trades e carteira.

## Visão geral

```text
Cliente
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

O .NET Aspire é o único orquestrador local. Ele inicia PostgreSQL, RabbitMQ, Migration Service, API e Matching Engine.

## Projetos principais

- `src/Api/Stockhub.Api`: entrada HTTP, configuração e registro dos módulos.
- `src/Modules`: módulos Users, Stocks e Orders, organizados em Domain, Application, Infrastructure e Presentation.
- `src/Consumers/Stockhub.Consumers.MatchingEngine`: consome partições de ordens, mantém os livros locais em memória e executa trades.
- `src/MigrationService`: aplica as migrations antes dos serviços iniciarem.
- `src/Common`: contratos e componentes realmente compartilhados.
- `aspire/Stockhub.Aspire`: orquestra toda a aplicação localmente.

## Fluxo de uma ordem

1. A API recebe a ordem e usa `X-User-Id` como identificação simplificada e intencional.
2. Orders valida e grava a ordem e o evento `OrderPlaced` na mesma transação.
3. Um dispatcher simples publica o evento no RabbitMQ Super Stream.
4. O `StockId` define a partição; somente um worker do grupo processa cada partição.
5. A ordem entra no livro do ativo, respeitando prioridade de preço e tempo.
6. Quando o maior bid alcança o menor offer, uma transação atualiza trade, ordens, saldos e carteira.

O PostgreSQL é a fonte de verdade. O livro em memória é reconstruído a partir das ordens abertas no startup.

## Persistência entre módulos

O PostgreSQL usa schemas separados:

- `users`: usuários e saldos;
- `stocks`: ativos e snapshots;
- `orders`: ordens, trades, carteira e tabelas do Outbox.

Orders consulta dados mínimos de Users e Stocks pelas views PostgreSQL `orders.user_view` e `orders.stock_view`. Elas substituem as antigas tabelas replicadas e não armazenam cópias dos dados.

## Mensageria

- RabbitMQ Super Streams é o broker e mantém a ordem dentro de cada partição.
- Ordens são particionadas por `StockId`, garantindo afinidade de um ativo com um worker por vez.
- Single Active Consumer distribui as partições e faz failover quando workers entram ou saem.
- O Transactional Outbox evita perder o evento depois que a ordem foi salva.
- `OrderPlaced` leva todos os dados necessários; o matching não consulta o banco para cada ordem recebida.

## Estado e limites

- Cada worker reconstrói no startup somente os books das partições que recebeu.
- PostgreSQL continua sendo a fonte de verdade e valida a liquidação quando existe match.
- O modelo ainda não reserva saldo ou posição no momento da criação da ordem; insuficiência posterior cancela a ordem durante a liquidação.
- Não há autenticação real por escolha de escopo do portfólio.
- Não foi identificado um fluxo completo de cancelamento de ordens.

## Avaliação

A arquitetura demonstra módulos, Outbox, particionamento, processamento em memória, escala horizontal e liquidação transacional sem exigir infraestrutura desproporcional ao projeto.
