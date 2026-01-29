# Core Module

## Overview
Shared application services, models, and utilities used across all features.

## Features
- HTTP interceptors for cross-cutting concerns
- Global error handling and translation
- Application configuration management
- Shared error models (ProblemDetails, ValidationError)

## Business Rules
- 401 errors trigger automatic redirect to login page
- API errors are translated to user-friendly messages
- Cookie-based authentication (no Bearer tokens needed)
- Distributed tracing handled by OpenTelemetry (TraceId)
