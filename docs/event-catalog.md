# Event Catalog

This document tracks all integration events exchanged between services via Wolverine and RabbitMQ.
**Source of Truth**: The interface contracts in `/shared/ProperTea.Contracts` are the strict schema definitions.

## Exchange: `organization.events`
**Publisher**: Organization Service (`ProperTea.Organization`)
**Transport**: RabbitMQ (Fanout)

| Message Identity | Payload Interface | Trigger | Subscribers |
| :--- | :--- | :--- | :--- |
| `organizations.registered.v1` | `IOrganizationRegistered` | **RegisterOrganizationHandler**<br>When a new tenant creates an organization. | **User Service**<br>(Configured in `WolverineConfiguration.cs`) |
| `organizations.identity-updated.v1` | `IOrganizationIdentityUpdated` | **UpdateIdentityHandler**<br>When an org is renamed or slug changes. | **User Service** |
| `organizations.deactivated.v1` | `IOrganizationDeactivated` | **DeactivateHandler**<br>When a tenant is manually deactivated. | **User Service** |
| `organizations.activated.v1` | `IOrganizationActivated` | **ActivateHandler**<br>When a tenant is reactivated. | **User Service** |

## Exchange: `user.events`
**Publisher**: User Service (`ProperTea.User`)
**Transport**: RabbitMQ (Topic)

| Message Identity | Payload Interface | Trigger | Subscribers |
| :--- | :--- | :--- | :--- |
| `user.profile-created.v1` | `IUserProfileCreated` | **CreateProfileHandler**<br>First time a user logs in via Keycloak. | *None currently configured* |

## Usage Guidelines

### 1. Naming Conventions
* **Identity**: `{entity}.{action}.v{version}` (e.g., `organizations.registered.v1`).
* **Routing Keys**: Use the exact Message Identity string as the routing key.

### 2. Contract Enforcement
* Producers **must** implement the interface from `ProperTea.Contracts`.
* Consumers **should** duplicate the record definition locally (Vertical Slice) but ensure property names match the contract interface.
* See `apps/services/ProperTea.User/Features/UserProfiles/External/OrganizationIntegrationEvents.cs` for an example of a consumer-side definition.

### 3. Adding New Events
1.  Define the interface in `shared/ProperTea.Contracts`.
2.  Implement the record in the Producer service with `[MessageIdentity]`.
3.  Add the mapping to `WolverineMessagingConfiguration` in the Producer.
4.  Update this catalog.
