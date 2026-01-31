# Company Feature

## Overview
Manages legal business entities (companies/LLCs) within an organization. Companies serve as the ownership layer for properties and other assets.

## Features
- Create new companies within an organization
- Update company details
- Delete companies (soft delete)
- List companies for current organization
- Automatically create default company on organization registration

## Business Rules
- All companies are scoped to their organization (multi-tenancy via Marten)
- Company names are required
- Deleted companies cannot be modified
- Default company is created with the organization name on registration
- Each organization can have multiple companies for managing separate legal entities

## Technical Details
- Uses Marten event sourcing with `CompanyAggregate`
- Implements `ITenanted` for multi-tenancy
- Publishes integration events for company lifecycle
- Subscribes to `IOrganizationRegistered` to create default company
