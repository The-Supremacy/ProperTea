# 0002: Separation of Rental, Market, and Property Services

**Status**: Accepted
**Date**: 2026-01-16
**Context**:
We need to distinguish between "Physical Existence" (Property), "Internal Commercial Potential" (Rental), and "Public Advertising" (Market).
Merging these concerns creates rigid coupling (e.g., you can't have a vacancy without publishing it).

**Decision**:
1. **Property Service**: Owns physical data (Walls, Inventory).
2. **Rental Service**:
   - Owns the **Internal Schedule** and **Base Financials**.
   - Tracks "Rentable" status and "Blocks" (e.g., Renovations).
   - Calculates "Lost Rent" based on *Base Rent* vs *Actual Contract*.
3. **Market Service**:
   - Owns the **External Publication**.
   - Manages the Applicant funnel.

**Consequences**:
- **Sync**: Market Service listens to Rental Service. If a unit is blocked (Renovation), the Publication is automatically paused.
- **Efficiency**: Landlords can manage internal inventory and pricing without spamming the public market.
