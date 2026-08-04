using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MineTimbermanBot.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace MineTimbermanBot.Telegram;

public sealed class TelegramBotWorker(
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramBotWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.UseWebhook)
        {
            logger.LogInformation("Webhook mode is enabled; long polling is disabled");
            return;
        }

        try
        {
            var bot = await botClient.GetMe(stoppingToken);

            logger.LogInformation(
                "Bot @{Username} ({BotId}) is starting in long polling mode",
                bot.Username,
                bot.Id);

            await botClient.DeleteWebhook(
                dropPendingUpdates: false,
                cancellationToken: stoppingToken);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates =
                [
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                ],
                DropPendingUpdates = options.Value.DropPendingUpdates
            };

            await botClient.ReceiveAsync(
                updateHandler,
                receiverOptions,
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Telegram bot is stopping");
        }
    }
}