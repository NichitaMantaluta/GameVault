# GameStore (GameVault)

A full-stack digital game store demo built as an **ASP.NET Core modular monolith** with a **React** storefront. Shoppers can browse a catalog, manage a cart, and check out with **Stripe**. Admins can manage the catalog.

Everything runs **locally** (API, React, PostgreSQL, Keycloak, Stripe test mode). There is **no cloud / Azure deployment** in this repo yet.

This README is written so someone can understand the project quickly and run it on their machine.

---

## Highlights

- Vertical-slice backend (features organized by use case, not Controllers / Services / Repositories)
- JWT auth with Keycloak (local Docker)
- Customer cart + Stripe Checkout (test mode)
- Admin catalog management (create / update / soft-disable)
- Owned-game detection after completed purchases
- React SPA with TypeScript and Vite
- Automated API tests with xUnit and `WebApplicationFactory`

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

### Backend feature areas

- **Games** — public catalog & details; admin create / update / soft-disable
- **Cart** — authenticated add / update / remove / get
- **Orders** — create checkout session, list/get orders, owned-games lookup, Stripe webhook

---

## Features

### Shopper

- Browse active games with search and pagination
- View game details
- Sign in with Keycloak
- Add games to cart (blocked if already owned)
- Checkout via Stripe hosted payment page
- View order history and order details in Account

### Admin (`Admin` role)

- Manage catalog (list including inactive games)
- Create and edit games (name, description, price, genre, image URL)
- Soft-disable games (`IsActive = false`) so they leave the public catalog

### Platform behaviors

- EF Core migrations applied automatically when the API starts in Development
- Orders stay `Pending` until Stripe sends `checkout.session.completed`
- Completed purchases grant ownership; UI shows **Owned** and APIs reject repurchase

---

## Prerequisites

Install:

1. [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. [Node.js](https://nodejs.org/) (LTS recommended) and npm
3. [Docker Desktop](https://www.docker.com/products/docker-desktop/)
4. PostgreSQL client tools optional (`psql`); Postgres itself can run in Docker
5. A [Stripe](https://stripe.com/) **test-mode** account + [Stripe CLI](https://stripe.com/docs/stripe-cli) (for local webhooks)

You do **not** need an Azure account or any cloud resources to run this project.

---

## Quick start (local)

Order: env → Compose (Postgres + Keycloak) → API (migrations) → seed → React. Stripe CLI is optional and separate.

### 1. Clone and configure environment

```bash
git clone <your-repo-url>
cd GameStore
```

Create a `.env` file in the **repo root** (same folder as `docker-compose.yml`) from the template:

```bash
# bash / macOS / Linux
cp .env.example .env

# PowerShell
Copy-Item .env.example .env
```

Open `.env` and replace the Stripe placeholders with your **test** keys (`Stripe__SecretKey`, `Stripe__WebhookSecret`) when you use payments. The other values in `.env.example` work for the local setup below.

> The API and Vite both load this root `.env`. Never commit real secrets.

### 2. Start PostgreSQL + Keycloak

```bash
docker compose up -d
```

Wait until both services are healthy:

```bash
docker compose ps
```

- Postgres: `localhost:5432` (credentials from `.env` `POSTGRES_*`)
- Keycloak: `http://localhost:8080` (on first start it imports the `GameStore` realm and custom theme)

Data persists in the Compose volume `postgres_data`. Seed SQL is **not** mounted into `/docker-entrypoint-initdb.d/` — the script truncates/inserts into tables that EF migrations create, so auto-init would fail on an empty database.

### 3. Start the API

```bash
dotnet run --project GameStore.Api
```

- URL: `http://localhost:5261`
- Development mode applies EF migrations on startup (creates schema)

### 4. Seed catalog data (once, after migrations)

With the API having run at least once (tables exist), apply the seed script (12 genres, 50 sample games):

```bash
psql -h localhost -U postgres -d gamestore -f GameStore.Api/Persistence/seed-dev-data.sql
```

Or run the SQL from any Postgres client against database `gamestore`. Re-running truncates and reloads genres/games.

If you ever need a fully empty Postgres volume again: `docker compose down -v` (destroys DB data), then compose up → start API once → seed.

### 5. Start the React app

```bash
cd GameStore.React
npm install
npm run dev
```

- Storefront: `http://localhost:5173`

### 6. Forward Stripe webhooks (optional — needed for completed orders)

Stripe is **not** started by Compose. When you want checkout → completed orders:

```bash
stripe listen --forward-to http://localhost:5261/api/payments/stripe/webhook
```

Copy the `whsec_...` signing secret from the CLI into `.env` as `Stripe__WebhookSecret`, then **restart the API**.

Without this step, checkout can create a Stripe session, but orders remain `Pending` and ownership / cart clearing will not run.
---

## Demo accounts (Keycloak)

| User | Password | Role |
|---|---|---|
| `admin` | `admin` | Admin (catalog management) |
| `customer` | `customer` | Customer (shop / cart / orders) |

Realm: **GameStore** (UI brand name: **GameVault**)  
Client: **gamestore** (public OIDC + PKCE)

Keycloak Admin Console (container bootstrap user from `.env`): `http://localhost:8080`

---

## Ports

| Service | Port |
|---|---|
| React (Vite) | `5173` |
| API | `5261` |
| Keycloak | `8080` |
| PostgreSQL | `5432` |

---

## Trying the main flows

1. Open `http://localhost:5173` and browse the catalog.
2. Sign in as `customer` / `customer`.
3. Add a game to the cart → **Checkout**.
4. On Stripe’s page, use test card `4242 4242 4242 4242`, any future expiry, any CVC.
5. After payment, Stripe CLI should deliver the webhook; the order becomes **Completed**, the cart updates, and the game shows as **Owned**.
6. Sign out, sign in as `admin` / `admin`, open **Account → Catalog** to create or edit games.

---

## Useful commands

```bash
# Run all API tests
dotnet test

# Build frontend
cd GameStore.React && npm run build

# Stop Postgres + Keycloak (keeps DB volume)
docker compose down

# Stop and wipe Postgres data volume
docker compose down -v
```

---

## API surface (overview)

| Area | Examples |
|---|---|
| Games | `GET /api/games`, `GET /api/games/{id}`, admin `POST` / `PUT` / `DELETE` |
| Cart | `GET /api/cart`, `POST /api/cart/items`, `PATCH` / `DELETE` items |
| Orders | `POST /api/orders`, `GET /api/orders`, `GET /api/orders/{id}` |
| Library | `GET /api/orders/owned-games` |
| Payments | `POST /api/payments/stripe/webhook` |

OpenAPI is enabled in Development on the API.

---

## Design notes for reviewers

- **Vertical slices:** each use case lives under `Features/{Area}/{UseCase}/` with endpoint + request/response types nearby.
- **Auth:** Keycloak issues JWTs; the API validates issuer/audience and maps realm roles to the `role` claim. Admins need the `Admin` role.
- **Images:** stored as URL strings (seed data and admin form use public HTTPS image URLs). There is no file-upload or blob storage service.
- **Soft delete:** disabling a game hides it from the public catalog without removing history.
- **Scope:** this is a local portfolio demo. Cloud hosting, managed identity, and messaging are out of scope for the current codebase.

---

## Troubleshooting

| Problem | What to check |
|---|---|
| API won’t start / DB errors | `docker compose ps` — Postgres healthy? `.env` `POSTGRES_*` values match Compose? |
| Empty catalog after first run | Start API once (migrations), then run `seed-dev-data.sql` (see Quick start §4) |
| Login fails / CORS / redirect issues | Keycloak up on `8080`? Realm imported? Vite on `5173`? |
| 401 on API calls | Signed in? Token audience `gamestore`? `Authentication__Authority` correct? |
| Checkout works but order stays Pending | Stripe CLI listening? `Stripe__WebhookSecret` matches CLI secret? API restarted? |
| “Already owned” / Owned button | Webhook completed the prior purchase? Refresh the page after payment. |
| Cover shows a letter instead of art | Image URL broken or empty — edit the game and use a valid HTTPS image URL. |

---

## License / status

Personal / portfolio project. Local demo credentials and Stripe **test** keys only — do not use production secrets in `.env`.
