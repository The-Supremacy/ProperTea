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
| **Property** (Aggregate Root) | The legal entity or complex (building, estate). Has a unique Code per Company (max 10 chars, `[A-Z0-9]`). Has a structured `Address` (Country, City, ZipCode, StreetAddress). | Property Service |
| **Building** (Aggregate Root) | A physical structure within a Property. Has a unique Code per Property (max 5 chars, `[A-Z0-9]`). Has a structured `Address` (inherits from Property if not set). Contains `Entrance` child value objects (each with a Guid Id, Code, Name) that represent access points to the building. | Property Service |
| **Entrance** (Value Object) | An access point to a Building (e.g., staircase, entrance door). Belongs to exactly one Building. Has a unique `Code` per Building (max 5 chars, `[A-Z0-9]`) and a `Name`. Units in an Apartment building may reference an Entrance by `EntranceId`. | Property Service |
| **Unit** (Aggregate Root) | A distinct physical space. Has a unique `Code` per Property (max 10 chars, `[A-Z0-9]`) and a generated `UnitReference` (`{CompanyCode}-{PropertyCode}-{BuildingCode?}-{UnitCode}`). Has a structured `Address` (inherits from Building or Property). Has a `UnitCategory`: **Apartment** (requires `BuildingId`), **Commercial** (optional `BuildingId`), **Parking** (optional `BuildingId`), **House** (must not have `BuildingId` — represents a standalone house as 1 Property + 1 Unit per ADR 0001), **Other** (optional `BuildingId`). May optionally reference a Building `Entrance` via `EntranceId`. | Property Service |

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
