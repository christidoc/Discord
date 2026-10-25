
using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace MyDiscordBot.Bots;

public sealed class DiscordBotService(
    DiscordSocketClient client,
    InteractionService interactions,
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<DiscordBotService> logger) : IHostedService
{
    private int _commandsRegistered;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var token = configuration["DISCORD_TOKEN"];

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Missing required configuration: DISCORD_TOKEN");
        }

        client.Log += LogAsync;
        interactions.Log += LogAsync;

        client.InteractionCreated += HandleInteractionAsync;
        client.Ready += RegisterCommandsAsync;

        await interactions.AddModulesAsync(
            Assembly.GetExecutingAssembly(),
            services);

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        logger.LogInformation("Discord bot is starting.");
    }

    private async Task RegisterCommandsAsync()
    {
        // Ready may fire again after a reconnect.
        if (Interlocked.Exchange(ref _commandsRegistered, 1) == 1)
            return;

        try
        {
#if DEBUG
            var guildId = configuration["DISCORD_TEST_GUILD_ID"];

            if (!ulong.TryParse(guildId, out var parsedGuildId))
            {
                throw new InvalidOperationException(
                    "Set DISCORD_TEST_GUILD_ID for development.");
            }

            await interactions.RegisterCommandsToGuildAsync(parsedGuildId);
#else
            await interactions.RegisterCommandsGloballyAsync();
#endif

            logger.LogInformation("Slash commands registered.");
        }
        catch
        {
            Interlocked.Exchange(ref _commandsRegistered, 0);
            throw;
        }
    }

    private async Task HandleInteractionAsync(
        SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(client, interaction);
            var result = await interactions.ExecuteCommandAsync(
                context, services);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Interaction failed: {Reason}", result.ErrorReason);

                if (!interaction.HasResponded)
                {
                    await interaction.RespondAsync(
                        "Het commando kon niet worden uitgevoerd.",
                        ephemeral: true);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error handling interaction.");

            if (!interaction.HasResponded)
            {
                await interaction.RespondAsync(
                    "Er ging iets mis bij het uitvoeren van dit commando.",
                    ephemeral: true);
            }
        }
    }

    private Task LogAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            _ => LogLevel.Debug
        };

        logger.Log(level, message.Exception, "{Message}", message.Message);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        client.InteractionCreated -= HandleInteractionAsync;
        client.Ready -= RegisterCommandsAsync;

        await client.StopAsync();
        await client.LogoutAsync();
    }
}
