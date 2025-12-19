var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "admin", secret: true);
var password = builder.AddParameter("password", "admin", secret: true);

var redis = builder.AddRedis("redis", 6379, password)
            .WithRedisInsight()
            .WithRedisCommander()
            .WithDataVolume();

var postgres = builder.AddPostgres("postgres", username, password, 5432)
                .WithPgAdmin()
                .WithDataVolume();

var rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password, 5672)
                .WithManagementPlugin()
                .WithDataVolume();

var landlordBff = builder.AddProject<Projects.ProperTea_Landlord_Bff>("landlord-bff")
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

var landlordWeb = builder.AddNpmApp("landlord-web", "../landlord/portal/web/propertea-landlord-portal")
    .WithNpmPackageInstallation()
    .WithReference(landlordBff)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithDeveloperCertificateTrust(true);

builder.Build().Run();
