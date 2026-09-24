# AGENTS.md

## Purpose

This repository is both a learning project and the foundation for a larger future application.

The user is deliberately building the system in stages to understand how the underlying technologies work, rather than only generating code that happens to run.

When working in this repository:

- Explain important framework behavior when it is not obvious.
- Prefer simple, explicit implementations first.
- Do not introduce unnecessary abstractions or dependencies.
- Preserve the current architecture unless there is a clear reason to change it.
- When suggesting a more advanced solution, explain why it is better and what trade-offs it introduces.
- Prefer small, testable changes over large rewrites.
- Before changing architecture, explain the impact on the existing design.

## Learning goals

The user wants a strong practical understanding of:

- ASP.NET Core
- Dependency injection
- Minimal APIs and controllers
- Repository pattern
- PostgreSQL
- SQL and database behavior
- React
- JavaScript / TypeScript fundamentals
- Docker
- Nginx
- Kubernetes
- Traefik
- System design
- Testing

The goal is not merely to complete features. The user should understand why the solution works.

## Current application direction

The repository is evolving toward a larger Engineering Manager application.

The current development work focuses on establishing the core application architecture first.

Current major pieces include:

- React frontend
- ASP.NET Core backend API
- PostgreSQL database
- Repository pattern for database access
- Docker-based local development
- Kubernetes experiments
- Traefik ingress

Earlier infrastructure experiments also used Nginx for load balancing.

## Current database approach

PostgreSQL is being used.

A `users` table has already been created.

For the current learning phase:

- Prefer direct PostgreSQL access through the repository layer.
- Do not introduce Entity Framework unless there is a clear reason to do so.
- Keep SQL explicit where practical.
- Parameterize all SQL.
- Keep database access out of API endpoint/controller code.

A future migration to EF Core is acceptable, but it should be treated as a deliberate architectural decision rather than an automatic default.

## Repository pattern

Business/API code should depend on an abstraction such as:

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(...);
    Task<User?> GetByEmailAsync(...);
    Task AddAsync(...);
}
```

A PostgreSQL implementation should live behind that interface.

ASP.NET dependency injection should construct and supply repository implementations.

Do not manually instantiate repositories inside endpoints or controllers unless specifically demonstrating why that is undesirable.

## ASP.NET dependency injection

The user has specifically been learning how ASP.NET infers endpoint parameters.

For Minimal APIs, parameters may come from different sources automatically.

For example:

```csharp
app.MapPost("/users",
    async (
        CreateUserRequest request,
        IUserRepository repository,
        CancellationToken cancellationToken) =>
    {
        ...
    });
```

Important distinction:

- `request` may be bound from the HTTP request body.
- `repository` is resolved from ASP.NET Core dependency injection.
- `cancellationToken` is supplied by the framework.

The React client does not send the repository or cancellation token.

When explaining APIs, make this distinction clear.

For controllers, similar framework behavior occurs through:

- model binding
- dependency injection
- controller construction
- action parameter binding

A plain C# class does not receive this behavior automatically.

## Frontend direction

React is being used for the frontend.

The user is also deliberately strengthening JavaScript and TypeScript fundamentals.

When producing frontend code:

- Prefer TypeScript where practical.
- Keep components small.
- Avoid introducing large state-management libraries without a real need.
- Explain asynchronous behavior, promises, fetch, state, props and hooks when relevant.
- Prefer understandable React over overly clever React.

The immediate frontend feature is user sign-in / user-related functionality.

## API direction

The immediate backend work includes:

1. Verify API behavior.
2. Add user endpoints.
3. Connect user endpoints to PostgreSQL.
4. Implement repository-based persistence.
5. Add tests.
6. Connect React to the API.

When adding endpoints, maintain separation between:

- transport / HTTP concerns
- application logic
- persistence

Do not place SQL directly inside React code or endpoint definitions.

## Testing direction

Tests should be added progressively.

Prefer a layered approach:

### Unit tests
For logic that does not need ASP.NET hosting or a real database.

### Repository integration tests
For verifying PostgreSQL behavior against a test database or container.

### API integration tests
For verifying HTTP endpoints through ASP.NET test hosting.

Do not test ASP.NET Minimal API handlers merely by manually calling generated endpoint delegates if that bypasses important framework behavior.

When the goal is to test routing, dependency injection, serialization, validation or HTTP status codes, prefer API integration tests.

## Docker history

The project previously used Docker to run:

- Nginx
- ASP.NET API instance 1
- ASP.NET API instance 2

Nginx performed round-robin load balancing between the two API containers.

This was successfully tested.

Docker Compose was then used to manage the services together.

This work was primarily a learning exercise for:

- containers
- port mapping
- service discovery
- reverse proxies
- load balancing

Do not assume Nginx remains required in the final application architecture.

## Kubernetes history

The project later moved into local Kubernetes experimentation.

Traefik ingress has been installed using Helm and routing has been verified.

Planned or ongoing Kubernetes learning work includes:

- Gateway API with Traefik
- liveness probes
- readiness probes
- Horizontal Pod Autoscaling (HPA)
- scaling multiple API instances

Expected health endpoints:

```text
/health/live
/health/ready
```

When working on Kubernetes files, explain the relationship between:

- Deployment
- Pod
- Service
- Ingress / Gateway
- replicas
- probes
- autoscaling

## PostgreSQL learning context

The user has been learning PostgreSQL internals and how they differ from SQL Server.

Topics already discussed include:

- PostgreSQL connection processes vs SQL Server worker/thread architecture
- WAL
- transaction durability
- memory vs disk writes
- MVCC
- tuples / row versions
- transaction isolation
- differences between PostgreSQL and SQL Server
- ways to connect to PostgreSQL
- psql
- VS Code PostgreSQL tools

PostgreSQL is currently running in Docker, which is why `psql` may not exist directly on the host machine unless PostgreSQL client tools are installed locally.

## Code style

Prefer:

- clear names
- explicit control flow
- small methods
- constructor injection where relevant
- async database/API code
- cancellation token propagation
- parameterized queries
- useful errors
- testable classes

Avoid:

- unnecessary patterns
- premature microservices
- excessive generic abstractions
- hidden framework magic without explanation
- large generated code dumps without context

## When proposing changes

For any meaningful architectural change, explain:

1. What problem it solves.
2. Why the current approach is insufficient.
3. What new complexity it introduces.
4. Whether the change is necessary now or can wait.

Prefer learning value and maintainability over novelty.

## Current priority

Unless the repository indicates otherwise, the current practical priority is:

1. Build user/sign-in functionality.
2. Verify the API works.
3. Implement PostgreSQL persistence using the repository pattern.
4. Add appropriate tests.
5. Connect the React frontend.
6. Continue infrastructure learning after the application path works end-to-end.
