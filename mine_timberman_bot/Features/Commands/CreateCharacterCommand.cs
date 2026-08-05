using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MineTimbermanBot.Features.Commands;

public class CreateCharacterCommand(
    IUserSessionStore userSessionStore,
    ILogger<CreateCharacterCommand> logger
) : BotCommandBase(logger, userSessionStore)
{
    public override string Name => "create";

    public override string Description => "Создать персонажа";

    protected override async Task ExecuteCoreAsync(BotCommandContext context, User user, CancellationToken cancellationToken)
    {
        var userData = await SessionStore.GetOrCreateAsync(user.Id, cancellationToken);
        var chat = context.Message.Chat;

        if (userData.CharacterName is null)
        {
            userData.CharacterName = user.Username;
            userData.BoltsInWorkSession = 0;
            userData.LogsInWorkSession = 0;
            userData.Force = 50;
            userData.Lucky = 15;

            if (chat.Type is ChatType.Group or ChatType.Supergroup)
            {
                await SessionStore.RegisterCharacterInChatAsync(chat.Id, user.Id, cancellationToken);
            }

            await context.BotClient.SendMessage(
                chat,
                $"Ты создал сына маркшейдерши и МГВМ {userData.CharacterName}",
                cancellationToken: cancellationToken);
            return;
        }

        await context.BotClient.SendMessage(
            chat,
            $"У тебя уже создан крепиль с именем {userData.CharacterName}",
            cancellationToken: cancellationToken);
    }
}