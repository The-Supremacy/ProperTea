# Domain Model

## Ubiquitous Language

Use these exact terms when naming classes, variables, events, and UI labels.

### Organization & Identity

| Term | Definition | Owned By |
|---|---|---|
| **Organization** | A ZITADEL-managed tenant. Isolation boundary for all data. | Organization Service |
| **Owner Organization** | The landlord/management org that owns properties. | -- |
| **Executor Organization** | A contractor org assigned to a Work Order. | Work Order Service |
| **User Profile** | Local representation of a ZITADEL user. Stores preferences, "Last Seen". | User Service |

### Property Management (Physical Reality)

| Term | Definition | Owned By |
|---|---|---|
| **Property** (Aggregate Root) | The legal entity or complex (building, estate). Has a unique Code per Company. | Property Service |
| **Building** (Child Entity of Property) | A physical structure within a Property. Has a unique Code per Property. Stored in the Property event stream. | Property Service |
| **Unit** (Aggregate Root) | A distinct physical space within a Property, optionally assigned to a Building. Has a unique Code per Property and a UnitCategory (Apartment, Commercial, Parking, Other). Holds `PropertyId`. Private house = 1 Property + 1 Unit (ADR 0001). | Property Service |

### Rental Management (Commercial Reality)

| Term | Definition | Owned By |
|---|---|---|
| **Rentable Unit** | The commercial view of a Unit. Tracks rentable status. | Rental Service |
| **Block** | A manual reservation of time (renovations, maintenance). Prevents renting. | Rental Service |
| **Company** (Aggregate Root) | A legal business entity (LLC) that owns properties. Has a unique Code per Organization. Multiple per Organization (ADR 0007). | Company Service |

### Maintenance & Operations (Actionable Reality)

| Term | Definition | Owned By |
|---|---|---|
| **Work Order** (Aggregate Root) | Primary entity for all maintenance and inspection tasks (ADR 0005). | Work Order Service |
| **Fault Report** | Work Order type: reactive maintenance for tenant-reported issues. | Work Order Service |
| **Rounds** | Work Order type: preventive/scheduled routine checks. | Work Order Service |
| **Property Inspection** | Work Order type: condition assessment (move-in/move-out). | Work Order Service |
| **Legal Audit** | Work Order type: statutory compliance check (fire safety, elevator certs). | Work Order Service |
| **Competence** | Skill category assigned to an Organization (e.g., Plumbing, Electrical). | Work Order Service |

## Primary User Flow

1. **Onboarding**: User registers via headless flow. Org + Admin created in ZITADEL and local state (ADR 0003).
2. **Definition**: User creates `Property` and `Unit` in Property Service.
3. **Setup**: User marks Unit as "Rentable" in Rental Service.
4. **Maintenance**: A Fault Report is created. Landlord assigns an Executor Organization.
5. **Execution**: Contractor views the Work Order on their dashboard and updates status.

## Aggregate Ownership Rules

- Each aggregate belongs to exactly one service.
- Cross-service references use integration events, never direct DB access.
- All aggregates are scoped to a tenant (organization) via Marten multi-tenancy.
