# Event Catalog

All integration events exchanged between services via Wolverine and RabbitMQ.
Contracts (interfaces) live in `shared/ProperTea.Contracts/Events/`. Implementations live in the publishing service.

## Naming Convention

`{entity}.{action}.v{version}` -- all lowercase, dot-separated.

## Events

### Exchange: `organization.events` (Fanout)

Publisher: Organization Service

| Message Identity | Contract | Trigger | Subscribers |
|---|---|---|---|
| `organizations.registered.v1` | `IOrganizationRegistered` | `RegisterOrganizationHandler` -- new tenant via headless flow | User Service, Company Service |
| `organizations.updated.v1` | `IOrganizationUpdated` | Organization details updated | (planned) |

### Exchange: `company.events` (Fanout)

Publisher: Company Service

| Message Identity | Contract | Trigger | Subscribers |
|---|---|---|---|
| `companies.created.v1` | `ICompanyCreated` | `CreateCompanyHandler` -- new company in organization | (planned: Property Service) |
| `companies.deleted.v1` | `ICompanyDeleted` | `DeleteCompanyHandler` -- soft delete | (planned: Property Service) |

### Exchange: `workorder.events` (Topic) -- planned

Publisher: Work Order Service

| Message Identity | Contract | Trigger | Subscribers |
|---|---|---|---|
| `workorder.assigned.v1` | `IWorkOrderAssigned` | Contractor org assigned | Notification Service |
| `workorder.completed.v1` | `IWorkOrderCompleted` | Task marked finished | Billing Service |

## Adding a New Event

1. Define interface in `shared/ProperTea.Contracts/Events/{Context}IntegrationEvents.cs`.
2. Implement as `[MessageIdentity("...")]` class in the publishing service.
3. Configure RabbitMQ exchange in publisher's Wolverine config.
4. Configure queue listener in each subscriber's Wolverine config.
5. Add row to the table above.
