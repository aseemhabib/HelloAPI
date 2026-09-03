# Implementation Plan

## Step 0 - Install/check prerequisites

On Windows, install:

1. VS Code
2. Docker Desktop
3. .NET SDK 10
4. Recommended VS Code C# tooling

Verify from the VS Code terminal:

```bash
dotnet --version
docker --version
docker compose version
```

## Step 1 - Create the API

Create the folder structure:

```bash
mkdir nginx-aspnet-load-balancer-lab
cd nginx-aspnet-load-balancer-lab
mkdir src
cd src
dotnet new web -n HelloApi
```

Open the root project folder in VS Code.

The API should expose:

```http
GET /api/hello
```

It should read:

```text
INSTANCE_NAME
```

from the environment.

If it is missing, use a sensible local fallback such as `local`.

Test without Docker first:

```bash
dotnet run --project src/HelloApi
```

## Step 2 - Containerise the API

Create:

```text
src/HelloApi/Dockerfile
```

Use a standard multi-stage .NET Docker build:

1. SDK image for restore/build/publish.
2. ASP.NET runtime image for execution.
3. Expose/listen on port 8080.

Build the image manually once if desired:

```bash
docker build -t hello-api ./src/HelloApi
```

## Step 3 - Create Docker Compose

Create:

```text
docker-compose.yml
```

Define:

- `api1`
- `api2`

Both should build from the same Dockerfile.

Configure:

```text
api1 -> INSTANCE_NAME=api-1
api2 -> INSTANCE_NAME=api-2
```

Map:

```text
api1: host 5001 -> container 8080
api2: host 5002 -> container 8080
```

Start them before adding NGINX:

```bash
docker compose up --build api1 api2
```

Verify:

```text
http://localhost:5001/api/hello
http://localhost:5002/api/hello
```

Do not continue until both work.

## Step 4 - Add NGINX

Create:

```text
nginx/nginx.conf
```

Define an upstream containing:

```text
api1:8080
api2:8080
```

Create an NGINX service in Docker Compose using an official NGINX image.

Mount the configuration read-only.

Map:

```text
host 8080 -> nginx container 80
```

## Step 5 - Launch the complete system

Run:

```bash
docker compose down
docker compose up --build
```

Optional detached mode:

```bash
docker compose up --build -d
```

Check:

```bash
docker compose ps
```

You should see:

```text
api1
api2
nginx
```

## Step 6 - Test the load balancer

Open:

```text
http://localhost:8080/api/hello
```

Refresh repeatedly.

Alternatively:

### PowerShell

```powershell
1..10 | ForEach-Object {
    Invoke-RestMethod http://localhost:8080/api/hello
}
```

### curl

```bash
curl http://localhost:8080/api/hello
```

Run it repeatedly.

The serving instance should alternate between `api-1` and `api-2` under simple sequential testing.

## Step 7 - Inspect what is happening

View logs:

```bash
docker compose logs -f
```

Specific services:

```bash
docker compose logs -f nginx
docker compose logs -f api1
docker compose logs -f api2
```

Inspect Docker network:

```bash
docker network ls
```

Then inspect the Compose-created network:

```bash
docker network inspect <project-name>_default
```

Notice:

- each container has its own IP address;
- you did not need to place those IPs in `nginx.conf`;
- service-name DNS is the stable abstraction.

## Step 8 - Prove failure behaviour

Stop one API:

```bash
docker compose stop api1
```

Call NGINX again.

Observe and record what happens.

Then restart it:

```bash
docker compose start api1
```

This is useful because it introduces the difference between:

- basic load balancing;
- active health checking;
- passive failure handling;
- resilient production architecture.

Do not over-engineer the solution yet.

## Step 9 - Clean up

```bash
docker compose down
```

To remove built images as well:

```bash
docker compose down --rmi local
```

## Recommended Learning Order

Do not ask AI to generate everything before understanding the moving parts.

A useful sequence is:

1. Get one API working locally.
2. Put that API in one container.
3. Run two copies using Compose.
4. Verify both copies directly.
5. Add NGINX.
6. Route through NGINX.
7. Inspect logs and networking.
8. Break one service deliberately.
9. Explain the architecture back in your own words.

That gives considerably more system-design value than simply generating the finished files.
