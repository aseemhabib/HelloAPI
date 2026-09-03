# AI Coding Agent Prompt

Use the following prompt with a coding agent from the repository root.

---

You are implementing a deliberately small system-design learning project.

Read `README.md`, `PROJECT_SPEC.md`, `IMPLEMENTATION_PLAN.md`, and `LEARNING_NOTES.md` before making changes.

## Goal

Create a local Docker Compose solution containing:

1. One ASP.NET Core Minimal API application.
2. Two running containers of that same API image: `api1` and `api2`.
3. One NGINX container acting as a reverse proxy and round-robin load balancer.
4. No frontend and no database.

## API

Implement:

```http
GET /api/hello
```

Return JSON similar to:

```json
{
  "message": "Hello from api-1",
  "instance": "api-1",
  "machineName": "...",
  "timestampUtc": "..."
}
```

Read the instance name from:

```text
INSTANCE_NAME
```

Do not hard-code separate application code for each instance.

## Containers

Both API services must use the same Dockerfile and same image/build context.

Both APIs should listen internally on:

```text
8080
```

Host mappings:

```text
api1: localhost:5001 -> 8080
api2: localhost:5002 -> 8080
```

Configure:

```text
api1 INSTANCE_NAME=api-1
api2 INSTANCE_NAME=api-2
```

## NGINX

Run NGINX in Docker.

Expose:

```text
localhost:8080 -> nginx:80
```

Configure an upstream using Docker DNS names:

```text
api1:8080
api2:8080
```

Use NGINX's default round-robin algorithm.

Do not use hard-coded container IP addresses.

Forward standard proxy headers where appropriate.

## Files expected

Create or complete:

```text
src/HelloApi/HelloApi.csproj
src/HelloApi/Program.cs
src/HelloApi/Dockerfile
nginx/nginx.conf
docker-compose.yml
.dockerignore
.gitignore
```

Keep the implementation simple and readable.

## Constraints

Do NOT add:

- React
- Razor
- database
- Entity Framework
- authentication
- Redis
- queues
- Kubernetes
- cloud resources
- unnecessary NuGet packages

This is a learning lab, not a production platform.

## Verification

Before declaring completion, verify the intended configuration supports:

```bash
docker compose up --build
```

and these calls:

```text
http://localhost:5001/api/hello
http://localhost:5002/api/hello
http://localhost:8080/api/hello
```

Repeated requests to port `8080` should reach both API instances.

Explain each file you create and, especially, explain:

1. Why both API containers can listen on port 8080.
2. Why their host ports are different.
3. Why NGINX uses `api1:8080` and `api2:8080`, rather than `localhost:5001` and `localhost:5002`.
4. Where round robin is configured (including the fact that it is NGINX's default).
5. Why both API instances should be stateless.

Do not introduce complexity beyond the specification.
