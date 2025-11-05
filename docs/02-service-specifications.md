# Service Specifications

**Version:** 1.1.0  
**Last Updated:** October 30, 2025  
**Status:** MVP 1 Specification - Revised

---

> **Note on Implementation Details:** The database schemas, aggregates, and detailed code samples in this document are for initial guidance, particularly for AI-assisted development. They are subject to change during implementation. The final, authoritative documentation for each service will be the code itself, its comments, and its OpenAPI specification.

## Table of Contents

1. [Core Services](#core-services)
   - [Identity Service](#identity-service)
   - [Contact Service](#contact-service)
   - [Organization Service](#organization-service)
   - [Permission Service](#permission-service)
   - [Preferences Service](#preferences-service)
2. [Domain Services](#domain-services)
   - [Property Base Service](#property-base-service)
   - [Rental Management Service](#vacancy-service)
   - [Market Service](#market-service)
   - [Lease Service](#lease-service)
   - [Inspection Service](#inspection-service)
   - [Maintenance Service](#maintenance-service)
3. [Infrastructure Services](#infrastructure-services)
   - [Search Service](#search-service)
4. [BFF Services](#bff-services)
   - [Landlord BFF](#landlord-bff)
   - [Tenant BFF](#tenant-bff)
   - [Market BFF](#market-bff)

---

## Core Services

### Identity Service

**Responsibility:** User authentication, JWT generation, external identity provider integration.

**Technology:** ASP.NET Core, .NET Identity, PostgreSQL

**Database:** `identity_db`

#### API Endpoints

**Token Management:**
```
POST   /api/token/login              - Authenticate user, return JWT
POST   /api/token/reissue            - Reissue expired JWT
POST   /api/token/external/{provider} - Initiate external provider login
POST   /api/token/external/callback  - Handle OAuth callback
```

**User Management:**
```
POST   /api/auth/register            - Register new user
POST   /api/auth/forgot-password     - Initiate password reset
POST   /api/auth/reset-password      - Complete password reset
POST   /api/auth/change-password     - Change password (authenticated)
POST   /api/auth/confirm-email       - Confirm email address
```

#### Data Models

**User (Aggregate Root):**
```csharp
public class ProperTeaUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; }
}
```

**ExternalLogin (Entity):**
```csharp
public class ExternalLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } // "Google", "Entra"
    public string ProviderKey { get; set; }
    public string ProviderDisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public virtual ProperTeaUser User { get; set; }
}
```

#### Integration Events Published

```csharp
public record UserCreated(Guid UserId, string Email, DateTime CreatedAt);
public record UserActivated(Guid UserId);
public record UserDeactivated(Guid UserId);
public record UserDeleted(Guid UserId);
```

#### Integration Events Consumed

None (Identity is the source of truth for users)

#### Worker Responsibilities

**Identity.Worker:**
- Processes outbox messages (publishes integration events to message broker)
- Future: May handle other background tasks like user cleanup

#### Database Schema

```sql
-- Users (from ASP.NET Identity)
CREATE TABLE users (
    id UUID PRIMARY KEY,
    user_name VARCHAR(256) NOT NULL UNIQUE,
    email VARCHAR(256) NOT NULL,
    email_confirmed BOOLEAN DEFAULT FALSE,
    password_hash VARCHAR(500),
    security_stamp VARCHAR(500),
    phone_number VARCHAR(50),
    phone_number_confirmed BOOLEAN DEFAULT FALSE,
    two_factor_enabled BOOLEAN DEFAULT FALSE,
    lockout_end TIMESTAMP,
    lockout_enabled BOOLEAN DEFAULT TRUE,
    access_failed_count INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP,
    last_login_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);

-- External logins
CREATE TABLE external_logins (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    provider VARCHAR(50) NOT NULL,
    provider_key VARCHAR(200) NOT NULL,
    provider_display_name VARCHAR(200),
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(provider, provider_key)
);

-- Sagas
CREATE TABLE user_registration_sagas (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    email VARCHAR(256) NOT NULL,
    state VARCHAR(50) NOT NULL, -- Started, ContactCreated, GroupsAssigned, Completed, Failed
    steps JSONB NOT NULL,
    data JSONB,
    retry_count INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    completed_at TIMESTAMP
);
```

#### Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=identity;Username=dev;Password=dev",
    "Kafka": "kafka:9092"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-at-least-32-chars",
    "Issuer": "ProperTea.Identity",
    "Audience": "ProperTea",
    "ExpiryMinutes": 15
  },
  "IdentitySettings": {
    "Password": {
      "RequireDigit": true,
      "RequiredLength": 8,
      "RequireNonAlphanumeric": true,
      "RequireUppercase": true,
      "RequireLowercase": true
    },
    "Lockout": {
      "DefaultLockoutTimeSpan": "00:30:00",
      "MaxFailedAccessAttempts": 5
    },
    "EmailConfirmation": {
      "Required": false
    }
  },
  "GoogleAuthSettings": {
    "ClientId": "",
    "ClientSecret": ""
  },
  "EntraIdSettings": {
    "Enabled": false,
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

---

### Contact Service

**Responsibility:** Personal profiles, organization-user profiles, GDPR compliance.

**Technology:** ASP.NET Core, Entity Framework Core, PostgreSQL

**Database:** `contact_db`

#### API Endpoints

```
GET    /api/contacts/{contactId}           - Get contact by ID
GET    /api/contacts/by-user/{userId}      - Get contact by user ID
POST   /api/contacts                       - Create contact
PUT    /api/contacts/{contactId}           - Update contact
DELETE /api/contacts/{contactId}           - Delete contact (GDPR)

POST   /api/contacts/{contactId}/invite    - Generate invite code for contact
POST   /api/contacts/{contactId}/connect-user - Connect existing user to contact
GET    /api/contacts/by-invite/{inviteCode} - Get contact by invite code

GET    /api/contacts/{contactId}/org-profiles              - Get org profiles
POST   /api/contacts/{contactId}/org-profiles              - Create org profile
PUT    /api/contacts/{contactId}/org-profiles/{profileId}  - Update org profile
DELETE /api/contacts/{contactId}/org-profiles/{profileId}  - Delete org profile

POST   /api/contacts/delete-request        - Request user data deletion
GET    /api/contacts/export?userId={guid}  - Export user data (GDPR)
```

#### Data Models

**PersonalProfile (Aggregate Root):**
```csharp
public class PersonalProfile : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; } // Nullable - contact may not have user account yet
    public string FullName { get; private set; }
    public string? PersonalPhone { get; private set; }
    public string? PersonalEmail { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public string? InviteCode { get; private set; }
    public DateTime? InviteCodeExpiry { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Domain methods
    public void UpdateProfile(string fullName, string? phone, string? email)
    {
        FullName = fullName;
        PersonalPhone = phone;
        PersonalEmail = email;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ContactUpdatedEvent(Id, UserId));
    }
    
    public string GenerateInviteCode()
    {
        InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(); // 8-char code
        InviteCodeExpiry = DateTime.UtcNow.AddDays(7); // 7-day expiry
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ContactInviteGeneratedEvent(Id, InviteCode, PersonalEmail));
        return InviteCode;
    }
    
    public void ConnectUser(Guid userId)
    {
        if (UserId.HasValue)
            throw new InvalidOperationException("Contact already has a user account");
            
        UserId = userId;
        InviteCode = null; // Clear invite code once used
        InviteCodeExpiry = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ContactUserConnectedEvent(Id, userId));
    }
    
    public void AnonymizeForGDPR()
    {
        FullName = "DELETED USER";
        PersonalEmail = $"deleted_{Id}@deleted.local";
        PersonalPhone = null;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ContactDeletedEvent(UserId));
    }
}
```

**OrganizationUserProfile (Entity):**
```csharp
public class OrganizationUserProfile
{
    public Guid Id { get; set; }
    public Guid PersonalProfileId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? WorkPhone { get; set; }
    public string? WorkEmail { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public virtual PersonalProfile PersonalProfile { get; set; }
}
```

#### Integration Events Published

```csharp
public record ContactCreated(Guid ContactId, Guid UserId, string FullName);
public record ContactUpdated(Guid ContactId, Guid UserId);
public record ContactDeleted(Guid UserId);
```

#### Integration Events Consumed

```csharp
public record UserCreated(Guid UserId, string Email, DateTime CreatedAt);
```

#### Worker Responsibilities

**Contact.Worker:**
- Listens to `UserCreated` event
- Creates `PersonalProfile` for new users
- Publishes `ContactCreated` event
- Orchestrates `DeletionRequestSaga` for GDPR compliance

#### Database Schema

```sql
CREATE TABLE personal_profiles (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL UNIQUE,
    full_name VARCHAR(200) NOT NULL,
    personal_phone VARCHAR(50),
    personal_email VARCHAR(256),
    date_of_birth DATE,
    invite_code VARCHAR(10),
    invite_code_expiry TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE organization_user_profiles (
    id UUID PRIMARY KEY,
    personal_profile_id UUID NOT NULL REFERENCES personal_profiles(id),
    organization_id UUID NOT NULL,
    work_phone VARCHAR(50),
    work_email VARCHAR(256),
    department VARCHAR(100),
    title VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(personal_profile_id, organization_id)
);

CREATE TABLE deletion_request_sagas (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    state VARCHAR(50) NOT NULL,
    blocking_reasons JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    completed_at TIMESTAMP
);
```

---

### Organization Service

**Responsibility:** Organizations, companies (business units), user-org membership.

**Technology:** ASP.NET Core, Entity Framework Core, PostgreSQL

**Database:** `organization_db`

#### API Endpoints

**Organizations:**
```
GET    /api/organizations                           - List organizations
GET    /api/organizations/{orgId}                   - Get organization
POST   /api/organizations                           - Create organization
PUT    /api/organizations/{orgId}                   - Update organization
DELETE /api/organizations/{orgId}                   - Delete organization
POST   /api/organizations/{orgId}/approve           - Approve organization

GET    /api/organizations/by-subdomain/{subdomain}  - Resolve org from subdomain
```

**Companies:**
```
GET    /api/organizations/{orgId}/companies         - List companies in org
GET    /api/companies/{companyId}                   - Get company
POST   /api/companies                               - Create company
PUT    /api/companies/{companyId}                   - Update company
DELETE /api/companies/{companyId}                   - Delete company
```

**User Membership:**
```
GET    /api/organizations/{orgId}/users             - List users in org
POST   /api/organizations/{orgId}/users/{userId}    - Add user to org
DELETE /api/organizations/{orgId}/users/{userId}    - Remove user from org
```

#### Data Models

**Organization (Aggregate Root):**
```csharp
public class Organization : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid OwnerUserId { get; private set; } // Organization creator - has immediate full access
    public string? Subdomain { get; private set; }
    public string? Logo { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public OrganizationStatus Status { get; private set; } // Pending, Approved, Suspended
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public virtual ICollection<Company> Companies { get; set; }
    public virtual ICollection<UserOrganization> UserOrganizations { get; set; }
    
    public static Organization Create(string name, Guid ownerUserId, string? subdomain = null)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerUserId = ownerUserId,
            Subdomain = subdomain,
            Status = OrganizationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        org.AddDomainEvent(new OrganizationCreatedEvent(org.Id, name, ownerUserId));
        return org;
    }
    
    public bool IsOwner(Guid userId) => OwnerUserId == userId;
    
    public void Approve()
    {
        if (Status != OrganizationStatus.Pending)
            throw new InvalidOperationException("Only pending organizations can be approved");
        
        Status = OrganizationStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new OrganizationApprovedEvent(Id));
    }
}
```

**Company (Entity):**
```csharp
public class Company
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? BankAccount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public virtual Organization Organization { get; set; }
}
```

**UserOrganization (Entity):**
```csharp
public class UserOrganization
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    
    public virtual Organization Organization { get; set; }
}
```

#### Integration Events Published

```csharp
public record OrganizationCreated(Guid OrganizationId, string Name);
public record OrganizationApproved(Guid OrganizationId);
public record CompanyCreated(Guid CompanyId, Guid OrganizationId, string Name);
public record UserAddedToOrganization(Guid UserId, Guid OrganizationId);
```

#### Integration Events Consumed

None

#### Worker Responsibilities

**Organization.Worker:**
- Orchestrates `OrganizationCreationSaga` (creates default company)
- Listens to organization lifecycle events

#### Database Schema

```sql
CREATE TABLE organizations (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    subdomain VARCHAR(100) UNIQUE,
    logo VARCHAR(500),
    phone VARCHAR(50),
    address VARCHAR(500),
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE companies (
    id UUID PRIMARY KEY,
    organization_id UUID NOT NULL REFERENCES organizations(id),
    name VARCHAR(200) NOT NULL,
    registration_number VARCHAR(100),
    vat_number VARCHAR(100),
    bank_account VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE user_organizations (
    organization_id UUID REFERENCES organizations(id),
    user_id UUID NOT NULL,
    joined_at TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (organization_id, user_id)
);
```

---

### Permission Service

**Responsibility:** Groups, permissions, authorization checks, permission caching.

**Technology:** ASP.NET Core, Entity Framework Core, PostgreSQL, Redis (cache)

**Database:** `permission_db`

#### API Endpoints

**Permission Definitions:**
```
GET    /api/permissions/definitions           - List all permissions
POST   /api/permissions/definitions/refresh   - Force refresh from services
```

**Groups:**
```
GET    /api/permissions/groups?orgId={guid}              - List groups
GET    /api/permissions/groups/{groupId}                 - Get group
POST   /api/permissions/groups                           - Create group
PUT    /api/permissions/groups/{groupId}                 - Update group
DELETE /api/permissions/groups/{groupId}                 - Delete group

GET    /api/permissions/groups/{groupId}/members         - List group members
POST   /api/permissions/groups/{groupId}/members         - Add user to group
DELETE /api/permissions/groups/{groupId}/members/{userId} - Remove from group

GET    /api/permissions/groups/{groupId}/permissions          - List group permissions
POST   /api/permissions/groups/{groupId}/permissions          - Assign permissions
DELETE /api/permissions/groups/{groupId}/permissions/{permKey} - Remove permission
```

**User Permissions:**
```
GET    /api/permissions/user/{userId}/org/{orgId}  - Get user permissions for org
POST   /api/permissions/check                      - Check if user has permission
```

#### Data Models

**Group (Aggregate Root):**
```csharp
public class Group : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public virtual ICollection<GroupMember> Members { get; set; }
    public virtual ICollection<GroupPermission> Permissions { get; set; }
    
    public void AssignPermissions(List<string> permissionKeys)
    {
        // Clear existing
        Permissions.Clear();
        
        // Add new
        foreach (var key in permissionKeys)
        {
            Permissions.Add(new GroupPermission { PermissionKey = key });
        }
        
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new GroupPermissionsChangedEvent(Id, permissionKeys));
    }
}
```

**PermissionDefinition (Entity):**
```csharp
public class PermissionDefinition
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; }
    public string PermissionKey { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### Integration Events Published

```csharp
public record GroupCreated(Guid GroupId, Guid OrganizationId, string Name);
public record GroupPermissionsChanged(Guid GroupId, List<string> Permissions);
public record UserPermissionsChanged(Guid UserId, int NewCacheVersion);
```

#### Integration Events Consumed

```csharp
public record OrganizationCreated(Guid OrganizationId, string Name);
public record PermissionsRegistered(string ServiceName, List<PermissionDefinition> Permissions);
```

#### Worker Responsibilities

**Permission.Worker:**
- Listens to `OrganizationCreated` → seeds default groups (Administrator, User)
- Listens to `PermissionsRegistered` → updates permission definitions
- Publishes `UserPermissionsChanged` when groups/permissions change

#### Database Schema

```sql
CREATE TABLE permission_definitions (
    id UUID PRIMARY KEY,
    service_name VARCHAR(100) NOT NULL,
    permission_key VARCHAR(200) NOT NULL UNIQUE,
    description VARCHAR(500),
    category VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE groups (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    organization_id UUID NOT NULL,
    company_id UUID,
    description VARCHAR(500),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE group_members (
    group_id UUID REFERENCES groups(id),
    user_id UUID NOT NULL,
    assigned_at TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (group_id, user_id)
);

CREATE TABLE group_permissions (
    group_id UUID REFERENCES groups(id),
    permission_key VARCHAR(200) NOT NULL,
    granted_at TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (group_id, permission_key)
);

-- Permission cache versions (in Redis, shown for reference)
-- Key: "permissions:version:{userId}" -> INT
-- Key: "permissions:user:{userId}:org:{orgId}:version:{version}" -> JSON (permissions list)
```

---

### Preferences Service

**Responsibility:** User UI preferences per portal and organization.

**Technology:** ASP.NET Core, Entity Framework Core, PostgreSQL

**Database:** `preferences_db`

#### API Endpoints

```
GET    /api/preferences?userId={guid}&portal={name}&orgId={guid}  - Get preferences
POST   /api/preferences                                           - Create/update preference
DELETE /api/preferences/{preferenceId}                            - Delete preference
```

#### Data Models

**UserPreference (Entity):**
```csharp
public class UserPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Portal { get; set; } // null = global, "Landlord"/"Tenant"/"Market"
    public Guid? OrganizationId { get; set; } // null = global, set = org-specific
    public string Key { get; set; } // "theme", "language", "columns.leases"
    public string Value { get; set; } // JSON blob
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### Integration Events

None (simple CRUD service)

#### Database Schema

```sql
CREATE TABLE user_preferences (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    portal VARCHAR(50),
    organization_id UUID,
    key VARCHAR(200) NOT NULL,
    value TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id, portal, organization_id, key)
);

CREATE INDEX idx_preferences_user ON user_preferences(user_id);
CREATE INDEX idx_preferences_lookup ON user_preferences(user_id, portal, organization_id);
```

---

## Domain Services

*(Continuing in next part due to length...)*

### Property Base Service

**Responsibility:** Manages the physical structure of properties, including buildings and the individual 'rentable units' within them (e.g., apartments, storage rooms, parking spaces). It **does not** manage the concept of a lease-ready `RentalObject`. Instead, it publishes integration events when a new rentable unit is defined.

**Technology:** ASP.NET Core, Entity Framework Core, PostgreSQL

**Database:** `property_db` (Structure to be finalized during implementation)

#### API Endpoints

```
GET    /api/properties/{propertyId}/units   - List all rentable units for a property
POST   /api/properties/{propertyId}/units   - Create a new rentable unit (e.g., an apartment)
```

#### Data Models (High-Level Concept)

**Property (Aggregate Root):**
- Represents a physical real estate asset (e.g., "Main Street 123").
- Contains a collection of `RentableUnit` entities.

**RentableUnit (Entity):**
- Represents a specific, physically distinct space that can be rented (e.g., "Apartment #101", "Parking Space #P2", "Storage Unit #S5").
- When a `RentableUnit` is created, it triggers an integration event.

#### Integration Events Published

```csharp
// Published when a new apartment, parking space, etc., is created.
public record RentableUnitCreated(
    Guid UnitId,
    Guid PropertyId,
    string UnitType, // "Apartment", "Parking", "Storage"
    Dictionary<string, string> Attributes // e.g., { "Area": "50sqm", "Rooms": "2" }
);
```

---

### Rental Management Service

**Responsibility:** Consumes `RentableUnitCreated` events to create and manage the `RentalObject` aggregate. A `RentalObject` is the commercial representation of a rentable unit, containing details about pricing, availability, and vacancy periods. This service is responsible for the lifecycle of a leasable entity.

**Technology:** ASP.NET Core, Entity Framework Core, PostgreSQL

**Database:** `rental_management_db`

#### API Endpoints

```
GET    /api/rental-objects?propertyId={guid}   - List rental objects for a property
GET    /api/rental-objects/{rentalObjectId}    - Get rental object details
POST   /api/rental-objects                     - Create rental object
PUT    /api/rental-objects/{rentalObjectId}    - Update rental object
DELETE /api/rental-objects/{rentalObjectId}    - Delete rental object
POST   /api/rental-objects/{rentalObjectId}/publish - Publish rental object
```

#### Data Models

**RentalObject (Aggregate Root):**
```csharp
public class RentalObject : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid BuildingId { get; private set; }
    public string ObjectNumber { get; private set; }
    public RentalObjectType Type { get; private set; } // Apartment, Parking, Commercial, Other
    public int? Floor { get; private set; }
    public string? Entrance { get; private set; }
    public decimal? Area { get; private set; }
    public int? Bedrooms { get; private set; }
    public int? Bathrooms { get; private set; }
    public DateTime? AvailableFrom { get; private set; }
    public DateTime? AvailableTo { get; private set; }
    
    public virtual ICollection<Room> Rooms { get; set; }
    
    public void PublishToMarket()
    {
        AddDomainEvent(new PublicationRequestedEvent(Id, AvailableFrom, AvailableTo));
    }
}
```

#### Integration Events Published

```csharp
public record RentalObjectCreated(Guid RentalObjectId, Guid PropertyId, RentalObjectData Data);
public record RentalObjectUpdated(Guid RentalObjectId, RentalObjectData Data);
public record PublicationRequested(Guid RentalObjectId, DateTime? AvailableFrom, DateTime? AvailableTo);
```

#### Database Schema

```sql
CREATE TABLE rental_objects (
    id UUID PRIMARY KEY,
    property_id UUID NOT NULL REFERENCES properties(id),
    building_id UUID NOT NULL REFERENCES buildings(id),
    object_number VARCHAR(50) NOT NULL,
    type VARCHAR(50) NOT NULL,
    floor INT,
    entrance VARCHAR(10),
    area DECIMAL(10,2),
    bedrooms INT,
    bathrooms INT,
    available_from DATE,
    available_to DATE,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(building_id, object_number)
);

CREATE TABLE rooms (
    id UUID PRIMARY KEY,
    rental_object_id UUID NOT NULL REFERENCES rental_objects(id),
    name VARCHAR(100),
    area DECIMAL(10,2),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE components (
    id UUID PRIMARY KEY,
    room_id UUID NOT NULL REFERENCES rooms(id),
    name VARCHAR(100),
    type VARCHAR(50),
    created_at TIMESTAMP DEFAULT NOW()
);
```

---

*(Continuing with remaining services: Market, Lease, Inspection, Maintenance, Search, and BFFs in subsequent files due to length constraints...)*

**Document Status:** Part 1 of 2 - Core and Property services documented. Remaining domain services, infrastructure services, and BFFs will be documented in Part 2.
