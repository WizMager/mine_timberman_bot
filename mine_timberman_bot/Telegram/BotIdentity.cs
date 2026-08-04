using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace MineTimbermanBot.Telegram;

public sealed class BotIdentity(
    ITelegramBotClient botClient,
    ILogger<BotIdentity> logger) : IHostedService
{
    public string Username { get; private set; } = string.Empty;

    public bool IsInitialized => !string.IsNullOrWhiteSpace(Username);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bot = await botClient.GetMe(cancellationToken);
        if (string.IsNullOrWhiteSpace(bot.Username))
        {
            throw new InvalidOperationException("Telegram GetMe returned empty username.");
        }

        Username = bot.Username.Trim().TrimStart('@');

        logger.LogInformation(
            "Bot identity initialized as @{Username} ({BotId})",
            Username,
            bot.Id);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public bool IsAddressedToThisBot(string? mentionedBotUsername)
    {
        if (!string.IsNullOrWhiteSpace(mentionedBotUsername))
        {
            if (!IsInitialized)
            {
                return false;
            }

            return string.Equals(
                mentionedBotUsername.Trim().TrimStart('@'),
                Username,
                StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
}
