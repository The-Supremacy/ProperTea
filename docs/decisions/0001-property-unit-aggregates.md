# 0001: Separation of Property and Unit Aggregates

**Status**: Accepted
**Date**: 2026-01-16
**Context**:
We need to model real estate assets ranging from single-family homes to 500-unit apartment complexes.
Modeling the `Unit` (Apartment) as a child entity inside the `Property` aggregate causes performance locking issues and massive document sizes.

**Decision**:
1. **Property** is an Aggregate Root representing the management context.
2. **Unit** is a separate Aggregate Root representing the distinct physical asset.
3. **Unit** holds a reference (`PropertyId`) to its parent.

**Consequences**:
- **Performance**: We can update a Unit (e.g., Inventory Inspection) without loading the Property.
- **Consistency**: Deleting a Property requires an eventual consistency process to remove child Units.
- **Private Houses**: Modeled as a Property containing exactly 1 Unit to maintain architectural uniformity.
