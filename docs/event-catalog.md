# Event Catalog

This document tracks all integration events exchanged between services via Wolverine and RabbitMQ.

## Exchange: `organization.events`
**Publisher**: Organization Service (`ProperTea.Organization`)
**Transport**: RabbitMQ (Fanout)

| Message Identity | Payload Interface | Trigger | Subscribers |
| :--- | :--- | :--- | :--- |
| `organizations.registered.v1` | `IOrganizationRegistered` | **RegisterOrganizationHandler**<br>New tenant created via headless flow. | **User Service**<br>**Company Service** (creates default company) |
| `organizations.updated.v1` | `IOrganizationUpdated` | When organization details are updated. | *(Future: User Service, Company Service)* |

## Exchange: `company.events`
**Publisher**: Company Service (`ProperTea.Company`)
**Transport**: RabbitMQ (Fanout)

| Message Identity | Payload Interface | Trigger | Subscribers |
| :--- | :--- | :--- | :--- |
| `companies.created.v1` | `ICompanyCreated` | **CreateCompanyHandler**<br>New company created within organization. | *(Future: Property Service)* |
| `companies.deleted.v1` | `ICompanyDeleted` | **DeleteCompanyHandler**<br>Company soft deleted (cannot be undone). | *(Future: Property Service)* |

## Exchange: `workorder.events`
**Publisher**: Work Order Service (`ProperTea.WorkOrder`)
**Transport**: RabbitMQ (Topic)

| Message Identity | Payload Interface | Trigger | Subscribers |
| :--- | :--- | :--- | :--- |
| `workorder.assigned.v1` | `IWorkOrderAssigned` | When a contractor org is assigned. | **Notification Service** |
| `workorder.completed.v1` | `IWorkOrderCompleted` | When a task is marked finished. | **Billing Service** |

## Usage Guidelines
1. **Naming**: `{entity}.{action}.v{version}`.
2. **Contract Enforcement**: Producers implement interfaces from `ProperTea.Contracts`.
