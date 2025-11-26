# Implementation Plan: Stage 3 - Azure Cloud Production

### Goal
Deploy the application to a production-grade Azure Kubernetes Service (AKS) cluster, utilizing managed Azure services for data, messaging, and security.

---

## 3.1. Production Environment & Services

- **Cluster:** Azure Kubernetes Service (AKS)
- **Infrastructure as Code (IaC):** Terraform
- **Database:** Azure Database for PostgreSQL Flexible Server
- **Messaging:** Azure Service Bus Premium
- **Cache:** Azure Cache for Redis
- **Secrets:** Azure Key Vault (integrated with AKS via CSI driver)
- **Storage:** Azure Blob Storage
- **Observability:** Azure Monitor (Application Insights, Log Analytics)
- **Ingress:** Azure Application Gateway Ingress Controller (AGIC)

## 3.2. Step-by-Step Action Plan

### Phase 1: Infrastructure Provisioning with Terraform

1.  **Setup Terraform:** Initialize a Terraform project in the `infrastructure/terraform/` directory. Configure the Azure provider and remote state storage (e.g., in an Azure Storage Account).
2.  **Provision Core Resources:** Write Terraform modules to provision:
    - Resource Group
    - Virtual Network (VNet) and subnets
    - Azure Container Registry (ACR)
3.  **Provision Managed Services:** Create Terraform resources for:
    - Azure Database for PostgreSQL (Azure Postgres Flexible Server)
    - Azure Service Bus
    - Azure Cache for Redis
    - Azure Key Vault
4.  **Provision AKS Cluster:** Create the Terraform configuration for the AKS cluster itself, integrating it with the VNet and enabling features like the Azure Key Vault CSI driver.

### Phase 2: Deployment & Configuration

1.  **Update CI/CD:** Modify the GitHub Actions workflow to:
    - Build and push Docker images to Azure Container Registry (ACR).
    - Run `terraform apply` to provision/update infrastructure.
2.  **Adapt Helm Charts:** Create production-specific `values.yaml` files for your Helm charts. These files will override defaults with connection strings, hostnames, and other settings for the Azure environment.
3.  **Manage Secrets:**
    - Store all production secrets (passwords, connection strings) in Azure Key Vault.
    - Update Helm charts to use the CSI driver to mount these secrets as files into the pods.
4.  **Configure ArgoCD for Production:** Set up ArgoCD to manage deployments to the AKS cluster, pointing to the production-specific Helm configurations.
5.  **Configure Ingress (AGIC):** Deploy the Application Gateway Ingress Controller to AKS and create Kubernetes `Ingress` resources to expose services via the Azure Application Gateway.

### Phase 3: Production Readiness

1.  **Migrate to Azure Monitor:** Update service configurations to send OpenTelemetry data directly to Azure Monitor / Application Insights.
2.  **Implement Deployment Strategy:** Configure a blue-green or canary deployment strategy using ArgoCD and ingress tooling to ensure zero-downtime releases.
3.  **Security Hardening:**
    - Apply Azure Policies to the AKS cluster to enforce security best practices (e.g., Pod Security Standards).
    - Implement vulnerability scanning for container images in ACR.
4.  **Load Testing:** Use a tool like k6 or Azure Load Testing to run performance tests against the production environment to establish baselines and identify bottlenecks.
5.  **Create Runbooks:** Document common operational procedures, such as disaster recovery (restoring a database from a backup), scaling services, and troubleshooting common errors.
