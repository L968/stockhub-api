# Stockhub

Simulador de mercado de ativos criado como projeto de portfólio. A aplicação permite cadastrar usuários e ativos, enviar bids e offers, consultar o order book e registrar trades e posições.

## Stack

- .NET 9 e ASP.NET Core
- PostgreSQL
- RabbitMQ Super Streams e Transactional Outbox
- .NET Aspire para orquestração local
- Entity Framework Core e Dapper
- xUnit e testes arquiteturais

Veja [ARCHITECTURE.md](ARCHITECTURE.md) para o mapa do sistema e as decisões atuais.

## Executar

Pré-requisitos: .NET 9 SDK e Docker compatível com .NET Aspire.

```bash
dotnet run --project aspire/Stockhub.Aspire/Stockhub.Aspire.AppHost
```

O Aspire inicia PostgreSQL, RabbitMQ, migrations, API e Matching Engine. Os endpoints e logs ficam disponíveis no dashboard do Aspire.

## Testar

```bash
dotnet test Stockhub.slnx
```

Os testes de integração usam Testcontainers e exigem Docker em execução.

## Identificação de usuário

Os endpoints autenticados usam o header `X-User-Id`. Isso é uma simplificação intencional do projeto; não representa autenticação pronta para produção.

## Licença

[MIT](../LICENSE.txt)
