var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", builder.Configuration["Configs:Username"]!, secret: true);
var password = builder.AddParameter("password", builder.Configuration["Configs:Password"]!, secret: true);

// Redis.
var redis = builder.AddRedis("redis", 6379, password)
            .WithRedisInsight(op =>
            {
                _ = op.WithHostPort(63790)
                    .WithLifetime(ContainerLifetime.Persistent);
            })
            .WithDataVolume("redis-data")
            .WithLifetime(ContainerLifetime.Persistent);

// Postgres.
var postgresPort = 5432;
var postgres = builder.AddPostgres("postgres", username, password, postgresPort)
                .WithPgAdmin(op =>
                {
                    _ = op.WithHostPort(54320)
                        .WithLifetime(ContainerLifetime.Persistent);
                })
                .WithDataVolume("postgres-data")
                .WithLifetime(ContainerLifetime.Persistent);

// Mailpit.
var mailpit = builder.AddMailPit("mailpit", httpPort: 8025, smtpPort: 1025)
                .WithDataVolume("mailpit-data")
                .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "true")
                .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "true")
                .WithLifetime(ContainerLifetime.Persistent);

// Keycloak.
var keycloakPort = 9080;
var keycloakRealm = "propertea";
var keycloakUrl = $"http://localhost:{keycloakPort}";
var keycloakAuthority = $"{keycloakUrl}/realms/{keycloakRealm}";
var keycloakConfigPath = Path.GetFullPath("Config/keycloak");

var keycloak = builder.AddKeycloak("keycloak", port: keycloakPort, adminUsername: username, adminPassword: password)
    // Enable the Organizations feature (preview in KC 26, GA in KC 26.1+).
    .WithEnvironment("KC_FEATURES", "organization")
    .WithDataVolume("keycloak-data")
    .WithRealmImport(keycloakConfigPath);

var scalarClientId = builder.Configuration["Configs:ScalarClientId"];
var apiAudience = builder.Configuration["Configs:ApiAudience"];
var orgServiceClientId = builder.Configuration["Configs:OrgServiceClientId"];
var orgServiceClientSecret = builder.Configuration["Configs:OrgServiceClientSecret"];
var userServiceClientId = builder.Configuration["Configs:UserServiceClientId"];
var userServiceClientSecret = builder.Configuration["Configs:UserServiceClientSecret"];

// Applications.
var rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password, 5672)
                .WithManagementPlugin(56720)
                .WithDataVolume("rabbitmq-data")
                .WithLifetime(ContainerLifetime.Persistent);

// Organization.
var organizationDb = postgres.AddDatabase("organization-db");
var organizationService = builder.AddProject<Projects.ProperTea_Organization>("organization")
    .WithEnvironment("OIDC__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__AuthServerUrl", $"{keycloakUrl}/")
    .WithEnvironment("Keycloak__Realm", keycloakRealm)
    .WithEnvironment("Keycloak__Resource", orgServiceClientId)
    .WithEnvironment("Keycloak__Credentials__Secret", orgServiceClientSecret)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(organizationDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

// User.
var userDb = postgres.AddDatabase("user-db");
var userService = builder.AddProject<Projects.ProperTea_User>("user")
    .WithEnvironment("OIDC__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__AuthServerUrl", $"{keycloakUrl}/")
    .WithEnvironment("Keycloak__Realm", keycloakRealm)
    .WithEnvironment("Keycloak__Resource", userServiceClientId)
    .WithEnvironment("Keycloak__Credentials__Secret", userServiceClientSecret)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(userDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

// Company.
var companyDb = postgres.AddDatabase("company-db");
var companyService = builder.AddProject<Projects.ProperTea_Company>("company")
    .WithEnvironment("OIDC__Authority", keycloakAuthority)
    .WithEnvironment("OIDC__Issuer", keycloakAuthority)
    .WithEnvironment("OIDC__Audience", apiAudience)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(companyDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

// Property.
var propertyDb = postgres.AddDatabase("property-db");
var propertyService = builder.AddProject<Projects.ProperTea_Property>("property")
    .WithEnvironment("OIDC__Authority", keycloakAuthority)
    .WithEnvironment("OIDC__Issuer", keycloakAuthority)
    .WithEnvironment("OIDC__Audience", apiAudience)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(propertyDb)
    .WithReference(rabbitmq)
    .WithReference(companyService)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

// Landlord Portal.
var landlordClientId = builder.Configuration["Configs:LandlordClientId"];
var landlordClientSecret = builder.Configuration["Configs:LandlordClientSecret"];
_ = builder.AddProject<Projects.ProperTea_Landlord_Bff>("landlord-bff")
    .WithReference(redis)
    .WithReference(organizationService)
    .WithReference(userService)
    .WithReference(companyService)
    .WithReference(propertyService)
    .WithEnvironment("Keycloak__AuthServerUrl", $"{keycloakUrl}/")
    .WithEnvironment("Keycloak__Realm", keycloakRealm)
    .WithEnvironment("Keycloak__Resource", landlordClientId)
    .WithEnvironment("Keycloak__Credentials__Secret", landlordClientSecret)
    .WithEnvironment("Keycloak__SslRequired", "none")
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WaitFor(redis)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

builder.Build().Run();
