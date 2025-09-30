# Companies Service

## Purpose
Manage company profiles within organizations (addresses, bank accounts, legal info).

## Responsibilities
- CRUD for companies scoped to an organization
- Validation for unique constraints within org (e.g., registration numbers)
- Optional company-specific preferences/settings

## API
- `GET /organizations/{orgId}/companies`
- `POST /organizations/{orgId}/companies`
- `GET /organizations/{orgId}/companies/{companyId}`
- `PUT /organizations/{orgId}/companies/{companyId}`
- `DELETE /organizations/{orgId}/companies/{companyId}` (soft delete)

## Multitenancy
- All operations require orgId in the path
- Future: gateway can validate company-org relationship if needed

## Observability
- Metrics: company counts, CRUD rates
- Traces: CRUD operations, validations
- Audit: company changes, soft deletes
