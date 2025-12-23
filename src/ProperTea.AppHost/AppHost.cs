using System.Globalization;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "admin", secret: true);
var password = builder.AddParameter("password", "admin", secret: true);

var redis = builder.AddRedis("redis", 6379, password)
            .WithRedisInsight(op =>
            {
                _ = op.WithHostPort(63790);
            })
            .WithDataVolume("redis-data")
            .WithLifetime(ContainerLifetime.Persistent);

var postgresPort = 5432;
var postgres = builder.AddPostgres("postgres", username, password, postgresPort)
                .WithPgAdmin(op =>
                {
                    _ = op.WithHostPort(54320);
                })
                .WithDataVolume("postgres-data")
                .WithLifetime(ContainerLifetime.Persistent);

var mailpit = builder.AddMailPit("mailpit", httpPort: 8025, smtpPort: 1025)
                .WithDataVolume("mailpit-data")
                .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "true")
                .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "true")
                .WithLifetime(ContainerLifetime.Persistent);

// ZITADEL will create its own database
var zitadelDatabaseName = "zitadelDb";
var zitadelPort = 9080;
var zitadelLoginUiPort = 9081;
var zitadelUrl = $"http://localhost:{zitadelPort}";
var zitadelLoginUiUrl = $"http://localhost:{zitadelLoginUiPort}";
var tokenPath = Path.GetFullPath("Config/zitadel/login-client.pat");
var zitadel = builder.AddContainer("zitadel", "ghcr.io/zitadel/zitadel", "v4.7.6")
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
    .WithEnvironment("ZITADEL_EXTERNALPORT", "8080")
    .WithEnvironment("ZITADEL_TLS_ENABLED", "false")
    .WithEnvironment("ZITADEL_MASTERKEY", "MasterkeyNeedsToHave32Characters")
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
    .WithEnvironment("ZITADEL_DEFAULTINSTANCE_FEATURES_LOGINV2_REQUIRED", "true") // v2
    .WithEnvironment("ZITADEL_DEFAULTINSTANCE_FEATURES_LOGINV2_BASEURI", $"{zitadelLoginUiUrl}/ui/v2/login")
    .WithArgs("start-from-init", "--masterkeyFromEnv", "--tlsMode", "disabled")
    .WaitFor(postgres)
    .WaitFor(mailpit);
var zitadelLogin = builder.AddContainer("zitadel-login", "ghcr.io/zitadel/zitadel-login", "v4.7.6")
    .WithHttpEndpoint(port: zitadelLoginUiPort, targetPort: 3000)
    .WithEnvironment("ZITADEL_API_URL", "http://zitadel:8080")
    .WithEnvironment("CUSTOM_REQUEST_HEADERS", "Host:localhost")
    .WithEnvironment("NEXT_PUBLIC_BASE_PATH", "/ui/v2/login")
    .WithBindMount(tokenPath, "/opt/zitadel/login-client.pat")
    .WithEnvironment("ZITADEL_SERVICE_USER_TOKEN_FILE", "/opt/zitadel/login-client.pat")
    .WaitFor(zitadel);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password, 5672)
                .WithManagementPlugin(56720)
                .WithDataVolume("rabbitmq-data");

var landlordBff = builder.AddProject<Projects.ProperTea_Landlord_Bff>("landlord-bff")
    .WithReference(redis)
    .WithEnvironment("OIDC__Authority", zitadelUrl)
    .WaitFor(redis)
    .WaitFor(zitadel)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

builder.Build().Run();
