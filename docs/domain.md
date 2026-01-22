# Product & Domain Overview

## Ubiquitous Language

### Property Management (The "Physical" Reality)
* **Property (Aggregate Root)**: The legal entity or complex (e.g., "Sunset Towers").
  - **Context**: Tenant-specific. Two organizations owning units in the same building will have *different* Property records.
* **Unit (Aggregate Root)**: The distinct physical space (e.g., "Apt 4B").
  - **Role**: Physical existence only.

### Rental Management (The "Commercial" Reality)
* **Rentable Unit**: The commercial view of a Unit.
  - **Base Rent**: The internal target price.
  - **Vacancy**: A computed period where the unit is free.
* **Block**: A manual reservation of time (e.g., Renovation, Maintenance).

### Location & Analytics (The "Global" Reality)
* **Place**: A normalized physical location (e.g., Google Place ID) shared across tenants.
* **Heatmap**: Aggregated analytics showing density of ProperTea units in a city.

### Marketing (Public Reality)
* **Publication**: A snapshot of a *Rentable Unit* exposed to the Market Portal.
* **Application**: A request from an Applicant.

## Primary User Flow
1. **Definition**: User creates `Property` and `Unit` in *Property Service*.
   - *System*: *Location Service* normalizes the address to a global `PlaceId`.
2. **Setup**: User marks Unit as "Rentable" in *Rental Service*.
3. **Marketing**: User clicks "Advertise". *Market Service* creates a **Publication**.
4. **Acquisition**: Applicant applies on Market Portal. Landlord accepts.
5. **Closing**: Contract signed. *Rental Service* closes the time slot.
