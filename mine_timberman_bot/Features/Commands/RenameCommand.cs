using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace MineTimbermanBot.Features.Commands;

public class RenameCommand(
    IUserSessionStore userSessionStore,
    ILogger<RenameCommand> logger
) : IBotCommand
{
    private const int MaxNameLength = 16;

    public string Name => "rename";

    public string Description => "Изменить имя крепиля в табеле";

    public async Task ExecuteAsync(BotCommandContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.BotClient.DeleteMessage(
                context.Message.Chat,
                context.Message.Id,
                cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            logger.LogDebug(
                exception,
                "Could not delete message {MessageId} in chat {ChatId}",
                context.Message.Id,
                context.Message.Chat.Id);
        }

        if (context.Message.From is not { } user)
        {
            await context.BotClient.SendMessage(
                context.Message.Chat,
                "Не удалось определить пользователя для игровой сессии.",
                cancellationToken: cancellationToken);
            return;
        }

        if (!userSessionStore.TryGet(user.Id, out var userData) || userData.CharacterName is null)
        {
            await context.BotClient.SendMessage(
                context.Message.Chat,
                "В табеле пустая строка, кому имя менять? Создай крепиля сразу",
                cancellationToken: cancellationToken);
            return;
        }

        var newName = context.Arguments.Trim();
        switch (newName.Length)
        {
            case 0:
                await context.BotClient.SendMessage(
                    context.Message.Chat,
                    "Напиши так: /rename НовоеИмя, там пробел после команды",
                    cancellationToken: cancellationToken);
                return;
            case > MaxNameLength:
                await context.BotClient.SendMessage(
                    context.Message.Chat,
                    $"Имя слишком длинное. Максимум {MaxNameLength} символов.",
                    cancellationToken: cancellationToken);
                return;
        }

        var oldName = userData.CharacterName;
        userData.CharacterName = newName;

        await context.BotClient.SendMessage(
            context.Message.Chat,
            $"Крепиль {oldName} теперь в табеле как {newName}.",
            cancellationToken: cancellationToken);
    }
}
