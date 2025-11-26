# 1. Project Overview

**Project:** ProperTea
**Purpose:** To build a production-ready, multi-tenant ERP system for real-estate property management, while
simultaneously mastering Azure cloud services, microservice architecture, and Kubernetes infrastructure.

---

## 1.1. Product Description

**ProperTea** is an ERP system that focuses on real-estate properties management, renting properties, and marketing them
to potential residents.

The system is designed to be multi-tenant, where each "Organization" (e.g., a property management company) has its own
isolated data and users.

## 1.2. Domain & User Types

The system revolves around properties, tenants, and the organizations that manage them. There are three primary types of
users:

1. **Landlord Office User:**
    * Scoped to a specific **Organization**.
    * These users perform the primary business functions: managing properties, units, finances, and contracts for their
      company.

2. **Tenant:**
    * **Not** scoped to a single Organization.
    * A single person can be a tenant for multiple properties managed by different Organizations.
    * The system must provide a unified view for a tenant to see all their rental contracts in one place.

3. **Market User:**
    * Represents prospective tenants browsing the public-facing market portal.
    * Can be anonymous or authenticated (e.g., to save favorite properties).

### 1.3. Identity Model

A core requirement is a **single identity system** for all user types. It is a realistic scenario for the same person to
be both a Landlord Office user for one company and a Tenant in a property managed by another. The system must support
this dual role, allowing users to switch contexts within the application.

## 1.4. Core Requirements

- **No License Fees:** All libraries, tools, and software used must be open-source and free from license fees.
- **Cloud Native & Azure Focused:** The platform must be designed for the cloud, leveraging Azure-native services (
  PostgreSQL, Service Bus, Redis, etc.) in production.
- **Production Parity:** The local development environment must be as close to the production AKS cluster as possible to
  minimize surprises during deployment.
- **Modern & Maintainable:** Utilize modern, well-supported technologies and best practices to ensure the long-term
  health of the project.
- **Staged Approach:** The project will be built in stages, with each stage layering complexity and introducing new
  concepts, from local Docker Compose to a full production-grade AKS deployment.
