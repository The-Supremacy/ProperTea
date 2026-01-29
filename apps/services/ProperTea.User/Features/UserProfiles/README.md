# User Profile Feature

## Overview
Manages user profiles and their lifecycle. User profiles are tenant-scoped and linked to external identity provider users.

## Features
- User profile creation from external authentication
- User activity tracking (last seen)
- Profile retrieval for current user
- Multi-tenant user isolation
- Integration with external identity provider (Zitadel)

## Business Rules
- User profiles are linked to external identity provider users
- Each user belongs to exactly one organization (tenant)
- User activity is tracked via last seen timestamps
- Profiles can be deactivated when their organization is deactivated
- External user IDs must be unique within the system
- Profile events are stored using event sourcing pattern
