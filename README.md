# SMT Management

SMT Management is a .NET 10 production-management platform for organizing manufacturing orders, boards, and electronic components. It combines an ASP.NET Core WebAPI with a Razor Pages user interface and uses SQLite for persistence.

The solution is designed for local development, containerized deployment, and automated delivery through GitHub Actions.

## Capabilities

- Create, read, update, delete, and search orders, boards, and components.
- Model many-to-many relationships:
  - Orders can contain multiple boards.
  - Boards can be used in multiple orders.
  - Boards can contain multiple components.
  - Components can be used on multiple boards.
- Assign boards to orders and components to boards.
- Export an order as a JSON production document for simulated production-line delivery.
- Authenticate Razor Pages users with cookies.
- Authenticate API requests with bearer tokens.
- Change user passwords securely using ASP.NET Core Identity password hashing.
- Check application and SQLite availability through `/health`.
- Run locally with Docker Compose or deploy the container to a cloud platform.

## Technology Stack

- .NET 10
- ASP.NET Core WebAPI
- ASP.NET Core Razor Pages
- SQLite with `Microsoft.Data.Sqlite`
- `System.Text.Json`
- Serilog
- xUnit and Moq
- Docker and Docker Compose
- GitHub Actions
- Optional Azure Container Apps deployment

## Architecture

```text
Razor Pages UI
      |
      v
WebAPI Controllers
      |
      v
Application Services
      |
      v
Repository Interfaces
      |
      v
SQLite Adapter
      |
      v
SQLite Database
```

The solution is separated into focused projects:

| Project | Responsibility |
|---|---|
| `SMTManagement.Contracts` | Domain models, request DTOs, service interfaces, repository interfaces, and persistence abstractions. |
| `SMTManagement.Services` | Application use cases for orders, boards, components, and production export. |
| `SMTManagement.SqliteAdapter` | SQLite connection management, schema initialization, repositories, relationship persistence, and shared SQLite helpers. |
| `SMTManagement.WebAPI` | API controllers, Razor Pages, authentication, middleware, health checks, logging configuration, and dependency-injection composition. |
| `SMTManagement.Tests` | Unit tests for controllers, services, production export, and authentication. |

## Design Principles and Patterns

The solution uses practical object-oriented design principles and established patterns:

- **Dependency Injection** — dependencies are provided through constructors and registered in `Program.cs`.
- **Dependency Inversion** — controllers depend on application service interfaces, and services depend on repository interfaces.
- **Repository Pattern** — persistence operations are isolated behind `IOrderRepository`, `IBoardRepository`, and `IComponentRepository`.
- **Factory Pattern** — `SqliteConnectionFactory` centralizes SQLite connection creation and configuration.
- **Application Service Pattern** — order, board, component, and production workflows are isolated from HTTP concerns.
- **Adapter Pattern** — SQLite-specific implementation is isolated in `SMTManagement.SqliteAdapter`.
- **Middleware/Pipeline Pattern** — global exception handling is implemented by `ExceptionHandlingMiddleware`.
- **Facade Pattern** — `ManagementApiClient` provides a focused client abstraction for Razor Pages API communication.
- **DRY** — shared SQLite mapping, repository helpers, page data loading, and delete-confirmation behavior are centralized.

## Domain Model and Persistence

SQLite stores the following tables:

- `Orders`
- `Boards`
- `Components`
- `Users`
- `OrderBoards`
- `BoardComponents`

`OrderBoards` and `BoardComponents` implement the many-to-many relationships. Foreign keys, indexes, and delete behaviors protect relationship integrity.

The main entities include the required business attributes plus identifiers and timestamps:

- **Order:** `Name`, `Description`, `OrderDate`, `Id`, `CreatedAt`, and related boards.
- **Board:** `Name`, `Description`, `Length`, `Width`, `Id`, `CreatedAt`, and related components.
- **Component:** `Name`, `Description`, `Quantity`, `Id`, and `CreatedAt`.

## API Endpoints

### Orders

```text
GET    /api/orders?search={term}
GET    /api/orders/{id}
POST   /api/orders
PUT    /api/orders/{id}
DELETE /api/orders/{id}
PUT    /api/orders/{id}/boards
GET    /api/orders/{id}/production-export
```

### Boards

```text
GET    /api/boards?search={term}
GET    /api/boards/{id}
POST   /api/boards
PUT    /api/boards/{id}
DELETE /api/boards/{id}
PUT    /api/boards/{id}/components
```

### Components

```text
GET    /api/components?search={term}
GET    /api/components/{id}
POST   /api/components
PUT    /api/components/{id}
DELETE /api/components/{id}
```

The production export endpoint returns a JSON file named `production-order-{id}.json`. The document contains the order, its boards, and their components.

## Authentication and Security

- Razor Pages use cookie authentication and an authorization policy for the UI.
- API endpoints use bearer-token authentication.
- Passwords are hashed using ASP.NET Core Identity password hashing services.
- SQL commands are parameterized.
- Unexpected exceptions are converted into safe problem responses by global middleware.
- Secrets and production credentials should be supplied through environment variables or cloud secret storage.

## Logging and Health Monitoring

Serilog is configured as the application logging framework and integrates with `ILogger<T>`. The application records request activity, authentication events, database operations, entity changes, production exports, cancellations, and exceptions.

The health endpoint is:

```text
GET /health
```

It performs a real SQLite check by opening the configured database connection and executing `SELECT 1`. A successful check returns healthy status; connection or query failures return unhealthy status and are logged.

## Running Locally

### Prerequisites

- .NET 10 SDK
- Docker Desktop, if using containers
- PowerShell, Bash, or an equivalent shell

### Run with .NET

```powershell
dotnet restore SMTManagement.slnx
dotnet build SMTManagement.slnx --configuration Release --no-restore
dotnet test SMTManagement.Tests/SMTManagement.Tests.csproj --configuration Release --no-build --no-restore
```

Run the WebAPI and Razor Pages host with:

```powershell
dotnet run --project SMTManagement.WebAPI/SMTManagement.WebAPI.csproj
```

### Run with Docker Compose

```powershell
docker compose up --build
```

The application is available at:

```text
http://localhost:8080
```

The SQLite database is persisted in the named Docker volume mounted at `/app/data`.

For local development, Docker Compose supports these environment variables:

```text
SMT_API_TOKEN
SMT_ADMIN_USERNAME
SMT_ADMIN_PASSWORD
```

The fallback values in `docker-compose.yml` are intended only for local development and must be overridden in shared or production environments.

## Containerization

The multi-stage `Dockerfile` uses:

- `mcr.microsoft.com/dotnet/sdk:10.0` for restore and publish.
- `mcr.microsoft.com/dotnet/aspnet:10.0` for the runtime image.

The container exposes port `8080` and runs the WebAPI host, which serves both the API and Razor Pages frontend. In cloud deployments, `/app/data` must be backed by persistent storage if SQLite data must survive container replacement.

## CI/CD

GitHub Actions workflows are located in `.github/workflows`.

### Development pipeline

`development.yml` runs for pull requests targeting `develop` and pushes to `develop`. It restores the complete solution, builds it in Release mode, runs unit tests, and publishes the development image:

```text
ghcr.io/<owner>/<repository>/development:latest
```

### Production pipeline

`production.yml` runs only for pushes to `master` or `main`. Pull requests do not publish or overwrite the production image. It restores, builds, tests, and publishes:

```text
ghcr.io/<owner>/<repository>/production:latest
```

Azure Container Apps deployment is optional and disabled by default. Enable it with the GitHub Actions variable:

```text
ENABLE_AZURE_DEPLOYMENT=true
```

When enabled, configure these production environment secrets:

```text
AZURE_CREDENTIALS
AZURE_RESOURCE_GROUP
AZURE_CONTAINER_APP_NAME
GHCR_USERNAME
GHCR_READ_TOKEN
```

The Azure workflow updates an existing Container App. The target application should be configured with port `8080`, persistent storage for `/app/data`, and production secrets managed by Azure.

## Testing

The solution uses xUnit and Moq. The test suite covers:

- Controller responses and not-found behavior.
- Create, update, delete, and relationship operations.
- Production export generation.
- Authentication and password changes.

The current validated test result is:

```text
38 passed
0 failed
0 skipped
```

## Operational Notes

- SQLite is appropriate for local development and small deployments.
- Production cloud deployments should use persistent storage for SQLite or migrate to a managed relational database as scale and concurrency requirements increase.
- Azure deployment assumes that the target Container App already exists.
- Production API tokens, initial credentials, GHCR tokens, and database settings must not be committed to source control.

## Project Status

The solution currently provides a complete .NET 10 application with:

- Razor Pages and WebAPI functionality.
- Layered architecture and interface-based dependencies.
- SQLite persistence and JSON interoperability.
- Structured logging and database health monitoring.
- Automated tests.
- Docker and Docker Compose support.
- Development and production GitHub Actions workflows.
- Optional Azure Container Apps deployment.

