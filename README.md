# Trading App Demo

Real-time trading demo: C# services stream order and bond-price events to a NestJS BFF over gRPC, the BFF fans them out over WebSocket, and a React UI renders live tables and charts.

```
┌────────────────────────────┐     gRPC stream      ┌──────────────────┐     WebSocket      ┌───────────────┐
│ Order Service (C#/.NET)    │ ───────────────────► │                  │                    │               │
│ simulated orders           │                      │ BFF (TS/Nest.js) │ ─────────────────► │ UI (TS/React) │
└────────────────────────────┘                      │                  │                    │               │
┌────────────────────────────┐     gRPC stream      │                  │                    │               │
│ Pricing Service (C#/.NET)  │ ───────────────────► │                  │                    │               │
│ bond prices                │                      └──────────────────┘                    └───────────────┘
└────────────────────────────┘
┌────────────────────────────┐
│ Reference Data (C#/.NET)   │
│ simulated bonds            │
└────────────────────────────┘
```

## Status

| Component | Stack | Status |
| --- | --- | --- |
| Orderbook | .NET 10 | In Progress |
| Prices | .NET 10 | Planned |
| Reference Data | .NET 10 | Planned |
| BFF | NestJS | In Progress |
| UI | React | Planned |

## Architecture

The orderbook service holds a simulated order book and streams `NEW` / `UPDATE` / `CANCEL` events. A second C# service will stream bond price updates. A third C# service will provide reference data for simulated bonds, used by both the order and pricing services. The BFF opens a single gRPC stream to each service, then relays events to subscribed browser clients. The UI will update in real-time and consist of an AG Grid table to display the orderbook and an AG Charts candlestick visualizer chart.

Protobuf contracts live in `protos/` and are shared by both backends. Use `buf generate` to generate protobuf code.
## Project layout

```
├── protos/                 Shared protobuf APIs
│   └── orderbook/v1/
├── orderbook/              C# gRPC orderbook service
├── pricing/                 C# gRPC bond-price service (planned)
├── reference-data/          C# reference data service (planned)
├── bff/                    NestJS BFF (gRPC client + WebSocket)
├── buf.yaml
└── buf.gen.yaml
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/)
- [pnpm](https://pnpm.io/)
- [Buf CLI](https://buf.build/docs/installation) (optional, for regenerating TS stubs)

## Run locally

Start the orderbook first, then the BFF.

### 1. Orderbook

```bash
dotnet run --project orderbook
```

Listens on `localhost:5023`.

### 2. BFF

```bash
cd bff
pnpm install
pnpm start:dev
```

Listens on `http://localhost:3000` (override with `PORT`). Connects to the orderbook at `localhost:5023`.

### 3. Subscribe from a client

Socket.IO events:

| Client → BFF | BFF → Client | Purpose |
| --- | --- | --- |
| `orderBook.subscribe` | `orderBook.update` | Start receiving order events |
| `orderBook.unsubscribe` | — | Stop receiving events |

Each `orderBook.update` payload is a `GetOrderEventsResponse`: an `Order` plus an `OrderAction` (`NEW`, `UPDATE`, or `CANCEL`).

Bond price subscribe/unsubscribe events will follow the same pattern once the prices service exists.

## Configuration

Orderbook settings in `orderbook/appsettings.json` under `AppConfig`:

| Key | Default | Description |
| --- | --- | --- |
| `MinOrderBookSize` | `50` | Seed the book with new orders until this size |
| `BondMapSize` | `100` | Number of generated bonds |
| `AccountMapSize` | `5` | Number of generated accounts |
| `OrderEventsPerSecond` | `10` | Simulated event rate |

`MinOrderBookSize` must be ≤ `AccountMapSize * BondMapSize`.

## Protobuf

Regenerate TypeScript stubs after proto changes:

```bash
cd bff && pnpm install
buf generate
```

C# stubs are generated at build time from `../protos/**/*.proto`.

## React UI (planned)

The UI will connect to the BFF over Socket.IO and show:

- Live order tables
- Live candlestick chart for pricing updates
