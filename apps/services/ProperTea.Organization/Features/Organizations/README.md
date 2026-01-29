# Organization Feature

## Overview
Manages organization lifecycle, registration, and configuration. Organizations are the top-level tenant entities in the ProperTea platform.

## Features
- Organization registration with admin user creation
- Organization name availability checking
- Organization activation workflow
- Audit log tracking
- Integration with external identity provider (Zitadel)

## Business Rules
- Organization names must be unique across the platform
- Organizations start in `Pending` status and must be activated before use
- Each organization is linked to an external identity provider organization
- Subscription tiers control feature access and resource limits
- Organization events are stored using event sourcing pattern
