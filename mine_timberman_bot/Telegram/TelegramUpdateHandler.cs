using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Telegram;

public sealed class TelegramUpdateHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramUpdateHandler> logger) : IUpdateHandler
{
    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        // Отдельный scope позволит командам безопасно использовать scoped-сервисы, например DbContext.
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<UpdateDispatcher>();

        await dispatcher.DispatchAsync(botClient, update, cancellationToken);
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        var logLevel = source switch
        {
            HandleErrorSource.PollingError => LogLevel.Warning,
            HandleErrorSource.HandleUpdateError => LogLevel.Error,
            HandleErrorSource.FatalError => LogLevel.Critical,
            _ => LogLevel.Error
        };

        logger.Log(
            logLevel,
            exception,
            "Telegram polling failed. Error source: {ErrorSource}",
            source);

        return Task.CompletedTask;
    }
}
