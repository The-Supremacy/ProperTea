# Property Service

## Purpose
Manage properties and listings within organizations.

## Responsibilities
- CRUD for properties and related data
- Listing workflow: draft → approved → published
- Emit events for search indexing
- Enforce resource-state rules in service (e.g., cannot edit finalized)

## API
- `GET /organizations/{orgId}/property/listings`
- `POST /organizations/{orgId}/property/listings`
- `GET /organizations/{orgId}/property/listings/{listingId}`
- `PUT /organizations/{orgId}/property/listings/{listingId}`
- `POST /organizations/{orgId}/property/listings/{listingId}/approve`
- `POST /organizations/{orgId}/property/listings/{listingId}/publish`
- (Optional) Company-context routes when needed:
  - `/organizations/{orgId}/companies/{companyId}/property/listings`

## Events
- `property.listing.created.v1`
- `property.listing.updated.v1`
- `property.listing.approved.v1`
- `property.listing.published.v1`

## Authorization
- Local checks using internal token permissions (property domain)
- If permission is company-scoped, resolve the listing’s `companyId` (or read from path/header) and verify it is allowed per token `permissionScopes`
- Recheck critical transitions if required

## Observability
- Metrics: listings by state, transition rates
- Traces: listing workflows
- Audit: listing approvals and publishes
