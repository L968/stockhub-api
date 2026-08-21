# Stockhub

Monorepo do simulador de mercado de ativos.

## Estrutura

- `backend/`: API .NET, módulos, Matching Engine, migrations, Aspire e testes.
- `frontend/`: aplicação React + TypeScript + Vite.

## Backend

```bash
dotnet run --project backend/aspire/Stockhub.Aspire/Stockhub.Aspire.AppHost
```

```bash
dotnet test backend/Stockhub.slnx
```

Veja [a documentação da arquitetura](backend/ARCHITECTURE.md).

## Frontend

```bash
cd frontend
npm install
npm run dev
```

Copie `frontend/.env.example` para `frontend/.env` quando executar o frontend fora do Aspire.

## Licença

[MIT](LICENSE.txt)
