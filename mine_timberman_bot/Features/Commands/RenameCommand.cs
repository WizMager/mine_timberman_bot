using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Features.Commands;

public class RenameCommand(
    IUserSessionStore userSessionStore,
    ILogger<RenameCommand> logger
) : BotCommandBase(logger, userSessionStore)
{
    private const int MaxNameLength = 16;

    public override string Name => "rename";

    public override string Description => "Изменить имя крепиля в табеле";

    protected override string MissingCharacterMessage { get; } =
        "В табеле пустая строка, кому имя менять? Создай крепиля сразу";

    protected override async Task ExecuteCoreAsync(BotCommandContext context, User user, CancellationToken cancellationToken)
    {
        var userData = await SessionStore.GetOrCreateAsync(user.Id, cancellationToken);

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