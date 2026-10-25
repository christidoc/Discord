
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using MyDiscordBot.Services;

namespace MyDiscordBot.Commands;

[Group("utility", "Helpful commands for server administration.")]
public sealed class UtilityCommands(
    NotificationService notifications,
    ILogger<UtilityCommands> logger)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Check whether the bot is responding.")]
    public async Task PingAsync()
    {
        await RespondAsync(
            $"Pong! Gateway latency: {Context.Client.Latency} ms.",
            ephemeral: true);
    }

    [SlashCommand("serverinfo", "Show information about this server.")]
    public async Task ServerInfoAsync()
    {
        var guild = Context.Guild;

        if (guild is null)
        {
            await RespondAsync(
                "This command can only be used in a server.",
                ephemeral: true);
            return;
        }

        await RespondAsync(
            $"**{guild.Name}**\n" +
            $"Server-ID: `{guild.Id}`\n" +
            $"Member Count: {guild.MemberCount}\n" +
            $"Created at: {guild.CreatedAt:dd-MM-yyyy}",
            ephemeral: true);
    }

    [SlashCommand("notify", "Send a notification to a channel.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task NotifyAsync(
        [Summary("channel", "The channel to send the notification to")]
        SocketTextChannel channel,
        [Summary("message", "The notification content")]
        string message)
    {
        if (Context.Guild is null ||
            channel.Guild.Id != Context.Guild.Id)
        {
            await RespondAsync(
                "Choose a channel from this server.",
                ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            await notifications.SendAsync(channel, message);

            await FollowupAsync(
                $"Notification sent to {channel.Mention}.",
                ephemeral: true);
        }
        catch (Discord.Net.HttpException exception)
        {
            logger.LogWarning(
                exception,
                "Could not send notification to channel {ChannelId}.",
                channel.Id);

            await FollowupAsync(
                "Failed to send the notification. Check the bot's channel permissions.",
                ephemeral: true);
        }
    }
}
