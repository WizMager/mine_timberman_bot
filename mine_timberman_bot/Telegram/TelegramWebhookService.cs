using Microsoft.Extensions.Options;
using MineTimbermanBot.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MineTimbermanBot.Telegram;

public sealed class TelegramWebhookService(
    ITelegramBotClient botClient,
    BotIdentity botIdentity,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramWebhookService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var botOptions = options.Value;
        if (!botOptions.UseWebhook)
        {
            return;
        }

        logger.LogInformation(
            "Bot @{Username} is registering webhook {WebhookUrl}",
            botIdentity.Username,
            botOptions.WebhookUrl);

        await botClient.SetWebhook(
            url: botOptions.WebhookUrl,
            allowedUpdates:
            [
                UpdateType.Message,
                UpdateType.CallbackQuery
            ],
            dropPendingUpdates: botOptions.DropPendingUpdates,
            secretToken: botOptions.SecretToken,
            cancellationToken: cancellationToken);

        logger.LogInformation("Webhook registered successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
