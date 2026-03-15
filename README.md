# Order Processing Microservice

A distributed order processing system built with .NET 10, demonstrating the **Saga Orchestration Pattern**, **CQRS**, **Event Sourcing**, and **message-driven architecture** using MassTransit with dual message broker support (RabbitMQ & Azure Service Bus).

---

## Architecture Overview

```
┌──────────────┐     ┌───────────────────────────────────────────────────────────┐
│  REST API    │     │              Message Broker (RabbitMQ / Azure SB)         │
│  Controllers │────▶│                                                           │
└──────────────┘     │  submit-order ─▶ process-payment ─▶ reserve-inventory     │
                     │       │                │                    │              │
                     │       ▼                ▼                    ▼              │
                     │  SubmitOrder     ProcessPayment     ReserveInventory       │
                     │  Consumer        Consumer           Consumer              │
                     │       │                │                    │              │
                     │       ▼                ▼                    ▼              │
                     │            ┌──────────────────────┐                       │
                     │            │    Order Saga         │                       │
                     │            │   (State Machine)     │                       │
                     │            └──────────┬───────────┘                       │
                     │                       │                                    │
                     │              ┌────────┴────────┐                          │
                     │              ▼                  ▼                          │
                     │      order-completed     order-faulted                    │
                     └───────────────────────────────────────────────────────────┘
                                        │
                     ┌──────────────────┼──────────────────┐
                     ▼                  ▼                   ▼
              ┌────────────┐   ┌──────────────┐   ┌──────────────┐
              │ SQL Server │   │  Event Store  │   │  Saga State  │
              │  (EF Core) │   │  (Stored      │   │  (EF Core    │
              │            │   │   Events)     │   │   Persisted) │
              └────────────┘   └──────────────┘   └──────────────┘
```

## Technology Stack

| Component | Technology | Version |
|---|---|---|
| Framework | .NET / ASP.NET Core | 10.0 |
| Message Broker | MassTransit (RabbitMQ + Azure Service Bus) | 9.0.1 |
| ORM | Entity Framework Core (SQL Server) | 10.0.3 |
| API Docs | Swagger UI (Swashbuckle) | 10.1.5 |
| Auth (optional) | Azure Identity + Key Vault | 1.19.0 |

---

## Project Structure

```
MicroserviceSample/
├── Controllers/            # REST API endpoints
│   ├── OrdersController    #   Submit orders, query saga state & events
│   ├── CustomersController #   Customer CRUD
│   ├── ProductsController  #   Product CRUD
│   ├── PaymentsController  #   Payment queries (read-only)
│   ├── InventoriesController # Inventory CRUD + quantity adjustment
│   └── EventsController    #   Event store queries
│
├── Contracts/              # Shared message contracts
│   ├── Commands/           #   SubmitOrderCommand, ProcessPaymentCommand,
│   │                       #   ReserveInventoryCommand
│   └── Events/             #   OrderSubmitted, PaymentProcessed,
│                           #   InventoryReserved, OrderCompleted, OrderFaulted
│
├── Consumers/              # MassTransit message consumers
│   ├── SubmitOrderConsumer       # Creates order, stores event, forwards to saga
│   ├── ProcessPaymentConsumer    # Processes payment, publishes result
│   ├── ReserveInventoryConsumer  # Reserves inventory, publishes result
│   ├── OrderCompletedConsumer    # Marks order completed, stores event
│   └── OrderFaultedConsumer      # Marks order faulted, stores event
│
├── Sagas/                  # MassTransit Saga State Machine
│   ├── OrderSaga           #   Orchestrates: Payment → Inventory → Complete
│   ├── OrderSagaState      #   Saga instance state entity
│   └── OrderSagaMap        #   EF Core entity configuration
│
├── CQRS/                   # Command/Query Responsibility Segregation
│   ├── Commands/
│   │   └── OrderCommandHandler   # Create order command
│   └── Queries/
│       └── OrderQueryHandler     # Order queries (by ID, all, by customer)
│
├── Domain/                 # Domain entities
│   ├── Order, OrderItem, OrderStatus
│   ├── Customer, Product, Payment, Inventory
│   └── StoredEvent         #   Event sourcing persistence model
│
├── Infrastructure/
│   ├── Data/
│   │   └── ApplicationDbContext  # EF Core DbContext with seed data
│   ├── EventStore/
│   │   └── EventStore            # JSON event serialization & storage
│   └── Messaging/
│       └── QueueNames            # Queue name constants
│
├── Migrations/             # EF Core migrations
├── Scripts/
│   └── setup-azure-servicebus-queues.ps1  # Azure SB queue provisioning
└── Program.cs              # Application bootstrap & MassTransit config
```

---

## Order Saga Flow

The saga orchestrates the order lifecycle through a state machine with the following transitions:

```
                    ┌─────────────────┐
                    │    Initial      │
                    └────────┬────────┘
                             │ OrderSubmitted
                             ▼
                 ┌───────────────────────┐
                 │   PaymentPending      │──── sends ProcessPaymentCommand
                 └───────────┬───────────┘
                    ┌────────┴────────┐
                    │                 │
              Payment OK        Payment Failed
                    │                 │
                    ▼                 ▼
        ┌───────────────────┐  ┌───────────┐
        │ InventoryPending  │  │  Faulted  │
        └───────────┬───────┘  └───────────┘
           ┌────────┴────────┐
           │                 │
      Reserved OK      Reserve Failed
           │                 │
           ▼                 ▼
     ┌───────────┐    ┌───────────┐
     │ Completed │    │  Faulted  │
     └───────────┘    └───────────┘
```

### Saga States

| State | Description |
|---|---|
| `Initial` | Order submitted, saga instance created |
| `PaymentPending` | Waiting for payment processing result |
| `InventoryPending` | Payment succeeded, waiting for inventory reservation |
| `Completed` | All steps succeeded, order finalized |
| `Faulted` | Payment or inventory failed, order marked as faulted |

### Message Queues

| Queue | Consumer | Purpose |
|---|---|---|
| `submit-order` | `SubmitOrderConsumer` | Creates order in DB, fires `OrderSubmittedEvent` |
| `process-payment` | `ProcessPaymentConsumer` | Processes payment, fires `PaymentProcessedEvent` |
| `reserve-inventory` | `ReserveInventoryConsumer` | Reserves stock, fires `InventoryReservedEvent` |
| `order-completed` | `OrderCompletedConsumer` | Marks order as completed |
| `order-faulted` | `OrderFaultedConsumer` | Marks order as faulted |
| `order-saga` | `OrderSaga` (state machine) | Saga orchestration endpoint |

---

## API Endpoints

### Orders

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/orders` | List all orders |
| `GET` | `/api/orders/{id}` | Get order by ID (includes customer, items, products) |
| `GET` | `/api/orders/customer/{customerId}` | Get orders for a customer |
| `POST` | `/api/orders` | Submit a new order (triggers saga) |
| `GET` | `/api/orders/saga/{correlationId}` | Get saga state for an order |
| `GET` | `/api/orders/events/{correlationId}` | Get all stored events for an order |

### Supporting Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET/POST/PUT/DELETE` | `/api/customers` | Customer CRUD |
| `GET/POST/PUT/DELETE` | `/api/products` | Product CRUD |
| `GET` | `/api/payments` | Query payments (by ID, by order) |
| `GET/POST/PUT/DELETE/PATCH` | `/api/inventories` | Inventory CRUD + quantity adjustment |
| `GET` | `/api/events` | Query event store (by correlation ID, by type) |

### Submit Order Request

```json
POST /api/orders
{
  "customerId": 1,
  "customerName": "Acme Corp",
  "items": [
    { "productId": 1, "quantity": 2, "unitPrice": 29.99 },
    { "productId": 3, "quantity": 1, "unitPrice": 49.99 }
  ]
}
```

**Response** (202 Accepted):
```json
{
  "correlationId": "a1b2c3d4-...",
  "message": "Order accepted. Processing via saga: Payment → Inventory → Complete."
}
```

---

## Event Store

Every significant step in the order lifecycle is persisted as a `StoredEvent`:

| Field | Type | Description |
|---|---|---|
| `Id` | `long` | Auto-increment primary key |
| `CorrelationId` | `Guid` | Links all events for a single order |
| `EventType` | `string` | Event class name (e.g., `OrderSubmittedEvent`) |
| `Payload` | `string` | JSON-serialized event data |
| `Timestamp` | `DateTime` | UTC timestamp |

Query events via `GET /api/orders/events/{correlationId}` or `GET /api/events`.

---

## Domain Model

```
Customer (1) ──── (*) Order (1) ──── (*) OrderItem (*) ──── (1) Product
                       │
                       └──── (1) Payment
                       
Product (1) ──── (*) Inventory
```

### OrderStatus Enum

```
Submitted → PaymentPending → PaymentCompleted → InventoryReserved → Completed
                                                                  → Faulted
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB or full instance)
- **One of:**
  - [RabbitMQ](https://www.rabbitmq.com/) (local development)
  - Azure Service Bus namespace (Basic tier or higher)

### Configuration

Edit `appsettings.json` to choose your message broker:

```json
{
  "MessageBroker": {
    "Provider": "RabbitMQ"          // or "AzureServiceBus"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "AzureServiceBus": {
    "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;..."
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OrderProcessingDb;..."
  }
}
```

### Run

```bash
# Restore and run
dotnet run --project MicroserviceSample

# The database is auto-migrated on startup with seed data:
# - 3 customers, 5 products, 5 inventory records, 2 sample orders
```

Swagger UI is available at: **https://localhost:7266/swagger**

### Azure Service Bus Setup

If using Azure Service Bus (Basic tier), queues must be pre-created:

```powershell
# Create all required queues
.\MicroserviceSample\Scripts\setup-azure-servicebus-queues.ps1
```

The app also validates at startup that all required queues exist.

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| **Saga Orchestration** (not Choreography) | Centralized control over the order workflow; easier to reason about failure paths |
| **Dual Broker Support** | RabbitMQ for local dev, Azure Service Bus for production — same code, config-driven |
| **EF Core Saga Persistence** | Saga state stored in SQL Server alongside domain data; pessimistic concurrency |
| **Event Store** | All domain events serialized to JSON and stored for audit trail and replay |
| **CQRS** | Separates read and write concerns for orders; queries bypass command handlers |
| **Azure SB Basic Tier** | Queues only (no topics); `ConfigureConsumeTopology = false` + dead letter queue routing |
| **Auto-Migration** | Database migrated automatically on startup for simplified deployment |

---

## Seed Data

The database is seeded with sample data on first migration:

- **3 Customers**: Acme Corp, Globex Corp, Initech
- **5 Products**: Widget A-E ($10.99–$59.99)
- **5 Inventory Records**: 100 units each
- **2 Sample Orders**: With order items and payments

---

## License

This project is for demonstration and learning purposes.
