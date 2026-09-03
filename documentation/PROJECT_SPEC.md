# Project Specification

## Project Name

NGINX ASP.NET Core Local Load-Balancing Lab

## Objective

Create a local containerised environment in which an NGINX reverse proxy receives HTTP requests and distributes them across two independently running instances of one ASP.NET Core API.

The project exists for system-design learning, not production use.

## Functional Requirements

### FR-1: API endpoint

The application must expose:

```http
GET /api/hello
```

### FR-2: Instance identification

The response must clearly identify which API instance served the request.

The identity must be configurable at runtime through an environment variable called:

```text
INSTANCE_NAME
```

### FR-3: Same application image

Both API containers must run the same application and Docker image.

There must not be separate source-code projects for api1 and api2.

### FR-4: Two instances

Docker Compose must run two API services:

- `api1`
- `api2`

Each service should listen internally on port `8080`.

### FR-5: Direct local access

For learning/debugging, expose:

- `api1` as `localhost:5001`
- `api2` as `localhost:5002`

### FR-6: NGINX

Run NGINX in its own container.

Expose it as:

```text
localhost:8080
```

### FR-7: Round robin

NGINX must define both API services as upstream servers and use its default round-robin behaviour.

### FR-8: Docker service discovery

NGINX must address the APIs by Docker Compose service name:

```text
api1:8080
api2:8080
```

Do not use hard-coded container IP addresses.

## Non-Functional Requirements

### Simplicity

The project should contain the minimum code needed to make the infrastructure behaviour obvious.

### Repeatability

A developer should be able to launch the whole solution with:

```bash
docker compose up --build
```

### Observability

The response should make the serving instance obvious.

Recommended response:

```json
{
  "message": "Hello from api-1",
  "instance": "api-1",
  "machineName": "...",
  "timestampUtc": "..."
}
```

Console logs should also include the instance name.

### No unnecessary dependencies

Do not add:

- database
- frontend
- authentication
- Redis
- message queue
- Kubernetes
- cloud services

Those are outside Phase 1.

## Technical Constraints

- Language: C#
- Runtime/framework: current supported .NET / ASP.NET Core, preferably .NET 10
- API style: ASP.NET Core Minimal API
- Container runtime: Docker Desktop
- Orchestration: Docker Compose
- Proxy/load balancer: NGINX Open Source
- Editor: VS Code

## Acceptance Criteria

The lab is complete when all of the following are true:

- [ ] `docker compose up --build` starts three containers.
- [ ] `http://localhost:5001/api/hello` returns `api-1`.
- [ ] `http://localhost:5002/api/hello` returns `api-2`.
- [ ] `http://localhost:8080/api/hello` returns a valid response.
- [ ] Repeated calls through port 8080 are served by both API instances.
- [ ] NGINX uses Docker service names rather than container IPs.
- [ ] Both API containers use the same image/build definition.
- [ ] No UI or database exists in Phase 1.

## Out of Scope

The following should deliberately be deferred:

- HTTPS
- authentication/authorization
- sticky sessions
- health-check-aware routing
- retries/circuit breakers
- rate limiting
- autoscaling
- Kubernetes
- service mesh
- cloud deployment
- monitoring platform
- frontend

These can become later learning exercises.
