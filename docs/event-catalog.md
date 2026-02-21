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
| `properties.created.v2` | `IPropertyCreated` | `CreatePropertyHandler` -- new property (fields: Code, Name, Address, CompanyId) | Rental Service (planned), Work Order Service (planned) |
| `properties.updated.v2` | `IPropertyUpdated` | `UpdatePropertyHandler` -- property details changed (fields: CompanyId, Code, Name, Address) | Rental Service (planned), Work Order Service (planned) |
| `properties.deleted.v1` | `IPropertyDeleted` | `DeletePropertyHandler` -- soft delete (blocked if active buildings or units exist) | Rental Service (planned) |
| `buildings.created.v1` | `IBuildingCreated` | `CreateBuildingHandler` -- new building (fields: Code, Name, Address) | (planned) |
| `buildings.updated.v1` | `IBuildingUpdated` | `UpdateBuildingHandler` -- building details changed (fields: Code, Name, Address) | (planned) |
| `buildings.deleted.v1` | `IBuildingDeleted` | `DeleteBuildingHandler` -- soft delete (blocked if active units exist) | (planned) |
| `units.created.v2` | `IUnitCreated` | `CreateUnitHandler` -- new unit (fields: Code, UnitReference, Category, BuildingId, EntranceId, Address, Floor) | Rental Service (planned), Work Order Service (planned) |
| `units.updated.v2` | `IUnitUpdated` | `UpdateUnitHandler` -- unit details changed (fields: Code, UnitReference, Category, BuildingId, EntranceId, Address, Floor) | Rental Service (planned), Work Order Service (planned) |
| `units.deleted.v1` | `IUnitDeleted` | `DeleteUnitHandler` -- soft delete | Rental Service (planned) |

**Deletion Strategy**: Cross-aggregate children block parent deletion (returning a 422 with a hint about what data must be removed first). Only intra-aggregate children (e.g. entrances within a building) are cascade-deleted with the parent, **unless** they are referenced by other aggregates — in that case, removal is blocked. See `PROPERTY_HAS_ACTIVE_BUILDINGS`, `PROPERTY_HAS_ACTIVE_UNITS`, `BUILDING_HAS_ACTIVE_UNITS`, and `BUILDING_ENTRANCE_HAS_ACTIVE_UNITS` error codes.

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
