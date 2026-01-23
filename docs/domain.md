# Product & Domain Overview

## Ubiquitous Language

### Property Management (The "Physical" Reality)
* **Property (Aggregate Root)**: The legal entity or complex.
* **Unit (Aggregate Root)**: The distinct physical space.

### Maintenance & Operations (The "Actionable" Reality)
* **Work Order (Aggregate Root)**: The primary entity for maintenance and inspections.
  - **Fault Report**: Reactive maintenance for tenant-reported issues.
  - **Rounds**: Preventive maintenance or scheduled routine checks.
  - **Property Inspection**: Condition assessments (e.g., Move-in/Move-out).
  - **Legal Audit**: Statutory compliance checks (e.g., Fire safety, Elevator certs).
* **Competence**: Skill categories assigned to an Organization (e.g., Plumbing, Electrical).

### Rental Management (The "Commercial" Reality)
* **Rentable Unit**: The commercial view of a Unit.
* **Block**: A manual reservation of time for renovations or maintenance.

### Visibility & Roles
* **Owner Organization**: The landlord/management org owning the property.
* **Executor Organization**: The contractor org assigned to a Work Order.

## Primary User Flow
1. **Onboarding**: User registers via the Headless flow. Org and Admin are created in ZITADEL and local state.
2. **Definition**: User creates `Property` and `Unit` in *Property Service*.
3. **Setup**: User marks Unit as "Rentable" in *Rental Service*.
4. **Maintenance**: A **Fault Report** is created. Landlord assigns an **Executor Organization**.
5. **Execution**: Contractor views the **Work Order** on their dashboard and updates status.
