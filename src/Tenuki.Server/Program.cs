using Tenuki.Protocol;
using Tenuki.Server;
using Tenuki.Server.Actions;
using Tenuki.Server.BackgroundServices;
using Tenuki.Server.Messaging;
using Tenuki.Server.Options;
using Tenuki.Server.Redis;
using Tenuki.Server.Routes;
using Tenuki.Services.Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options =>
    JsonUtils.ConfigureOptions(options.SerializerOptions)
);
builder
    .Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    })
    .AddJsonProtocol(options => JsonUtils.ConfigureOptions(options.PayloadSerializerOptions));
builder.Services.AddSingleton<ActionContext>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddRedis();

builder
    .Services.AddOptions<GameNotificationOptions>()
    .BindConfiguration("GameNotifications")
    .ValidateOnStart();
builder.Services.AddHostedService<GameNotificationBackgroundService>();

builder
    .Services.AddOptions<GameNotificationRecoveryOptions>()
    .BindConfiguration("GameNotificationRecovery")
    .ValidateOnStart();
builder.Services.AddHostedService<GameNotificationRecoveryBackgroundService>();

builder.Services.AddSingleton<IGameStateStore, RedisGameStateStore>();
builder.Services.AddSingleton<IGameStateTokenCodec, GameStateHmacTokenCodec>();
builder.Services.AddSingleton<GameChannel>();
builder.Services.AddSingleton<IMessageBroadcaster<GameMessage>>(services =>
    services.GetRequiredService<GameChannel>()
);
builder.Services.AddSingleton<LobbyChannel>();
builder.Services.AddSingleton<IMessageBroadcaster<LobbyMessage>>(services =>
    services.GetRequiredService<LobbyChannel>()
);

builder
    .Services.AddOptions<GamePresenceOptions>()
    .BindConfiguration("GamePresence")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IConnectionPresence, RedisConnectionPresence>();
builder.Services.AddSingleton<ConnectionRegistry>();
builder.Services.AddSingleton<ConnectionPresenceRefresher>();
builder.Services.AddHostedService<ConnectionPresenceBackgroundService>();

builder.Services.AddSingleton<IGamePresence, RedisGamePresence>();
builder.Services.AddSingleton(RedisStreamSettings.Default);
builder.Services.AddSingleton<RedisIncomingMessageReader>();
builder.Services.AddSingleton<IncomingMessageDispatcher>();
builder
    .Services.AddOptions<HmacTokenOptions>()
    .BindConfiguration("HmacToken")
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

AllRoutes.Register(app);

app.Run();
