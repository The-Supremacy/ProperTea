using System.Globalization;

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

// ZITADEL.
var zitadelDatabaseName = "zitadelDb";
var zitadelPort = 9080;
var zitadelLoginUiPort = 9081;
var zitadelUrl = $"http://localhost:{zitadelPort}";
var zitadelLoginUiUrl = $"http://localhost:{zitadelLoginUiPort}";
var zitadelConfigPath = Path.GetFullPath("Config/zitadel");
var zitadelTokenPath = "/opt/zitadel/config/login-ui-client.pat";
var zitadel = builder.AddContainer("zitadel", "ghcr.io/zitadel/zitadel", "v4.10.0")
    .WithHttpEndpoint(port: zitadelPort, targetPort: 8080, name: "http")
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_HOST", "postgres")
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_PORT", postgresPort.ToString(CultureInfo.InvariantCulture))
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_DATABASE", zitadelDatabaseName)
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_USERNAME", username)
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_PASSWORD", password)
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_SSL_MODE", "disable")
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_USERNAME", username)
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_PASSWORD", password)
    .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_SSL_MODE", "disable")
    .WithEnvironment("ZITADEL_EXTERNALSECURE", "false")
    .WithEnvironment("ZITADEL_EXTERNALDOMAIN", "localhost")
    .WithEnvironment("ZITADEL_EXTERNALPORT", "9080")
    .WithEnvironment("ZITADEL_TLS_ENABLED", "false")
    .WithEnvironment("ZITADEL_MASTERKEY", "MasterkeyNeedsToHave32Characters")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_LOGINCLIENTPATPATH", zitadelTokenPath)
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_MACHINE_USERNAME", "login-client")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_MACHINE_NAME", "Automatically Initialized IAM_LOGIN_CLIENT")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_PAT_EXPIRATIONDATE", "2029-01-01T00:00:00Z")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_NAME", "ProperTea")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_HUMAN_USERNAME", "admin")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_HUMAN_EMAIL_ADDRESS", "admin@propertea.localhost")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_HUMAN_EMAIL_VERIFIED", "true")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_HUMAN_PASSWORD", "Password1!")
    .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_HUMAN_PASSWORDCHANGEREQUIRED", "false")
    .WithEnvironment("ZITADEL_NOTIFICATIONS_PROVIDERS_SMTP_HOST", "mailpit")
    .WithEnvironment("ZITADEL_NOTIFICATIONS_PROVIDERS_SMTP_PORT", "1025")
    .WithEnvironment("ZITADEL_NOTIFICATIONS_PROVIDERS_SMTP_TLS", "false")
    .WithEnvironment("ZITADEL_NOTIFICATIONS_PROVIDERS_SMTP_FROM", "noreply@propertea.localhost")
    .WithEnvironment("ZITADEL_NOTIFICATIONS_PROVIDERS_SMTP_FROMNAME", "ProperTea")
    .WithEnvironment("ZITADEL_DEFAULTINSTANCE_FEATURES_LOGINV2_REQUIRED", "true")
    .WithEnvironment("ZITADEL_DEFAULTINSTANCE_FEATURES_LOGINV2_BASEURI", $"{zitadelLoginUiUrl}/ui/v2/login")
    .WithEnvironment("ZITADEL_OIDC_DEFAULTLOGINURLV2", $"{zitadelLoginUiUrl}/ui/v2/login/login?authRequest=")
    .WithEnvironment("ZITADEL_OIDC_DEFAULTLOGOUTURLV2", $"{zitadelLoginUiUrl}/ui/v2/login/logout?post_logout_redirect=")
    .WithEnvironment("ZITADEL_SAML_DEFAULTLOGINURLV2", $"{zitadelLoginUiUrl}/ui/v2/login/login?samlRequest=")
    .WithArgs("start-from-init", "--masterkeyFromEnv", "--tlsMode", "disabled")
    .WithBindMount(zitadelConfigPath, "/opt/zitadel/config")
    .WaitFor(postgres)
    .WaitFor(mailpit);
_ = builder.AddContainer("zitadel-login", "ghcr.io/zitadel/zitadel-login", "v4.10.0")
    .WithHttpEndpoint(port: zitadelLoginUiPort, targetPort: 3000)
    .WithEnvironment("ZITADEL_API_URL", "http://zitadel:8080")
    .WithEnvironment("ZITADEL_SERVICE_USER_TOKEN_FILE", zitadelTokenPath)
    .WithEnvironment("CUSTOM_REQUEST_HEADERS", "Host:localhost")
    .WithEnvironment("NEXT_PUBLIC_BASE_PATH", "/ui/v2/login")
    .WithBindMount(zitadelConfigPath, "/opt/zitadel/config")
    .WaitFor(zitadel);

var scalarClientId = builder.Configuration["Configs:ScalarClientId"];
var audience = builder.Configuration["Configs:ProjectId"];

// Applications.
var rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password, 5672)
                .WithManagementPlugin(56720)
                .WithDataVolume("rabbitmq-data")
                .WithLifetime(ContainerLifetime.Persistent);

// Organization.
var organizationServiceAccountJwtPath = Path.GetFullPath("Config/zitadel/organization-service.json");
var organizationServiceAppJwtPath = Path.GetFullPath("Config/zitadel/organization-app.json");
var organizationDb = postgres.AddDatabase("organization-db");
var organizationService = builder.AddProject<Projects.ProperTea_Organization>("organization")
    .WithEnvironment("OIDC__Authority", zitadelUrl)
    .WithEnvironment("OIDC__Issuer", zitadelUrl)
    .WithEnvironment("OIDC__Audience", audience)
    .WithEnvironment("Zitadel__ServiceAccountJwtPath", organizationServiceAccountJwtPath)
    .WithEnvironment("Zitadel__AppJwtPath", organizationServiceAppJwtPath)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(organizationDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(zitadel)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

// User.
var userServiceAccountJwtPath = Path.GetFullPath("Config/zitadel/user-service.json");
var userServiceAppJwtPath = Path.GetFullPath("Config/zitadel/user-app.json");
var userDb = postgres.AddDatabase("user-db");
var userService = builder.AddProject<Projects.ProperTea_User>("user")
    .WithEnvironment("OIDC__Authority", zitadelUrl)
    .WithEnvironment("OIDC__Issuer", zitadelUrl)
    .WithEnvironment("OIDC__Audience", audience)
    .WithEnvironment("Zitadel__ServiceAccountJwtPath", userServiceAccountJwtPath)
    .WithEnvironment("Zitadel__AppJwtPath", userServiceAppJwtPath)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(userDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(zitadel)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

// Company.
var companyDb = postgres.AddDatabase("company-db");
var companyService = builder.AddProject<Projects.ProperTea_Company>("company")
    .WithEnvironment("OIDC__Authority", zitadelUrl)
    .WithEnvironment("OIDC__Issuer", zitadelUrl)
    .WithEnvironment("OIDC__Audience", audience)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(companyDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(zitadel)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

// Property.
var propertyDb = postgres.AddDatabase("property-db");
var propertyService = builder.AddProject<Projects.ProperTea_Property>("property")
    .WithEnvironment("OIDC__Authority", zitadelUrl)
    .WithEnvironment("OIDC__Issuer", zitadelUrl)
    .WithEnvironment("OIDC__Audience", audience)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WithReference(propertyDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(zitadel)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

// Landlord Portal.
var landlordClientId = builder.Configuration["Configs:LandlordClientId"];
_ = builder.AddProject<Projects.ProperTea_Landlord_Bff>("landlord-bff")
    .WithReference(redis)
    .WithReference(organizationService)
    .WithReference(userService)
    .WithReference(companyService)
    .WithReference(propertyService)
    .WithEnvironment("OIDC__Authority", zitadelUrl)
    .WithEnvironment("OIDC__ClientId", landlordClientId)
    .WithEnvironment("Scalar__ClientId", scalarClientId)
    .WaitFor(redis)
    .WaitFor(zitadel)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

builder.Build().Run();
