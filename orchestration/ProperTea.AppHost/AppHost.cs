var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", builder.Configuration["Configs:Username"]!, secret: true);
var password = builder.AddParameter("password", builder.Configuration["Configs:Password"]!, secret: true);

//
// Redis.
//
var redis = builder.AddRedis("redis", 6379, password)
            .WithRedisInsight(op =>
            {
                _ = op.WithHostPort(63790)
                    .WithLifetime(ContainerLifetime.Persistent);
            })
            .WithDataVolume("redis-data")
            .WithLifetime(ContainerLifetime.Persistent);

//
// Postgres.
//
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

//
// Keycloak.
//
var keycloak = builder.AddKeycloak("keycloak", 9080, username, password)
                .WithEnabledFeatures(["organization"])
                .WithRealmImport("./Config/keycloak/realms")
                .WithBindMount("./Config/keycloak/themes", "/opt/keycloak/themes")
                .WithDataVolume("keycloak-data")
                .WithLifetime(ContainerLifetime.Persistent)
                .WithOtlpExporter();
var realmName = builder.Configuration["Configs:KeycloakRealm"];
var keycloakUrl = "http://localhost:9080/";
var keycloakAuthority = keycloakUrl + "realms/" + realmName;
var scalarClientId = builder.Configuration["Configs:ScalarClientId"];
var serviceAccountClientId = builder.Configuration["Configs:ServiceAccountClientId"];
var serviceAccountClientSecret = builder.Configuration["Configs:ServiceAccountClientSecret"];

//
// Applications.
//
var rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password, 5672)
                .WithManagementPlugin(56720)
                .WithDataVolume("rabbitmq-data")
                .WithLifetime(ContainerLifetime.Persistent);

//
// Organization.
//
var organizationDb = postgres.AddDatabase("organization-db");
var organizationService = builder.AddProject<Projects.ProperTea_Organization>("organization")
    .WithEnvironment("OIDC__Authority", keycloakAuthority)
    .WithEnvironment("OIDC__Issuer", keycloakAuthority)
    .WithEnvironment("Keycloak__AuthServerUrl", keycloakUrl)
    .WithEnvironment("Keycloak__Realm", realmName)
    .WithEnvironment("Keycloak__Resource", serviceAccountClientId)
    .WithEnvironment("Keycloak__Credentials__Secret", serviceAccountClientSecret)
    .WithEnvironment("Keycloak__SslRequired", "none")
    .WithEnvironment("Keycloak__ConfidentialPort", "0")
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(keycloak)
    .WithReference(organizationDb)
    .WithReference(rabbitmq)
    .WaitFor(keycloak)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

//
// User.
//
var userDb = postgres.AddDatabase("user-db");
var userService = builder.AddProject<Projects.ProperTea_User>("user")
    .WithEnvironment("OIDC__Authority", keycloakAuthority)
    .WithEnvironment("OIDC__Issuer", keycloakAuthority)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(keycloak)
    .WithReference(userDb)
    .WithReference(rabbitmq)
    .WaitFor(keycloak)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

//
// Landlord Portal.
//
var landlordClientId = builder.Configuration["Configs:LandlordClientId"];
var landlordClientSecret = builder.Configuration["Configs:LandlordClientSecret"];
var landlordAudience = builder.Configuration["Configs:Audience"];
_ = builder.AddProject<Projects.ProperTea_Landlord_Bff>("landlord-bff")
    .WithEnvironment("OIDC__Authority", keycloakAuthority)
    .WithEnvironment("OIDC__Issuer", keycloakAuthority)
    .WithEnvironment("OIDC__Audience", landlordAudience)
    .WithEnvironment("OIDC__ClientId", landlordClientId)
    .WithEnvironment("OIDC__ClientSecret", landlordClientSecret)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(redis)
    .WithReference(organizationService)
    .WithReference(userService)
    .WithReference(keycloak)
    .WaitFor(redis)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

builder.Build().Run();
