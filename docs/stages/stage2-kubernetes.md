# Implementation Plan: Stage 2 - Kubernetes Migration

### Goal
Move the application to a production-like Kubernetes infrastructure, introducing Helm, a full local observability stack, and GitOps deployment practices.

---

## 2.1. Environment & Tooling

- **Cluster:** Kind (Kubernetes in Docker)
- **Packaging:** Helm charts
- **Ingress:** Traefik Ingress Controller
- **Observability:** Prometheus (metrics), Loki (logs), Grafana (dashboards), Tempo (traces)
- **Deployment:** ArgoCD

## 2.2. Services to Add

- **Financial Service:** Manages invoicing and payments.
- **Maintenance Service:** Manages work orders.
- **Tenant Portal (Frontend):** The Next.js application for tenants.
- **Market Portal (Frontend):** The Next.js application for prospective renters.

## 2.3. Step-by-Step Action Plan

### Phase 1: Cluster Setup & Containerization

1.  **Install Tooling:** Install Kind, Helm, and `kubectl` on the development machine.
2.  **Create Kind Cluster:** Write a script to create a local Kind cluster with the necessary configuration (e.g., port mappings).
3.  **Create Helm Charts:**
    - For each microservice, create a Helm chart in `infrastructure/helm/charts/`.
    - Parameterize the image tag, replicas, resource limits, and environment variables.
    - Create templates for `Deployment`, `Service`, and `ConfigMap`.
4.  **Deploy Infrastructure Charts:** Install Helm charts for Traefik, Prometheus, Grafana, etc., into the Kind cluster.

### Phase 2: Application Deployment & GitOps

1.  **Install ArgoCD:** Deploy ArgoCD to the Kind cluster and configure it to watch your Git repository.
2.  **Create ArgoCD Applications:**
    - In your repository, create ArgoCD `Application` manifests for each of your microservices.
    - These manifests will point to the Helm charts you created.
3.  **Initial Deployment:** Commit the ArgoCD application manifests. ArgoCD should automatically detect them and deploy your services using Helm.
4.  **Configure Ingress:** Create Traefik `IngressRoute` resources to expose your BFFs and APIs to the host machine.
5.  **Verify Deployment:** Ensure all pods are running and that you can access the application through the Traefik ingress.

### Phase 3: Observability & Networking

1.  **Instrument Services:** Ensure all services are exporting OpenTelemetry data.
2.  **Configure Telemetry Collection:** Deploy the OpenTelemetry Collector, configured to scrape metrics for Prometheus and send traces to Tempo.
3.  **Configure Logging:** Configure your services to log structured JSON to `stdout`. Set up a log collection agent (e.g., Promtail) to forward logs to Loki.
4.  **Create Grafana Dashboards:** Build basic dashboards in Grafana to visualize key metrics (request rate, error rate, latency) and query logs from Loki and traces from Tempo.
5.  **Implement Network Policies:**
    - Define Kubernetes `NetworkPolicy` resources for each service.
    - By default, deny all ingress/egress traffic.
    - Explicitly allow traffic only from specific sources (e.g., the Property service should only accept traffic from the BFF).

### Phase 4: Testing

1.  **Contract Testing:**
    - Introduce Pact for contract testing between services.
    - Integrate Pact verification into the CI pipeline.
2.  **E2E Testing:**
    - Adapt the E2E test suite to run against the application deployed in the Kind cluster.
    - The CI pipeline should trigger these tests after a successful deployment to the dev environment.
