# Local Development Guide

**Version:** 1.0.0  
**Last Updated:** October 25, 2025  
**Status:** MVP 1 Setup Guide

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Development Modes](#development-modes)
4. [Mode 1: Inner Loop (Single Service)](#mode-1-inner-loop-single-service)
5. [Mode 2: Application Loop (Multi-Service Debug)](#mode-2-application-loop-multi-service-debug)
6. [Mode 3: Integration Testing](#mode-3-integration-testing)
7. [Mode 4: Pre-Production (Kind)](#mode-4-pre-production-kind)
8. [Debugging Guide](#debugging-guide)
9. [Troubleshooting](#troubleshooting)

---

## Overview

ProperTea supports **4 development modes** optimized for different workflows:

| Mode | What Runs | Debugging | When to Use |
|------|-----------|-----------|-------------|
| **1. Inner Loop** | Infrastructure (Docker) + One service (Rider) | ✅ Full debugging (one service) | Daily feature development |
| **2. Application Loop** | All services (Docker) | ✅ Attach to 3-5 services | Multi-service flows, e2e debugging |
| **3. Integration Testing** | All services (Docker) | ❌ Logs + traces only | Automated testing, CI/CD |
| **4. Pre-Production** | All services (Kind k8s) | ❌ kubectl logs | Validate Helm charts, k8s configs |

---

## Prerequisites

### Required Software

```bash
# .NET SDK
dotnet --version  # Should be 8.0 or higher

# Docker
docker --version  # Should be 24.0 or higher
docker compose version

# Rider IDE
# Download from: https://www.jetbrains.com/rider/

# Kind (for Mode 4)
kind --version

# Helm (for Mode 4)
helm version

# Optional: kubectl
kubectl version --client
```

### Hardware Requirements

- **CPU:** 4+ cores (8+ recommended for full platform)
- **RAM:** 16GB minimum (32GB recommended)
- **Disk:** 50GB free space (for Docker images, volumes)

---

## Development Modes

### Quick Start

```bash
# Clone repository
git clone https://github.com/your-org/ProperTea.git
cd ProperTea

# Mode 1: Infrastructure only
make infra-up

# Mode 2: Full platform
make platform-up

# Mode 3: Integration tests
make integration-test

# Mode 4: Kind cluster
make kind-up
```

---

## Mode 1: Inner Loop (Single Service)

**Purpose:** Fast iteration on a single service with full debugging.

### Setup

**1. Start Infrastructure:**
```bash
make infra-up
```

This starts:
- PostgreSQL (port 5432)
- Redis (port 6379)
- Kafka + Zookeeper (ports 9092, 9093)
- Elasticsearch (port 9200)
- SeaweedFS (ports 9000, 9001)
- Jaeger (port 16686)
- Prometheus (port 9090)
- Loki (port 3100)
- Grafana (port 3000)

**2. Configure Service:**

```json
// appsettings.Development.json (e.g., Identity.Service)
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=identity;Username=dev;Password=dev",
    "Redis": "localhost:6379",
    "Kafka": "localhost:9093"
  },
  "JwtSettings": {
    "Secret": "local-dev-secret-key-at-least-32-characters-long",
    "Issuer": "ProperTea.Identity",
    "Audience": "ProperTea"
  },
  "ProperTelemetry": {
    "Endpoint": "http://localhost:4317"
  }
}
```

**3. Run Database Migrations:**

```bash
cd services/Identity/ProperTea.Identity.Service
dotnet ef database update
```

**4. Debug in Rider:**

- Open `ProperTea.Identity.Service.csproj` in Rider
- Set breakpoints in your code
- Press `F5` or click "Debug" button
- Service starts on `http://localhost:5001`

**5. Test the Service:**

```bash
# Register a user
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Login
curl -X POST http://localhost:5001/api/token/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'
```

**6. View Telemetry:**

- **Jaeger (Traces):** http://localhost:16686
- **Grafana (Dashboards):** http://localhost:3000
- **Prometheus (Metrics):** http://localhost:9090

### Workflow

```bash
# Make code changes
# Hot reload kicks in (if enabled)
# OR press Ctrl+F5 to restart with debugging

# View logs in Rider console
# Set breakpoints, inspect variables
# Step through code (F8, F9)
```

### Stopping

```bash
# Stop service: Ctrl+C in Rider
# Stop infrastructure
make infra-down
```

---

## Mode 2: Application Loop (Multi-Service Debug)

**Purpose:** Debug interactions between 2-5 services (e2e flows).

### Setup

**1. Start Full Platform:**

```bash
make platform-up
```

This starts:
- All infrastructure (from Mode 1)
- All microservices (APIs + Workers) in Docker
- Services exposed on ports 5001-5020

**Wait ~30 seconds for all containers to start.**

**2. Verify Services Running:**

```bash
docker ps | grep propertea

# Should see:
# propertea-identity-service
# propertea-identity-worker
# propertea-contact-service
# propertea-contact-worker
# propertea-landlord-bff
# ... etc
```

**3. Attach Debugger in Rider:**

**Method: Attach to Docker Containers**

1. In Rider: `Run → Attach to Process...` (or `Ctrl+Alt+F5`)
2. In "Show processes from" dropdown: Select **"Docker"**
3. Filter by "propertea" to see your services
4. Select containers to debug (Ctrl+Click for multiple):
   - `identity-service`
   - `contact-service`
   - `landlord-bff`
5. Click **"Attach with .NET Debugger"**

**Rider will attach debuggers to selected containers!**

**4. Set Breakpoints:**

- Open `Identity.Service/Endpoints/Auth/Register.cs`
- Set breakpoint at line where user is created
- Open `Contact.Worker/Handlers/UserCreatedHandler.cs`
- Set breakpoint where contact is created

**5. Trigger Flow:**

```bash
# Register user via BFF
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@test.com","password":"Test123!"}'
```

**6. Debug Across Services:**

- Request hits BFF → routes to Identity service
- **Breakpoint triggers in Identity.Service!**
- Step through code, inspect variables
- Continue (F9) → Identity publishes `UserCreated` event
- Contact worker consumes event
- **Breakpoint triggers in Contact.Worker!**
- Full e2e debugging! 🎉

### Container Logs

```bash
# View logs for specific service
docker logs -f propertea-identity-service

# View all logs
docker-compose -f docker-compose.services.yml logs -f
```

### Rebuilding After Code Changes

```bash
# Rebuild specific service
docker-compose -f docker-compose.services.yml up -d --build identity-service

# Reattach debugger in Rider
```

### Stopping

```bash
make platform-down
```

---

## Mode 3: Integration Testing

**Purpose:** Automated end-to-end tests across multiple services.

### Setup

**Test Project Structure:**

```
tests/
  integration/
    ProperTea.Integration.Tests/
      UserRegistrationFlowTests.cs
      PropertyPublicationFlowTests.cs
      Fixtures/
        PlatformFixture.cs
```

**Platform Fixture:**

```csharp
// tests/integration/ProperTea.Integration.Tests/Fixtures/PlatformFixture.cs
public class PlatformFixture : IAsyncLifetime
{
    public HttpClient LandlordBffClient { get; private set; }
    public HttpClient IdentityClient { get; private set; }
    public HttpClient ContactClient { get; private set; }

    public async Task InitializeAsync()
    {
        // Start docker-compose
        await ProcessEx.RunAsync("docker-compose", 
            "-f docker-compose.infrastructure.yml up -d");
        await ProcessEx.RunAsync("docker-compose", 
            "-f docker-compose.services.yml up -d --build");
        
        // Wait for health checks
        await WaitForHealthyAsync("http://localhost:5000/health", timeout: TimeSpan.FromMinutes(2));
        
        LandlordBffClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        IdentityClient = new HttpClient { BaseAddress = new Uri("http://localhost:5001") };
        ContactClient = new HttpClient { BaseAddress = new Uri("http://localhost:5002") };
    }

    public async Task DisposeAsync()
    {
        await ProcessEx.RunAsync("docker-compose", "-f docker-compose.services.yml down -v");
        await ProcessEx.RunAsync("docker-compose", "-f docker-compose.infrastructure.yml down -v");
    }
}
```

**Integration Test Example:**

```csharp
public class UserRegistrationFlowTests : IClassFixture<PlatformFixture>
{
    private readonly PlatformFixture _platform;

    [Fact]
    public async Task UserRegistration_ShouldCreateContactAndAssignGroups()
    {
        // Arrange
        var request = new { email = "test@example.com", password = "Test123!" };

        // Act: Register via BFF
        var response = await _platform.LandlordBffClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert: Registration succeeded
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.ShouldNotBeNull();

        // Assert: Contact created (eventual consistency - wait up to 10 seconds)
        await _platform.WaitForAsync(async () =>
        {
            var contactResponse = await _platform.ContactClient.GetAsync($"/api/contacts/by-user/{result.UserId}");
            return contactResponse.IsSuccessStatusCode;
        }, timeout: TimeSpan.FromSeconds(10));

        // Assert: Can login
        var loginResponse = await _platform.LandlordBffClient.PostAsJsonAsync("/api/auth/login", 
            new { email = request.email, password = request.password });
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

### Running Tests

```bash
# Run all integration tests
make integration-test

# Or manually
docker-compose -f docker-compose.infrastructure.yml up -d
docker-compose -f docker-compose.services.yml up -d --build
dotnet test tests/integration/ProperTea.Integration.Tests
docker-compose -f docker-compose.services.yml down -v
docker-compose -f docker-compose.infrastructure.yml down -v
```

### CI/CD Integration

```yaml
# .github/workflows/integration-tests.yml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Start services
        run: |
          docker-compose -f docker-compose.infrastructure.yml up -d
          docker-compose -f docker-compose.services.yml up -d --build
      
      - name: Run integration tests
        run: dotnet test tests/integration/ProperTea.Integration.Tests
      
      - name: Cleanup
        if: always()
        run: |
          docker-compose -f docker-compose.services.yml down -v
          docker-compose -f docker-compose.infrastructure.yml down -v
```

---

## Mode 4: Pre-Production (Kind)

**Purpose:** Test Helm charts and Kubernetes configurations locally.

### Setup Kind Cluster

**1. Create Cluster:**

```bash
make kind-up

# Or manually:
kind create cluster --name propertea-local --config kind-config.yaml
```

**kind-config.yaml:**
```yaml
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
  - role: control-plane
    extraPortMappings:
      - containerPort: 80
        hostPort: 80
      - containerPort: 443
        hostPort: 443
  - role: worker
  - role: worker
```

**2. Install Infrastructure:**

```bash
# Install Traefik ingress controller
helm repo add traefik https://traefik.github.io/charts
helm install traefik traefik/traefik

# Install PostgreSQL
helm repo add bitnami https://charts.bitnami.com/bitnami
helm install postgres bitnami/postgresql -f charts/infrastructure/postgres-values.yaml

# Install Redis
helm install redis bitnami/redis -f charts/infrastructure/redis-values.yaml

# Install Kafka
helm install kafka bitnami/kafka -f charts/infrastructure/kafka-values.yaml
```

**3. Build and Load Images:**

```bash
# Build all service images
make docker-build

# Load images into Kind cluster
kind load docker-image propertea/identity-service:latest --name propertea-local
kind load docker-image propertea/contact-service:latest --name propertea-local
# ... repeat for all services
```

**4. Deploy Services:**

```bash
# Deploy all services via Helm
helm install propertea ./charts/propertea -f values.local.yaml

# Verify deployments
kubectl get pods
kubectl get services
kubectl get ingress
```

**5. Access Services:**

```bash
# Port-forward to BFF
kubectl port-forward svc/landlord-bff 8080:80

# Test
curl http://localhost:8080/health
```

### View Logs

```bash
# Logs for specific pod
kubectl logs -f deployment/identity-service

# Logs for worker
kubectl logs -f deployment/identity-worker

# Logs with label selector
kubectl logs -l app=identity-service --tail=100
```

### Debugging Issues

```bash
# Describe pod (see events, errors)
kubectl describe pod identity-service-xxx

# Get pod status
kubectl get pods -o wide

# Execute into pod
kubectl exec -it identity-service-xxx -- /bin/bash
```

### Cleanup

```bash
# Delete cluster
kind delete cluster --name propertea-local

# Or via Makefile
make kind-down
```

---

## Debugging Guide

### Debugging in Mode 1 (Rider Native)

**Full debugging capabilities:**
- Breakpoints
- Step through (F8), Step into (F7), Step out (Shift+F8)
- Variable inspection
- Hot reload (edit code while debugging)
- Conditional breakpoints
- Exception breakpoints

### Debugging in Mode 2 (Attach to Docker)

**Capabilities:**
- Breakpoints ✅
- Step through ✅
- Variable inspection ✅
- Hot reload ❌ (requires container rebuild)

**Limitations:**
- Each attached debugger adds ~50MB RAM overhead
- Recommend attaching to max 5 services
- Hot reload doesn't work - must rebuild container for code changes

**Workflow:**
1. Make code changes
2. Rebuild container: `docker-compose up -d --build identity-service`
3. Reattach debugger in Rider
4. Set breakpoints
5. Trigger flow

### Using Distributed Tracing Instead of Debugging

**For complex flows across many services, use Jaeger:**

1. Trigger flow (e.g., user registration)
2. Open Jaeger: http://localhost:16686
3. Search for trace by operation (e.g., "POST /api/auth/register")
4. View full trace:
   - BFF → Identity (12ms)
   - Identity → Kafka (publish event) (3ms)
   - Contact Worker ← Kafka (consume event) (5ms)
   - Contact → Database (create contact) (8ms)
5. Click on each span to see:
   - Tags (userId, email, etc.)
   - Logs (info, errors)
   - Duration

**This is often faster than debugging for understanding flows!**

### Remote Debugging in Kind (Advanced)

**Only for production-like issues:**

1. Deploy service with debug configuration
2. Port-forward debugger port: `kubectl port-forward pod/identity-service-xxx 5000:5000`
3. In Rider: `Run → Attach to Remote Process → localhost:5000`

**Not recommended for daily development - use Mode 1 or 2.**

---

## Troubleshooting

### Issue: "Port already in use"

```bash
# Find process using port
lsof -i :5432  # PostgreSQL
lsof -i :6379  # Redis

# Kill process
kill -9 <PID>

# Or stop all Docker containers
docker stop $(docker ps -aq)
```

### Issue: "Cannot connect to database"

```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Check logs
docker logs propertea-postgres

# Restart
docker-compose -f docker-compose.infrastructure.yml restart postgres
```

### Issue: "Rider can't attach to Docker container"

**Ensure container built with debug symbols:**

```dockerfile
# Dockerfile.debug
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
RUN apt-get update && apt-get install -y unzip && \
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

# Build with Debug configuration
RUN dotnet build -c Debug
RUN dotnet publish -c Debug
```

```bash
# Rebuild with debug Dockerfile
docker build -f Dockerfile.debug -t propertea/identity-service:debug .
```

### Issue: "Integration tests failing - services not ready"

**Increase wait timeout in PlatformFixture:**

```csharp
await WaitForHealthyAsync("http://localhost:5000/health", 
    timeout: TimeSpan.FromMinutes(3)); // Increased from 2 to 3 minutes
```

### Issue: "Kind cluster out of disk space"

```bash
# Prune Docker images
docker system prune -a

# Check disk usage
docker system df
```

---

## Makefile Reference

```makefile
# Mode 1: Infrastructure
infra-up:           # Start infrastructure (Postgres, Redis, Kafka, etc.)
infra-down:         # Stop infrastructure
infra-logs:         # View infrastructure logs

# Mode 2: Full Platform
platform-up:        # Start infrastructure + all services
platform-down:      # Stop all services
platform-rebuild:   # Rebuild and restart services

# Mode 3: Integration Tests
integration-test:   # Run integration tests
test-watch:         # Run tests in watch mode

# Mode 4: Kind
kind-up:            # Create Kind cluster + deploy services
kind-down:          # Delete Kind cluster
kind-logs:          # View pod logs

# Utilities
docker-build:       # Build all Docker images
clean:              # Remove all containers, volumes, images
```

---

**Next Documents:**
- `06-deployment-kubernetes.md` - Helm charts, k8s manifests
- `10-migration-guide.md` - Refactor existing code
- `11-implementation-roadmap.md` - Phased approach

**Document Version:**

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-10-22 | Initial local development guide |

