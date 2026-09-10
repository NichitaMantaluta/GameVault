# GameStore (GameVault)

A full-stack digital game store demo: **ASP.NET Core** modular monolith + **React** storefront. Browse a catalog, manage a cart, check out with **Stripe**, and manage the catalog as an admin.

Everything runs **locally** (API, React, PostgreSQL, Keycloak, Stripe test mode). No cloud / Azure deployment in this repo.

---

## Tech stack

| Area | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Minimal APIs, EF Core, Npgsql |
| Frontend | React 19, TypeScript, Vite 8, keycloak-js |
| Database | PostgreSQL (local Docker) |
| Auth | Keycloak 26 (local Docker) |
| Payments | Stripe Checkout + webhooks (`Stripe.net`, test mode) |
| Tests | xUnit, Microsoft.AspNetCore.Mvc.Testing |
| Local containers | Docker Compose (PostgreSQL + Keycloak) |

---

## Solution layout

```text
GameStore/
├── GameStore.Api/          # HTTP API (games, cart, orders, Stripe webhook)
├── GameStore.Tests/        # API integration tests
├── GameStore.React/        # Storefront SPA
├── keycloak/               # Realm import + custom login theme
├── docker-compose.yml      # PostgreSQL + Keycloak
├── .env.example            # Safe config template
└── .env                    # Your local secrets (gitignored)
```

---

## Prerequisites

1. [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. [Node.js](https://nodejs.org/) (LTS recommended) and npm
3. [Docker Desktop](https://www.docker.com/products/docker-desktop/)
4. PostgreSQL client tools optional (`psql`)
5. A [Stripe](https://stripe.com/) **test-mode** account + [Stripe CLI](https://stripe.com/docs/stripe-cli) (for local webhooks)

---

## Quick start (local)

Order: env → Compose → API (migrations) → seed → React. Stripe CLI is optional and separate.

### 1. Clone and configure environment

```bash
git clone <your-repo-url>
cd GameStore
```

```bash
# bash / macOS / Linux
cp .env.example .env

# PowerShell
Copy-Item .env.example .env
```

Replace Stripe placeholders in `.env` when you use payments. Other defaults work for local setup.

> The API and Vite both load this root `.env`. Never commit real secrets.

### 2. Start PostgreSQL + Keycloak

```bash
docker compose up -d
docker compose ps
```

Wait until both are healthy. Postgres: `localhost:5432`. Keycloak: `http://localhost:8080` (imports the `GameStore` realm on first start).

### 3. Start the API

```bash
dotnet run --project GameStore.Api
```

- URL: `http://localhost:5261`
- Development applies EF migrations on startup

### 4. Seed catalog data (once, after migrations)

Seed needs tables from migrations, so run this after the API has started once:

```bash
psql -h localhost -U postgres -d gamestore -f GameStore.Api/Persistence/seed-dev-data.sql
```

Re-running truncates and reloads genres/games. For a empty DB: `docker compose down -v`, then compose up → API → seed.

### 5. Start the React app

```bash
cd GameStore.React
npm install
npm run dev
```

- Storefront: `http://localhost:5173`

### 6. Forward Stripe webhooks (optional — needed for completed orders)

```bash
stripe listen --forward-to http://localhost:5261/api/payments/stripe/webhook
```

Copy the `whsec_...` secret into `.env` as `Stripe__WebhookSecret`, then restart the API. Without this, checkout works but orders stay `Pending`.

```bash
# Stop Compose (keeps DB volume)
docker compose down

# Wipe Postgres data volume
docker compose down -v

# API tests
dotnet test
```

---

## Demo accounts (Keycloak)

| User | Password | Role |
|---|---|---|
| `admin` | `admin` | Admin (catalog management) |
| `customer` | `customer` | Customer (shop / cart / orders) |

Realm: **GameStore** (UI brand: **GameVault**). Client: **gamestore** (public OIDC + PKCE).  
Admin Console: `http://localhost:8080` (bootstrap user from `.env`).

---

## Trying the main flows

1. Open `http://localhost:5173` and browse the catalog.
2. Sign in as `customer` / `customer`.
3. Add a game to the cart → **Checkout**.
4. On Stripe’s page, use test card `4242 4242 4242 4242`, any future expiry, any CVC.
5. After payment (with Stripe CLI running), the order becomes **Completed** and the game shows as **Owned**.
6. Sign out, sign in as `admin` / `admin`, open **Account → Catalog** to create or edit games.

---

## Troubleshooting

| Problem | What to check |
|---|---|
| API won’t start / DB errors | `docker compose ps` — Postgres healthy? `.env` `POSTGRES_*` match Compose? |
| Empty catalog after first run | Start API once (migrations), then run `seed-dev-data.sql` |
| Login fails / CORS / redirect issues | Keycloak on `8080`? Realm imported? Vite on `5173`? |
| 401 on API calls | Signed in? Audience `gamestore`? `Authentication__Authority` correct? |
| Checkout works but order stays Pending | Stripe CLI listening? `Stripe__WebhookSecret` matches? API restarted? |
| “Already owned” / Owned button | Webhook completed the prior purchase? Refresh after payment. |
| Cover shows a letter instead of art | Image URL broken or empty — use a valid HTTPS URL. |

---

Personal / portfolio project. Local demo credentials and Stripe **test** keys only — do not use production secrets in `.env`.
