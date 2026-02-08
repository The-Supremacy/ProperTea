# Mobile Table Strategy Discussion

## TanStack Table Mobile Capabilities

TanStack Table is **highly capable** for mobile:
- ✅ **Responsive columns** - Hide/show columns based on viewport
- ✅ **Column ordering** - Reorder for mobile-first layout
- ✅ **Virtual scrolling** - Handle large datasets efficiently
- ✅ **Horizontal scroll** - Natural table experience on mobile
- ✅ **Sticky columns** - Pin important columns (e.g., name)
- ✅ **Expandable rows** - Show summary, expand for details
- ✅ **Full sorting/filtering** - All features work on mobile

## Three Approaches

### Approach 1: Cards Only (Current Implementation)
**What**: Desktop uses table, mobile uses custom card layout

**Pros:**
- Familiar mobile UX pattern
- Can optimize information density per card
- Easier to make touch-friendly
- Works well for simple entities

**Cons:**
- Loss of functionality (sorting columns, reordering)
- Duplication of templates/logic
- Users can't compare rows side-by-side
- Limited to what we design in the card

**Best for:** Simple lists, content-focused apps, public-facing sites

### Approach 2: Table Only (Simplified)
**What**: Same table on all devices, responsive design

**Pros:**
- Single template/logic
- Full functionality everywhere
- Users familiar with desktop can use mobile
- Power users get all features
- TanStack Table handles most responsive concerns

**Cons:**
- Requires horizontal scroll on narrow screens
- Text truncation may be needed
- Touch targets need careful sizing
- May feel "cramped" on phones

**Best for:** Data-heavy apps, power-user tools, admin interfaces

### Approach 3: User Toggle (Hybrid)
**What**: Offer both views, let user choose per-device

**Pros:**
- Best of both worlds
- User preference respected
- Power users can opt into table on mobile
- Casual users get cards
- Preference persisted per-device

**Cons:**
- Most complex to implement
- Both templates maintained
- Toggle UI adds clutter
- May confuse some users

**Best for:** Apps with diverse user base, complex entities

## Recommendation for ProperTea

**Use Approach 2 (Table Only)** with these refinements:

### Why Table Works for Property Management:
1. **Landlords are power users** - They want data, not simplified views
2. **Tablets are common** - 10"+ devices handle tables well
3. **Comparison matters** - Need to see multiple properties/companies
4. **Mobile usage is secondary** - Desktop/tablet are primary workflows
5. **Consistent UX** - No surprises switching devices

### Implementation Strategy:
```typescript
// Column configuration for mobile-first table
columns: [
  {
    id: 'name',
    header: 'Name',
    enableSorting: true,
    enableHiding: false, // Always visible
    size: 200,
    minSize: 150,
  },
  {
    id: 'status',
    header: 'Status',
    enableHiding: true,
    meta: { hideOnMobile: true }, // Hide by default on < 768px
    size: 120,
  },
  {
    id: 'createdAt',
    header: 'Created',
    enableHiding: true,
    meta: { hideOnMobile: false }, // Keep on mobile
    size: 150,
  },
  {
    id: 'actions',
    header: 'Actions',
    enableSorting: false,
    enableHiding: false,
    size: 80,
  },
];
```

### Mobile Optimizations:
1. **Responsive column visibility**
   - Always show: Name, Actions
   - Auto-hide on mobile: Status, Created (with toggle)
   - Priority-based hiding (hide least important first)

2. **Horizontal scroll with sticky column**
   ```css
   .table-container {
     overflow-x: auto;
     -webkit-overflow-scrolling: touch;
   }

   /* Sticky first column (name) */
   th:first-child, td:first-child {
     position: sticky;
     left: 0;
     background: var(--bg-card);
     z-index: 1;
   }
   ```

3. **Touch-friendly interactions**
   - Larger row height on mobile (min 48px)
   - Bigger tap targets for actions (44x44px minimum)
   - Swipe gestures for common actions (optional)

4. **Compact mode toggle**
   - Toolbar button: "Compact View"
   - Reduces padding, font size slightly
   - Shows more rows on mobile

5. **Expandable rows (future)**
   ```typescript
   {
     id: 'expander',
     header: '',
     cell: ({ row }) => (
       row.getCanExpand() ? (
         <button onClick={row.getToggleExpandedHandler()}>
           {row.getIsExpanded() ? '▼' : '▶'}
         </button>
       ) : null
     ),
   }
   ```

### Fallback to Cards
If user feedback shows table is too difficult on mobile:
- Add "Grid View" toggle in toolbar
- Use TanStack Table's row data as card content source
- Switch view client-side (no data refetch)
- Persist preference per-user

## Decision
**Start with Approach 2 (Table Only)** for ProperTea Landlord Portal:
- Implement responsive column hiding
- Add sticky first column
- Ensure 44px touch targets
- Monitor user feedback
- Add card toggle if needed (Phase 2)

**Rationale:**
- Landlords need full functionality on all devices
- Property management is data-intensive
- Simplifies implementation significantly
- TanStack Table already handles 90% of mobile concerns
- We can always add cards later if needed

## Implementation Checklist
- [ ] Configure responsive column visibility
- [ ] Implement sticky first column
- [ ] Ensure touch-friendly sizing (44px minimum)
- [ ] Add horizontal scroll indicators
- [ ] Test on real mobile devices (not just browser DevTools)
- [ ] Add column visibility toggle in toolbar
- [ ] Consider compact mode toggle
- [ ] Document mobile UX in user guide
