# NGINX + ASP.NET Core Load Balancer Lab

## Purpose

This is a deliberately small system-design lab. The goal is to see, locally and visibly, how an HTTP request can enter through one endpoint and be distributed by an NGINX load balancer across multiple instances of the same ASP.NET Core API.

There is no UI, database, authentication, queue, cache, or cloud infrastructure in Phase 1.

## Architecture

```text
Browser / curl
      |
      | GET http://localhost:8080/api/hello
      v
+----------------------+
| NGINX                |
| Reverse Proxy / LB   |
| host 8080 -> port 80 |
+----------+-----------+
           |
           | default round robin
      +----+----+
      |         |
      v         v
+-----------+ +-----------+
| api1      | | api2      |
| ASP.NET   | | ASP.NET   |
| Core API  | | Core API  |
| :8080     | | :8080     |
+-----------+ +-----------+
```

Optional host mappings:

```text
localhost:5001 -> api1:8080
localhost:5002 -> api2:8080
```

Those mappings make it easy to test each API directly.

## Important Docker Concept

Both ASP.NET containers can listen on **port 8080 internally**.

They do not conflict because each container has its own network namespace.

Docker Compose can expose them to different host ports:

```text
5001:8080
5002:8080
```

NGINX should communicate with them over the Docker network using service names:

```text
api1:8080
api2:8080
```

It should not use the Windows host ports for container-to-container traffic.

## Suggested Technology

- VS Code
- C#
- ASP.NET Core Minimal API
- .NET 10
- Docker Desktop
- Docker Compose
- NGINX Open Source

Minimal APIs are ideal here because the API code should remain tiny; the main learning goal is infrastructure and request routing.

## API Contract

### Request

```http
GET /api/hello
```

### Response

Example:

```json
{
  "message": "Hello from api-1",
  "instance": "api-1",
  "machineName": "f7321a4c97ab",
  "timestampUtc": "2026-08-28T01:00:00Z"
}
```

`instance` should come from an environment variable such as:

```text
INSTANCE_NAME=api-1
```

Both containers must run the **same Docker image**. Do not create two different API projects.

## Expected Test

Start the solution:

```bash
docker compose up --build
```

Repeatedly request:

```text
http://localhost:8080/api/hello
```

You should see responses similar to:

```text
Hello from api-1
Hello from api-2
Hello from api-1
Hello from api-2
```

NGINX uses round robin by default when an upstream contains multiple servers.

## Direct Tests

When host ports are exposed, verify the API instances independently:

```text
http://localhost:5001/api/hello
http://localhost:5002/api/hello
```

Then test the real entry point:

```text
http://localhost:8080/api/hello
```

## Suggested Folder Structure

```text
nginx-aspnet-load-balancer-lab/
│
├── src/
│   └── HelloApi/
│       ├── HelloApi.csproj
│       ├── Program.cs
│       └── Dockerfile
│
├── nginx/
│   └── nginx.conf
│
├── docker-compose.yml
├── .dockerignore
├── .gitignore
├── README.md
├── PROJECT_SPEC.md
├── IMPLEMENTATION_PLAN.md
├── AI_CODING_PROMPT.md
└── LEARNING_NOTES.md
```

## Conceptual NGINX Configuration

```nginx
events {}

http {
    upstream hello_api {
        server api1:8080;
        server api2:8080;
    }

    server {
        listen 80;

        location / {
            proxy_pass http://hello_api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
```

No load-balancing algorithm is specified because NGINX's default is round robin.

## Conceptual Docker Compose

```yaml
services:
  api1:
    build:
      context: ./src/HelloApi
    environment:
      INSTANCE_NAME: api-1
      ASPNETCORE_HTTP_PORTS: 8080
    ports:
      - "5001:8080"

  api2:
    build:
      context: ./src/HelloApi
    environment:
      INSTANCE_NAME: api-2
      ASPNETCORE_HTTP_PORTS: 8080
    ports:
      - "5002:8080"

  nginx:
    image: nginx:alpine
    ports:
      - "8080:80"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - api1
      - api2
```

## What Success Teaches You

You will have demonstrated:

1. Horizontal application instances.
2. Stateless HTTP services.
3. Reverse proxying.
4. Layer-7 HTTP load balancing.
5. Round-robin routing.
6. Docker images versus containers.
7. Internal container ports versus host ports.
8. Docker DNS/service discovery.
9. Infrastructure configuration as code.
10. A single public entry point hiding multiple backend instances.

That is a very useful miniature version of concepts that appear in larger distributed systems.
