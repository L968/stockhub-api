# StockHub Web

React client for the StockHub market simulation.

## Run locally

```bash
npm install
npm run dev
```

The Aspire AppHost starts this frontend and injects the API URL. When running it separately, copy `.env.example` to `.env.local`.

## Project structure

- `src/app`: routing shell and global layout.
- `src/features`: code grouped by product area (`auth`, `dashboard`, `market`, `activity`).
- `src/components`: small shared presentation components.
- `src/lib`: API client, query cache, formatting and browser storage.
- `src/types`: contracts that mirror backend responses.

Remote data is managed by TanStack Query. Local component state is kept local; no global state library is needed at the current scale.

## Quality checks

```bash
npm run check
```

Tests intentionally cover the critical boundaries: identity persistence, API headers/errors and order submission.
