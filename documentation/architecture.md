# Architecture

## Overview

This repository is an incremental full-stack learning project that is expected to evolve into a larger Engineering Manager application.

The system is intentionally being built in stages so that each architectural layer can be understood independently before additional complexity is introduced.

The application currently centers around three primary layers:

```text
React frontend
      |
      v
ASP.NET Core API
      |
      v
Repository layer
      |
      v
PostgreSQL
```

Infrastructure experiments using Docker, Nginx, Kubernetes and Traefik support the application but are not intended to dictate the application architecture unnecessarily.

---

# 1. Frontend

## Technology

React, ideally with TypeScript.

## Responsibilities

The frontend should be responsible for:

- rendering UI
- collecting user input
- calling backend HTTP endpoints
- displaying loading/error/success states
- maintaining client-side state where appropriate

The frontend should not:

- connect directly to PostgreSQL
- contain SQL
- know how repositories are implemented
- contain server-side secrets

## Immediate feature direction

User and sign-in functionality.

A typical request path should look like:

```text
React form
    |
    | HTTP request
    v
ASP.NET endpoint
    |
    v
Application / repository logic
    |
    v
PostgreSQL
```

---

# 2. Backend API

## Technology

ASP.NET Core.

The project has been exploring Minimal APIs, while also comparing them conceptually with controllers.

## Responsibilities

The API layer should handle:

- HTTP routing
- request deserialization
- input validation
- authentication/authorization when added
- status codes
- response serialization

It should delegate persistence to repositories rather than containing SQL directly.

Example conceptual endpoint:

```csharp
app.MapPost("/users",
    async (
        CreateUserRequest request,
        IUserRepository repository,
        CancellationToken cancellationToken) =>
    {
        var user = await repository.CreateAsync(
            request,
            cancellationToken);

        return Results.Created($"/users/{user.Id}", user);
    });
```

In this example ASP.NET supplies different parameters through different mechanisms:

```text
CreateUserRequest
    <- HTTP request body / model binding

IUserRepository
    <- dependency injection container

CancellationToken
    <- ASP.NET request lifecycle
```

Only the request data originates from the React client.

---

# 3. Dependency Injection

ASP.NET Core's dependency injection container is part of the application's composition layer.

Conceptually:

```text
Program.cs
   |
   | registers
   v
IUserRepository -> PostgresUserRepository
```

Then an endpoint or controller can depend on `IUserRepository` instead of constructing a database implementation itself.

Example:

```csharp
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
```

This gives the application:

- lower coupling
- easier testing
- replaceable persistence implementations
- centralized construction of dependencies

A plain C# class outside ASP.NET does not automatically get this behavior.

---

# 4. Repository Layer

## Purpose

The repository isolates PostgreSQL access from HTTP/API code.

Conceptually:

```text
API
 |
 v
IUserRepository
 |
 v
PostgresUserRepository
 |
 v
PostgreSQL
```

A likely interface is:

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<User> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken);
}
```

The concrete implementation can use a PostgreSQL driver such as Npgsql.

## Why direct SQL initially

The project is intentionally beginning without Entity Framework Core.

This allows direct learning of:

- SQL
- connections
- commands
- parameters
- transactions
- database errors
- mapping query results into C# objects

Entity Framework may be introduced later if its productivity benefits justify the abstraction.

---

# 5. PostgreSQL

## Current state

A PostgreSQL database exists and currently includes a `users` table.

PostgreSQL is running in Docker.

This means PostgreSQL server tools installed inside the container are not automatically installed on the host operating system.

For example:

```text
psql inside container != psql available in Windows terminal
```

To use `psql` directly from the host, PostgreSQL client tools would need to be installed locally, or `psql` can be run inside the PostgreSQL container.

## Important PostgreSQL concepts already explored

### WAL

PostgreSQL uses a write-ahead log.

A simplified write flow is:

```text
transaction changes
       |
       v
shared memory / buffers
       |
       +----> WAL records
       |
       v
data pages eventually written to disk
```

Durability depends on the WAL being persisted appropriately before a committed transaction is considered durable.

### MVCC

PostgreSQL uses Multi-Version Concurrency Control.

Updates generally create new tuple versions rather than simply overwriting the existing row in place.

A query sees row versions according to its transaction snapshot and isolation rules.

This is different from thinking of a query as simply reading "whatever value is currently sitting in memory."

---

# 6. Docker

Docker was used as the first infrastructure learning stage.

The architecture used:

```text
                +--> ASP.NET API 1
Browser -> Nginx
                +--> ASP.NET API 2
```

Nginx performed round-robin load balancing between the two API instances.

Docker Compose was later used to define the services together.

This demonstrated:

- images
- containers
- container networking
- ports
- reverse proxies
- service names
- load balancing

The Nginx setup was successful.

It should be considered a learning stage rather than a permanent architectural requirement.

---

# 7. Kubernetes

The project later progressed to Kubernetes.

## Current state

Traefik ingress has been installed using Helm.

Ingress routing has been verified.

## Next learning targets

### Gateway API

Explore Kubernetes Gateway API with Traefik.

Conceptually:

```text
Client
  |
Gateway
  |
Route
  |
Service
  |
Pods
```

### Health probes

Planned endpoints:

```text
/health/live
/health/ready
```

Liveness answers:

```text
Should Kubernetes restart this container?
```

Readiness answers:

```text
Should Kubernetes currently send traffic to this pod?
```

These are intentionally different questions.

### Horizontal Pod Autoscaler

Planned learning area:

```text
traffic / CPU load increases
          |
          v
Horizontal Pod Autoscaler
          |
          v
Deployment replicas increase
          |
          v
more Pods
```

The goal is to understand how Kubernetes replaces the manual idea of running multiple API containers with declarative scaling and service discovery.

---

# 8. Testing Strategy

Testing should be introduced progressively.

## Unit tests

Use for isolated application logic.

These should not require:

- HTTP
- ASP.NET hosting
- a real PostgreSQL instance

## Repository integration tests

Use when verifying:

- SQL syntax
- PostgreSQL behavior
- data mapping
- constraints
- transactions

These tests should ideally run against an isolated test PostgreSQL database/container.

## API integration tests

Use ASP.NET integration test infrastructure when testing:

- routes
- dependency injection
- JSON binding
- validation
- HTTP responses
- middleware
- status codes

Conceptually:

```text
Test
 |
 v
HTTP request
 |
 v
ASP.NET test server
 |
 v
Endpoint
 |
 v
Repository/test dependency
```

Calling endpoint logic directly can be useful for pure functions, but it does not verify ASP.NET's routing, binding or dependency-injection behavior.

---

# 9. Authentication Direction

The application is moving toward sign-in functionality.

Authentication should be added deliberately after the basic user/API/database path works.

Likely stages:

```text
1. Create/read users
2. Verify persistence
3. Add password handling
4. Add authentication endpoint
5. Add authentication mechanism
6. Protect endpoints
7. Add authorization rules
```

Passwords must never be stored in plaintext.

When authentication is implemented, use established ASP.NET security mechanisms and appropriate password hashing rather than custom cryptography.

---

# 10. Current Development Sequence

Recommended order:

```text
1. Confirm ASP.NET API works
        |
        v
2. Create user API contract
        |
        v
3. Implement IUserRepository
        |
        v
4. Implement PostgreSQL repository
        |
        v
5. Test repository
        |
        v
6. Test API
        |
        v
7. Build React sign-in/user UI
        |
        v
8. Connect React to API
        |
        v
9. Add authentication properly
        |
        v
10. Continue Kubernetes/scaling work
```

This keeps infrastructure learning from obscuring the application flow.

---

# 11. Architectural Principles

The project should currently favor:

- modularity
- understandable code
- explicit SQL
- dependency injection
- separation of concerns
- testability
- gradual complexity

Avoid introducing advanced architecture simply because it is common in enterprise applications.

In particular, do not introduce prematurely:

- microservices
- message brokers
- CQRS
- event sourcing
- complex domain frameworks
- service meshes
- distributed caching

Those technologies can be explored later when the application has a concrete requirement for them.

---

# 12. Long-Term Direction

The likely evolution is:

```text
Current learning project
        |
        v
Full-stack application
        |
        v
Engineering Manager application
        |
        +--> authentication
        +--> users
        +--> teams
        +--> projects
        +--> engineering metrics
        +--> management workflows
        +--> reporting
```

The architecture should evolve in response to real requirements rather than attempting to predict the final system in advance.
