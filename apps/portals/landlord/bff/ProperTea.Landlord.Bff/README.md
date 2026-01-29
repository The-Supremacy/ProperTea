# Landlord BFF

## Overview
Backend for Frontend service that acts as a pass-through between the Angular frontend and backend microservices. Contains NO business logic.

## Features
- API endpoints tailored for landlord portal frontend
- DTO mapping between backend services and frontend
- Authentication and authorization enforcement
- Multiple backend service aggregation when needed
- Service discovery via .NET Aspire

## Business Rules
- **NO business logic** - pure pass-through/mapper only
- **NO direct database access** - all data comes from backend services
- **NO event publishing** - only backend services publish events
- All endpoints require authentication unless explicitly marked anonymous
- Uses typed HTTP clients to communicate with backend services
