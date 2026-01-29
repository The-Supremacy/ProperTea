export const ORGANIZATION_API = {
  BASE: '/api/organizations',
  CHECK_AVAILABILITY: '/api/organizations/check-availability',
  AUDIT_LOG: (id: string) => `/api/organizations/${id}/audit-log`,
} as const;

export const ORGANIZATION_VALIDATION = {
  NAME_MIN_LENGTH: 2,
  NAME_MAX_LENGTH: 100,
  NAME_DEBOUNCE_MS: 500,
} as const;

export const CREATEORG_PASSWORD_REQUIREMENTSS = {
  MIN_LENGTH: 8,
  MAX_LENGTH: 100,
  MUST_HAVE_LOWERCASE: true,
  MUST_HAVE_UPPERCASE: true,
  MUST_HAVE_NUMBER: true,
  MUST_HAVE_SPECIAL: true,
} as const;

export const ORGANIZATION_ROUTES = {
  REGISTER: '/organizations/register',
  DASHBOARD: '/dashboard',
} as const;
