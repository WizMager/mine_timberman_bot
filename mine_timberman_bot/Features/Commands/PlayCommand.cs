using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;

namespace MineTimbermanBot.Features.Commands;

public sealed class PlayCommand(
    IUserSessionStore sessionStore,
    ILogger<PlayCommand> logger) : IBotCommand
{
    public string Name => "play";

    public string Description => "Начать учебную игру";

    public async Task ExecuteAsync(
        BotCommandContext context,
        CancellationToken cancellationToken)
    {
        if (context.Message.From is not { } user)
        {
            await context.BotClient.SendMessage(
                context.Message.Chat,
                "Не удалось определить пользователя для игровой сессии.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            await context.BotClient.DeleteMessage(
                context.Message.Chat,
                context.Message.Id,
                cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            // В группе для удаления чужих сообщений боту могут понадобиться права администратора.
            logger.LogDebug(
                exception,
                "Could not delete /play message {MessageId} in chat {ChatId}",
                context.Message.Id,
                context.Message.Chat.Id);
        }

        var session = sessionStore.GetOrCreate(user.Id);

        lock (session)
        {
            session.State = UserSessionStates.Choosing;
            session.Score = 0;
            session.SelectedSide = null;
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Влево", "side:left"),
            InlineKeyboardButton.WithCallbackData("Вправо", "side:right")
        });

        await context.BotClient.SendMessage(
            context.Message.Chat,
            "Выберите сторону для игры:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}
