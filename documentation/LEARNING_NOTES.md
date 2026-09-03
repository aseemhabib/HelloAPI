# System Design Learning Notes

## 1. Reverse Proxy vs Load Balancer

In this lab NGINX performs both roles.

### Reverse proxy

The browser knows only:

```text
localhost:8080
```

It does not need to know the API containers.

NGINX receives the request and proxies it to a backend service.

### Load balancer

Because NGINX has more than one backend instance in its upstream group, it chooses which backend should receive each request.

For this lab the algorithm is round robin.

## 2. Horizontal Scaling

You are not giving one application more CPU/RAM.

Instead, you are running multiple copies of the same application:

```text
api1
api2
```

That models horizontal scaling.

## 3. Why Statelessness Matters

Suppose request 1 is processed by api1 and request 2 is processed by api2.

If important user state exists only in api1's process memory, api2 will not know about it.

For horizontally scaled systems, request-serving application instances are therefore normally designed to be stateless, with shared state placed in external systems when required.

This lab contains no state at all, which makes it ideal for load balancing.

## 4. Image vs Container

The ASP.NET Dockerfile creates an **image**.

Docker Compose starts two **containers** from that same application definition.

Conceptually:

```text
                 +--> container api1
hello-api image -|
                 +--> container api2
```

This is more realistic than maintaining two copies of the source.

## 5. Host Port vs Container Port

Consider:

```yaml
ports:
  - "5001:8080"
```

It means:

```text
Windows host port 5001
        |
        v
container port 8080
```

For api2:

```text
5002:8080
```

Both containers can use 8080 internally because they are isolated from one another.

## 6. Container-to-Container Networking

Inside the Docker Compose network, NGINX should use:

```text
api1:8080
api2:8080
```

`api1` and `api2` are DNS-resolvable Compose service names.

The Windows host mappings 5001 and 5002 are not needed for NGINX.

They exist only to make this learning exercise easier to inspect from your browser.

## 7. Why Not Hard-Code Container IPs?

Container IP addresses can change when containers are recreated.

Service names are stable.

Therefore:

```text
api1:8080
```

is a better dependency address than:

```text
172.x.x.x:8080
```

## 8. Round Robin

With two equal backend servers, a simplified sequence looks like:

```text
Request 1 -> api1
Request 2 -> api2
Request 3 -> api1
Request 4 -> api2
```

Real observed results can sometimes differ because of connection behaviour, failures, retries, browser behaviour, or configuration, but this is the conceptual model.

NGINX uses round robin by default when no other upstream selection method is specified.

## 9. Where This Appears in Real Systems

The local architecture:

```text
Client
  |
NGINX
 / \
API API
```

represents the same broad idea seen in production architectures such as:

```text
Internet
   |
Cloud/Application Load Balancer
   |
+--+--+--+
|  |  |  |
API instances
```

The implementation technology changes, but the system-design principle is similar.

## 10. Questions You Should Be Able to Answer Afterwards

1. Why use a load balancer?
2. What is horizontal scaling?
3. Why should backend services be stateless?
4. What is a reverse proxy?
5. What is round robin?
6. What happens when one backend fails?
7. What is the difference between a Docker image and a container?
8. What is the difference between host and container ports?
9. How does NGINX locate the API containers?
10. Why should you use service names rather than container IPs?
11. Why is NGINX the only endpoint a normal client should need?
12. Where would TLS termination normally occur?
13. How would health checks improve this design?
14. How would the design change if sessions were stored in local memory?
15. How is this conceptually related to an AWS ALB, Azure Application Gateway, or Kubernetes Service/Ingress?

## Suggested Phase 2 Exercises

Do these one at a time rather than all together.

### Exercise A - Add a third API instance

Add:

```text
api3
```

and observe the distribution.

### Exercise B - Change algorithm

Change NGINX from round robin to `least_conn`.

Understand why least-connections can behave differently under longer-running requests.

### Exercise C - Weighted routing

Give one backend a greater weight and observe request distribution.

### Exercise D - Simulate latency

Add an optional endpoint that delays for a few seconds.

Use this to compare round robin with least-connections.

### Exercise E - Failure experiment

Stop one API while sending requests.

Study how NGINX responds.

### Exercise F - Health endpoint

Add:

```text
GET /health
```

and learn the difference between an application health endpoint and load-balancer health-check behaviour.

### Exercise G - Add a tiny UI

Only after the infrastructure is understood, add a simple React page that repeatedly calls the load-balanced endpoint and displays which instance responded.

That would make a useful bridge into the larger Engineering Manager application.
