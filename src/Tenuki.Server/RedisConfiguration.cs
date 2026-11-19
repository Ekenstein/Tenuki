using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Tenuki.Server;

public static class RedisConfiguration
{
    public static void AddRedis(this IServiceCollection services)
    {
        services
            .AddOptions<ConfigurationOptions>()
            .Configure<IConfiguration, ILoggerFactory>(
                (options, configuration, loggerFactory) =>
                {
                    options.LoggerFactory = loggerFactory;

                    var connectionString = configuration["Redis:ConnectionString"];
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        var parsed = ConfigurationOptions.Parse(connectionString);
                        options.EndPoints.Clear();
                        foreach (var endpoint in parsed.EndPoints)
                        {
                            options.EndPoints.Add(endpoint);
                        }

                        options.Password = parsed.Password;
                        options.Ssl = parsed.Ssl;
                        options.User = parsed.User;
                    }
                }
            )
            .Validate(
                options => options.EndPoints.Count > 0,
                "At least one Redis endpoint must be configured."
            )
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(services =>
        {
            var options = services.GetRequiredService<IOptions<ConfigurationOptions>>();
            return ConnectionMultiplexer.Connect(options.Value);
        });
    }
}
