# Event Catalog

All integration events exchanged between services via Wolverine and RabbitMQ.
Contracts (interfaces) live in `shared/ProperTea.Contracts/Events/`. Implementations live in the publishing service.

## Naming Convention

`{entity}.{action}.v{version}` -- all lowercase, dot-separated.

## Events

### Exchange: `organization.events` (Topic)

Publisher: Organization Service

| Message Identity | Contract | Trigger | Subscribers |
|---|---|---|---|
| `organizations.registered.v1` | `IOrganizationRegistered` | `RegisterOrganizationHandler` -- new tenant via headless flow | User Service, Company Service |
| `organizations.updated.v1` | `IOrganizationUpdated` | Organization details updated | (planned) |

### Exchange: `company.events` (Topic)

Publisher: Company Service

| Message Identity | Contract | Trigger | Subscribers |
|---|---|---|---|
| `companies.created.v1` | `ICompanyCreated` | `CreateCompanyHandler` -- new company in organization | Property Service |
| `companies.updated.v1` | `ICompanyUpdated` | `UpdateCompanyHandler` -- company details changed | Property Service |
| `companies.deleted.v1` | `ICompanyDeleted` | `DeleteCompanyHandler` -- soft delete | Property Service (planned cascade) |

### Exchange: `property.events` (Topic)

Publisher: Property Service

| Message Identity | Contract | Trigger | Subscribers |
|---|---|---|---|
| `properties.created.v1` | `IPropertyCreated` | `CreatePropertyHandler` -- new property (fields: Code, Name, Address, SquareFootage) | Rental Service (planned), Work Order Service (planned) |
| `properties.updated.v1` | `IPropertyUpdated` | `UpdatePropertyHandler` -- property details changed (fields: Code, Name, Address, SquareFootage) | Rental Service (planned), Work Order Service (planned) |
| `properties.deleted.v1` | `IPropertyDeleted` | `DeletePropertyHandler` -- soft delete, triggers unit cascade | **Unit Service (internal)**, Rental Service (planned) |
| `units.created.v1` | `IUnitCreated` | `CreateUnitHandler` -- new unit (fields: Code, UnitNumber, Category, BuildingId, Floor, SquareFootage, RoomCount) | Rental Service (planned), Work Order Service (planned) |
| `units.updated.v1` | `IUnitUpdated` | `UpdateUnitHandler` -- unit details changed (fields: Code, UnitNumber, Category, BuildingId, Floor, SquareFootage, RoomCount) | Rental Service (planned), Work Order Service (planned) |
| `units.deleted.v1` | `IUnitDeleted` | `DeleteUnitHandler` -- soft delete or cascade from property deletion | Rental Service (planned) |

**Internal Subscription**: The Property Service routes `properties.deleted.v1` to a durable local queue (`unit-cascade-delete`) to cascade-delete Units when a Property is deleted (ADR 0001).

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
