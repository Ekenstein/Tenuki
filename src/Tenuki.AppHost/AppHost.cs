var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithDataVolume("tenuki-redis-data");

var server = builder
    .AddDotnetProject("server", "../Tenuki.Server/Tenuki.Server.csproj")
    .WithEnvironment(async context =>
    {
        var connectionString = redis.Resource.GetConnectionStringAsync(context.CancellationToken);
        context.EnvironmentVariables["Redis__ConnectionString"] = connectionString;
    })
    .WaitFor(redis);

builder
    .AddViteApp(
        name: "client",
        workingDirectory: "../Tenuki.Client",
        packageManager: "npm",
        useHttps: false
    )
    .WithReference(server)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
