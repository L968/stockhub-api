# Stockhub

Monorepo for an asset market simulator.

## Structure

- `backend/`: .NET API, modules, Matching Engine, migrations, Aspire, and tests.
- `frontend/`: React, TypeScript, and Vite application.

## Backend

```bash
dotnet run --project backend/aspire/Stockhub.Aspire/Stockhub.Aspire.AppHost
```

```bash
dotnet test backend/Stockhub.slnx
```

See the [architecture documentation](backend/ARCHITECTURE.md).

## Frontend

Aspire starts the frontend together with the API. To work only on the interface:

```bash
cd frontend
npm install
npm run dev
```

Copy `frontend/.env.example` to `frontend/.env.local` when running the frontend outside Aspire.

## License

[MIT](LICENSE.txt)
