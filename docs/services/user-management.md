# User Management Service

## Purpose
Manage user profiles, organization memberships, user groups, invitations, and preferences.

## Responsibilities
- User profiles: name, avatar, contact info
- Memberships: users in organizations; groups per org
- Invitations: invite flows and acceptance
- Preferences: global defaults and per-organization, BFF sync endpoints
- Provide user’s organizations list for gateway checks

## API
- `GET /users/{userId}/profile`
- `PUT /users/{userId}/profile`
- `GET /users/{userId}/organizations`
- `GET /org/{orgId}/groups`
- `POST /org/{orgId}/groups`
- `POST /org/{orgId}/groups/{groupId}/members`
- `DELETE /org/{orgId}/groups/{groupId}/members/{userId}`
- `POST /org/{orgId}/invitations`
- `POST /org/{orgId}/invitations/{inviteId}/accept`
- Preferences:
  - `GET /users/{userId}/preferences` (bulk)
  - `PUT /users/{userId}/preferences` (bulk, ETag)

## Preferences Model
- Namespaced blobs for FE: `ui.landlord`, `grid.defaults`, etc.
- FE stores preferences locally (localStorage) and syncs with BFF
- Re-fetch on FE/BFF version changes (via `/config/version`)

## Security
- Gateway forwards internal token
- Service revalidates permission for sensitive changes (e.g., group edits)

## Observability
- Metrics: invitation success rate, preference sync counts
- Traces: profile read/write, membership changes
- Audit: group changes, invitations, preference updates
