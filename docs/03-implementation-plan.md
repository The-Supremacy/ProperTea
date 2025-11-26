# 3. Implementation Plan

This document outlines the overall, multi-stage implementation plan for the ProperTea project. The project is broken
down into three distinct stages, each building upon the last to progressively add functionality and technical
complexity.

Detailed, step-by-step actions for each stage are located in the `/docs/stages/` directory.

---

## Stage 1: The Foundation

### Goal

To build the core services and validate all critical cross-cutting concerns, including authentication, multi-tenancy,
asynchronous communication, and the CQRS pattern. The entire system will run locally via Docker Compose.

### Scope

- 5 core backend services (.NET)
- 1 Backend-for-Frontend (BFF) Gateway (YARP)
- Foundational infrastructure: Keycloak, PostgreSQL, RabbitMQ, Redis.
- Local development environment using Docker Compose.

**➡️ See detailed plan: Stage 1 - Foundation**

---

## Stage 2: Kubernetes Migration & Expansion

### Goal

To migrate the application from Docker Compose to a production-like Kubernetes environment. This stage introduces
container orchestration, Helm for packaging, GitOps for deployment, and a full local observability stack.

### Scope

- Local Kubernetes cluster using **Kind**.
- Helm charts for all services.
- **ArgoCD** for GitOps-based continuous deployment.
- Local observability stack (Prometheus, Loki, Grafana, Tempo).
- Addition of more domain services and the first frontend portals.

**➡️ See detailed plan: Stage 2 - Kubernetes**

---

## Stage 3: Azure Cloud Production

### Goal

To deploy the application to a production-grade environment in Microsoft Azure, leveraging managed cloud services for
reliability, scalability, and security.

### Scope

- **Azure Kubernetes Service (AKS)** cluster.
- Infrastructure provisioned via **Terraform**.
- Migration to Azure managed services: Azure Database for PostgreSQL, Azure Service Bus, Azure Cache for Redis, Azure
  Key Vault.
- Integration with **Azure Monitor** for production observability.
- Implementation of production-grade security and deployment strategies (e.g., blue-green deployments).

**➡️ See detailed plan: Stage 3 - Azure**
