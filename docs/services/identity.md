# Identity Service

## Purpose
Authenticate users (credentials + MFA + external providers) and issue JWT tokens to clients.

## Responsibilities
- Credentials auth, MFA (TOTP), lockout policies
- External OIDC providers (e.g., Entra)
- Issue access (10m) and refresh (30d) tokens
- Password reset, email verification
- Session and token revocation; login history

## Core APIs
- POST /auth/login, /auth/logout, /auth/refresh
- POST /auth/register, /auth/verify-email
- POST /auth/forgot-password, /auth/reset-password
- MFA: /auth/mfa/setup, /auth/mfa/verify, /auth/mfa/disable
- /.well-known/jwks, /.well-known/openid-configuration
- Health: /health/live, /health/ready

## Security
- RSA-256 signing; quarterly rotation with overlap
- httpOnly cookies recommended for FE
- Brute force protection and anomaly detection

## Observability
- Metrics: auth attempts, tokens issued, lockouts
- Traces for auth flows; audit security events
