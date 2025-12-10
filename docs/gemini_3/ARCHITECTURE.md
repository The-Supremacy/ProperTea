# ProperTea - Technical Architecture

> **Version**: 4.0 (Final)
> **Goal**: Modern Microservices Education & Azure Parity.

## 1. Environment Strategy (The "4-Tier" Model)

We treat the "Local Cluster" as a distinct **SIT** environment on separate hardware.

| Env | Type | Infrastructure | Purpose |
| :--- | :--- | :--- | :--- |
| **DEV** | **Local** | **Docker Compose** | Inner-loop logic, debugging, quick iteration. |
| **SIT** | **Remote** | **Talos Linux** (Mini-PC) | Infrastructure testing, Helm charts, GitOps validation. |
| **UAT** | **Cloud** | **Azure AKS** | User Acceptance Testing, Cloud parity check. |
| **PROD**| **Cloud** | **Azure AKS** | Production traffic. |

## 2. Technical Stack

| Area | Technology | Implementation Details |
| :--- | :--- | :--- |
| **Identity** | **Authentik** | **Dev:** Points to `Mailpit`.<br>**SIT/Prod:** Points to `SendGrid`. |
| **Backend** | **.NET 10** | Vertical Slice Architecture. |
| **Frontend** | **Next.js 15** | BFF Pattern (Server Actions) + `next-auth` v5. |
| **Database** | **PostgreSQL 18** | **Strategy:** Single Instance, Multiple Databases (`propertea_org`, `propertea_comp`).<br>**Cloud:** Azure Flex Server (Preview). |
| **Storage** | **Azurite** | **Dev/SIT:** Emulator (Azure Blob parity).<br>**Cloud:** Azure Storage Account. |
| **Secrets** | **Infisical** | **Dev:** CLI Wrapper (`infisical run`).<br>**SIT/Cloud:** K8s Operator. |
| **Flags** | **Unleash** | Self-hosted container for feature management. |
| **Observability**| **Prometheus Stack** | **Loki** (Logs), **Tempo** (Traces), **Prometheus** (Metrics). |

## 3. Tooling & Development

### A. Secrets (Infisical)
We do not commit `.env` files.
* **Setup:** `brew install infisical`
* **Usage:** Run `infisical run -- docker-compose up`
* **Mechanism:** The CLI fetches secrets from the project and injects them as environment variables into the Docker process.

### B. Local HTTPS (mkcert)
We use valid SSL certificates locally to support Secure Cookies.
* **Setup:** `mkcert -install` (One time) -> `mkcert "*.propertea.localhost"`
* **Usage:** Traefik mounts these certificates.
* **Result:** Green lock in browser at `https://app.propertea.localhost`.

### C. Azure Emulation (Azurite)
We use the official Microsoft emulator to avoid "S3 Wrapper" code.
* **Container:** `mcr.microsoft.com/azure-storage/azurite`
* **Code:** Use `new BlobServiceClient("UseDevelopmentStorage=true")`.
* **Parity:** 100% API compatible with Azure Cloud.

### D. Logs (Dozzle)
A lightweight log viewer for Docker Compose.
* **Access:** `http://localhost:8888`
* **Benefit:** Filter logs by container (e.g., just show `propertea_org` errors) without CLI noise.

## 4. System Architecture

### Tenant Resolution Flow
1.  **Request:** `https://acme.propertea.com` -> **Traefik** -> **Next.js BFF**.
2.  **Identification:** BFF checks Redis for slug `acme`. Resolves to GUID.
3.  **Propagation:** BFF adds header `X-Organization-Id: <GUID>` to all downstream API calls.
4.  **Enforcement:** .NET Middleware reads header; sets `EF Core` Query Filter.

### Authorization (ReBAC)
* **Engine:** OpenFGA.
* **Model:** Defined in `src/ProperTea.Permissions/model.fga`.
* **Check:** `.NET SDK` calls OpenFGA: `Check(User:Bob, relation:can_view, object:invoice:123)`.

### SIT Cluster (Talos Linux)
* **Hardware:** Mini-PC (Intel NUC / Beelink).
* **OS:** Talos (API-managed, No SSH).
* **Management:** `talosctl` from Dev Laptop.
* **Exposure:** **Cloudflare Tunnel** (`cloudflared`) to expose `https://sit.propertea.com` without opening router ports.

## 5. Diagram: SIT/Prod Topology

```mermaid
graph TD
    subgraph "External"
        User[Developer / Tester]
    end

    subgraph "SIT Cluster (Talos)"
        subgraph "Ingress"
            Tunnel[Cloudflare Tunnel]
            Traefik[Traefik Gateway]
        end

        subgraph "Platform"
            Authentik[Authentik]
            Unleash[Unleash]
            Infisical[Infisical Operator]
        end

        subgraph "App Services"
            BFF[Next.js Portal]
            OrgSvc[Organization Svc]
        end

        subgraph "Data"
            Postgres[(PostgreSQL 18)]
            Azurite[(Azurite)]
            Mailpit[(Mailpit)]
        end
    end

    User -- "[https://sit.propertea.com](https://sit.propertea.com)" --> Tunnel
    Tunnel --> Traefik
    Traefik --> BFF
    Traefik --> Authentik

    BFF -- "X-Organization-Id" --> OrgSvc
    OrgSvc --> Postgres
    OrgSvc --> Azurite
