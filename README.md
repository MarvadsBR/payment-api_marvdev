# Payment API

REST API for payment management built with **ASP.NET Core 8**, **Entity Framework Core 8**, **SQL Server 2022** and **Docker**.

> Projeto de portfólio demonstrando boas práticas de desenvolvimento de APIs com .NET.

---

## Tech Stack

| Layer        | Technology                     |
|--------------|--------------------------------|
| Web API      | ASP.NET Core 8                 |
| ORM          | Entity Framework Core 8        |
| Database     | SQL Server 2022                |
| Container    | Docker + Docker Compose        |
| Docs         | Swagger / OpenAPI (Swashbuckle)|

---

## Project Structure

```
payment-api/
├── Controllers/
│   └── PaymentsController.cs   # HTTP endpoints
├── Models/
│   ├── Payment.cs              # Entity
│   ├── PaymentStatus.cs        # Enum: Pending | Completed | Failed | Refunded
│   └── PaymentMethod.cs        # Enum: CreditCard | DebitCard | Pix | BankTransfer
├── Data/
│   └── AppDbContext.cs         # EF Core DbContext
├── Services/
│   ├── IPaymentService.cs      # Interface
│   └── PaymentService.cs       # Business logic
├── DTOs/
│   ├── CreatePaymentDto.cs
│   ├── PaymentResponseDto.cs
│   └── UpdatePaymentStatusDto.cs
├── Program.cs
├── appsettings.json
├── Dockerfile
└── docker-compose.yml
```

---

## Endpoints

| Method   | Route                          | Description                              |
|----------|--------------------------------|------------------------------------------|
| `GET`    | `/api/payments`                | List all payments (filter by `?status=`) |
| `GET`    | `/api/payments/{id}`           | Get payment by ID                        |
| `POST`   | `/api/payments`                | Create a new payment                     |
| `PATCH`  | `/api/payments/{id}/status`    | Update payment status                    |
| `DELETE` | `/api/payments/{id}`           | Delete payment (only if Pending)         |

---

## Running with Docker (recommended)

```bash
docker-compose up --build
```

| Service     | URL                         |
|-------------|-----------------------------|
| Swagger UI  | http://localhost:8080        |
| API base    | http://localhost:8080/api   |
| SQL Server  | localhost:1433               |

> The API auto-creates the database on first startup via `EnsureCreated()`.

---

## Running Locally

**Prerequisites:** .NET 8 SDK, SQL Server (or Docker for SQL only)

```bash
# 1. Start only SQL Server
docker-compose up sqlserver -d

# 2. Restore and run
dotnet restore
dotnet run
```

API will be available at `https://localhost:7xxx` (port shown in terminal).

---

## Example Requests

### Create a payment
```http
POST /api/payments
Content-Type: application/json

{
  "amount": 150.00,
  "currency": "BRL",
  "method": "Pix",
  "description": "Order #1042",
  "externalReference": "ORD-1042"
}
```

### Update status
```http
PATCH /api/payments/{id}/status
Content-Type: application/json

{
  "status": "Completed"
}
```

### Filter by status
```http
GET /api/payments?status=Pending
```

---

## Design Decisions

- **Service layer** separates business rules from HTTP concerns.
- **DTOs** prevent over-posting and decouple the API contract from the data model.
- **Enum → string** stored in DB for readability over the wire and in queries.
- **`DeleteResult` enum** allows the controller to return precise HTTP codes (404 vs 409) without exceptions.
- **`EnsureCreated()`** keeps the demo zero-config; swap for `Migrate()` + migrations in production.

---

## License

MIT
