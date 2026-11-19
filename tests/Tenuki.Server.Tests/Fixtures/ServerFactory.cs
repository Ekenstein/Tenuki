using InMemoryEmulator.Redis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Immutable;

namespace Tenuki.Server.Tests.Fixtures;

public sealed class ServerFactory(
    TimeProvider timeProvider,
    ImmutableDictionary<string, string?> config
) : WebApplicationFactory<Program>
{
    public static readonly ImmutableDictionary<string, string?> ValidConfig = MapOf(
        PairOf<string, string?>("HmacToken:Secret", "test-secret"),
        PairOf<string, string?>("Redis:ConnectionString", "localhost:6379,password=secret_password")
    );

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();

        builder.ConfigureAppConfiguration(
            (context, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(config);
            }
        );

        builder.ConfigureTestServices(services =>
        {
            var timeProviderServiceDescriptor = new ServiceDescriptor(
                typeof(TimeProvider),
                timeProvider
            );
            services.Replace(timeProviderServiceDescriptor);

            services.UseInMemoryRedis();
        });
    }
}
