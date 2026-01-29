# Auth Feature

## Overview
Authentication and authorization for the landlord portal using cookie-based authentication.

## Features
- User login and logout
- Signal-based authentication state management
- Route guards for protected pages
- User profile caching
- Computed properties for UI (userName, userInitials, etc.)

## Business Rules
- Authentication uses HTTP-only cookies (token managed by BFF)
- Authenticated users cannot access login/register pages
- Unauthenticated users are redirected to login
- User session data retrieved from BFF `/api/users/me` endpoint
