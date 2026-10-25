using Discord;

namespace MyDiscordBot.Services;

public class NotificationService
{
    public async Task SendAsync(
        IMessageChannel channel,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (message.Length > 2_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(message),
                "Discord messages cannot exceed 2,000 characters.");
        }

        await channel.SendMessageAsync(
            text: message,
            allowedMentions: AllowedMentions.None);
    }
}
