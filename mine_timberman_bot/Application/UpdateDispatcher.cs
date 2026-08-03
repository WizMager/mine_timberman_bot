using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Callbacks;
using MineTimbermanBot.Application.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Application;

public sealed class UpdateDispatcher(
    CommandDispatcher commandDispatcher,
    CallbackDispatcher callbackDispatcher,
    IUnitOfWork unitOfWork,
    ILogger<UpdateDispatcher> logger)
{
    public async Task DispatchAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is { Text: not null } message)
        {
            await commandDispatcher.DispatchAsync(botClient, message, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (update.CallbackQuery is { } callback)
        {
            await callbackDispatcher.DispatchAsync(botClient, callback, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        logger.LogDebug(
            "Update {UpdateId} of type {UpdateType} has no registered handler",
            update.Id,
            update.Type);
    }
}
