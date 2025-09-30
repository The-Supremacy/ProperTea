# Landlord Portal (Frontend)

## Stack
- Next.js (App Router, TypeScript)
- TailwindCSS + MUI
- React Query for server state
- Minimal Zustand for UI/local state
- next-auth for auth (credentials with Identity Service; OIDC with Entra later)

## Architecture
- Acts as BFF for UI concerns but calls the Gateway for all backend APIs
- Tenant-aware routing: `/{organizationId}/...`
- Auth flow:
  - Credentials login against Identity Service
  - Store tokens in httpOnly cookies
  - Include access token when calling Gateway
- Preferences:
  - Store locally (localStorage) and sync via BFF bulk endpoints
  - Re-fetch on FE/BFF version change via `/config/version`

## Pages (examples)
- Organization switcher and dashboard
- Companies management
- Properties and listings
- Users and groups management
- Settings and feature flags (if exposed)

## Observability
- Frontend logs and error tracking (console + optional client-side tracing)
- Propagate correlation IDs via headers where possible

## Security
- Use HTTPS locally (Traefik/mkcert) and in production
- Strict CORS at Gateway
- Avoid exposing sensitive data in client-side storage
