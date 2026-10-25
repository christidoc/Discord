using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using MyDiscordBot.Services;
using MyDiscordBot.Bots;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new DiscordSocketClient(
    new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds,
        LogGatewayIntentWarnings = true,
        AlwaysDownloadUsers = false
    }));

builder.Services.AddSingleton(provider =>
    new InteractionService(
        provider.GetRequiredService<DiscordSocketClient>().Rest));

builder.Services.AddSingleton<NotificationService>();
builder.Services.AddHostedService<DiscordBotService>();

using var host = builder.Build();
await host.RunAsync();