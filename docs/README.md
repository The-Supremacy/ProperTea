# ProperTea - Documentation Index

**Version:** 1.2.0  
**Last Updated:** October 29, 2025  
**Project Status:** MVP 1 - In Development

---

## 📚 Documentation Overview

This documentation suite provides comprehensive guidance for developing, deploying, and maintaining the ProperTea microservices platform. All documents are designed to be both **human-readable** and **AI-assistant friendly** for efficient development workflows.

---

## 🎯 Quick Start

**For Developers:**
1. Start here: [00-architecture-overview.md](./00-architecture-overview.md) - Understand the system
2. Setup local environment: [09-local-development.md](./09-local-development.md) - Get running in 15 minutes
3. Pick your first service: [11-implementation-roadmap.md](./11-implementation-roadmap.md) - See what to build first

**For Saga Implementation:**
1. Quick reference: [QUICK-REFERENCE.md](./QUICK-REFERENCE.md) - Choreography vs Orchestration (5 min)
2. Working examples: [examples/sagas/](./examples/sagas/) - Copy and adapt these
3. Track progress: [IMPLEMENTATION-CHECKLIST.md](./IMPLEMENTATION-CHECKLIST.md) - Complete checklist

**For AI Assistants:**
- System context: [00-architecture-overview.md](./00-architecture-overview.md)
- Service specifications: [02-service-specifications.md](./02-service-specifications.md)
- Patterns and libraries: [03-event-driven-patterns.md](./03-event-driven-patterns.md), [04-shared-libraries.md](./04-shared-libraries.md)

---

## 📖 Document Structure

### Core Architecture (Start Here)

| Document | Description | Audience |
|----------|-------------|----------|
| **[00-architecture-overview.md](./00-architecture-overview.md)** | System design, principles, technology stack, service boundaries | Everyone |
| **[01-authentication-authorization.md](./01-authentication-authorization.md)** | Auth flows, JWT enrichment, sessions, permissions, GDPR | Backend Developers |
| **[02-service-specifications.md](./02-service-specifications.md)** | All 15 services: endpoints, models, events, database schemas | Backend Developers, AI |
| **[03-event-driven-patterns.md](./03-event-driven-patterns.md)** | Sagas, choreography, outbox pattern, event catalog | Backend Developers |

### Implementation Guides

| Document | Description | Audience |
|----------|-------------|----------|
| **[04-shared-libraries.md](./04-shared-libraries.md)** | ProperCqrs, ProperDdd, ProperSagas, ProperStorage usage | All Developers |
| **[09-local-development.md](./09-local-development.md)** | 4-mode workflow, debugging, docker-compose, Kind setup | All Developers |
| **[10-migration-guide.md](./10-migration-guide.md)** | Refactoring guide for existing services | Current Contributors |
| **[11-implementation-roadmap.md](./11-implementation-roadmap.md)** | Phased approach, service dependencies, milestones | Project Managers, Devs |
| **[IMPLEMENTATION-SUMMARY.md](./IMPLEMENTATION-SUMMARY.md)** | Architecture decisions and checklist | All Developers |
| **[QUICK-REFERENCE.md](./QUICK-REFERENCE.md)** | Choreography vs Orchestration quick reference | All Developers |
| **[examples/sagas/](./examples/sagas/)** | Complete working saga examples | Backend Developers |

### Deployment & Operations

| Document | Description | Audience |
|----------|-------------|----------|
| **[08-observability.md](./08-observability.md)** | OpenTelemetry, Jaeger, Loki, Prometheus, Grafana, Azure Monitor | DevOps, SRE |
| **[DESIGN-DECISIONS.md](./DESIGN-DECISIONS.md)** | Architectural decisions and rationales | Everyone |
| **[MIGRATION.md](./MIGRATION.md)** | Refactoring guide for existing services | Current Contributors |

---

## 🏗️ System Architecture at a Glance

### Services (15 Total)

**Core Services (5):**
- Identity - Authentication, JWT, external logins
- Contact - Organization-owned profiles, invitations, GDPR
- Organization - Orgs, companies, membership
- Permission - Groups, permissions, authorization
- Preferences - UI settings per portal/org

**Domain Services (6):**
- Property Base - Properties, buildings, rental objects
- Rental Management - Rental objects, vacancy periods, availability
- Market - Listings, applications, offers
- Lease - Agreements, approval, signatures
- Dwelling Inspection - Inspections scheduling, assignments
- Maintenance - Fault notifications, work orders

**Infrastructure Services (1):**
- Search - Elasticsearch indexing, autocomplete

**BFF Services (3):**
- Landlord BFF - Session management for Landlord portal
- Tenant BFF - Session management for Tenant portal
- Market BFF - Session management for Market portal

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Runtime** | .NET 9 |
| **API** | ASP.NET Core Minimal APIs |
| **Database** | PostgreSQL 17 |
| **Caching** | Redis 7 |
| **Messaging (Local)** | Kafka 4.0 |
| **Messaging (Prod)** | Azure Service Bus |
| **Search** | Elasticsearch 9.0 |
| **Storage (Local)** | Azurite (Azure Blob emulator) |
| **Storage (Prod)** | Azure Blob Storage |
| **Orchestration (Local)** | Kind (Kubernetes in Docker) |
| **Orchestration (Prod)** | Azure Kubernetes Service (AKS) |
| **Observability (Local)** | OpenTelemetry, Jaeger, Loki, Prometheus, Grafana |
| **Observability (Prod)** | Azure Monitor, Application Insights |

---

## 🎓 Learning Path

### Phase 1: Foundation
1. Read [00-architecture-overview.md](./00-architecture-overview.md)
2. Set up local development: [09-local-development.md](./09-local-development.md)
3. Run Identity service in Mode 1 (inner loop)
4. Understand auth flow: [01-authentication-authorization.md](./01-authentication-authorization.md)

### Phase 2: Core Services
1. Implement Contact service: [02-service-specifications.md](./02-service-specifications.md#contact-service)
2. Understand org-owned contact model: [SESSION-2025-10-29.md](./SESSION-2025-10-29.md)
3. Create Contact GDPR deletion saga: [03-event-driven-patterns.md](./03-event-driven-patterns.md#saga-orchestration-pattern)

### Phase 3: Domain Services
1. Implement Organization + Permission services
2. Add JWT enrichment to BFF (with orgOwnerUserId)
3. Test multi-service flow in Mode 2 (application loop)
4. Write integration tests

### Phase 4: Advanced Patterns
1. Implement Property Base service
2. Add event-driven choreography (Rental Management, Market)
3. Set up Elasticsearch
4. Deploy to Kind

---

## 📋 Implementation Status

See [11-implementation-roadmap.md](./11-implementation-roadmap.md) for detailed status.

---

## 🆘 Troubleshooting

See [09-local-development.md#troubleshooting](./09-local-development.md#troubleshooting) for common issues and solutions.

---

## 🔄 Document Maintenance

### Design Sessions
- [SESSION-2025-10-29-PART2.md](./SESSION-2025-10-29-PART2.md) - Storage strategy, observability, JWT optimization, contact ownership, AKS deployment
- [SESSION-2025-10-29.md](./SESSION-2025-10-29.md) - Contact ownership model, blob storage strategy, JWT optimization

### Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.2.0 | 2025-10-29 | Updated tech stack, removed ACA, simplified examples, clarified storage strategy |
| 1.1.0 | 2025-10-29 | Simplified README, updated service names, blob storage strategy |
| 1.0.0 | 2025-10-25 | Initial documentation suite for MVP 1 |

---

## 📄 License

MIT License - See LICENSE file for details.

---

**For detailed architectural decisions, see [DESIGN-DECISIONS.md](./DESIGN-DECISIONS.md)**
