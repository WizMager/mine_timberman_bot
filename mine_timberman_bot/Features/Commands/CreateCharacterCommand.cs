using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace MineTimbermanBot.Features.Commands;

public class CreateCharacterCommand(
    IUserSessionStore userSessionStore,
    ILogger<CreateCharacterCommand> logger
) : IBotCommand
{
    public string Name => "create";
    
    public string Description => "Создать персонажа";

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

        var userData = userSessionStore.GetOrCreate(user.Id);
        var chat = context.Message.Chat;

        if (userData.CharacterName is null)
        {
            userData.CharacterName = user.Username;
            userData.BoltsInWorkSession = Random.Shared.Next(1, 5);
            userData.LogsInWorkSession = 0;
            userData.Force = 10;

            if (chat.Type is ChatType.Group or ChatType.Supergroup)
            {
                userSessionStore.RegisterCharacterInChat(chat.Id, user.Id);
            }

            await context.BotClient.SendMessage(
                chat,
                "Ты создал сына маркшейдерши и МГВМ",
                cancellationToken: cancellationToken);
            return;
        }

        await context.BotClient.SendMessage(
            chat,
            $"У тебя уже создан крепиль с именем {userData.CharacterName}",
            cancellationToken: cancellationToken);
    }
}
