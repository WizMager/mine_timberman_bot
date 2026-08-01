using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Features.Commands;

public class StatsCommand(
    IUserSessionStore userSessionStore,
    ILogger<StatsCommand> logger
) : BotCommandBase(logger, userSessionStore)
{
    public override string Name => "stats";

    public override string Description => "Узнать свои характеристики";

    protected override string MissingCharacterMessage => "Характеристики твоего пениса - 4см, а чтобы узнать характеристики крепиля его нужно создать!";

    protected override async Task ExecuteCoreAsync(BotCommandContext context, User user, CancellationToken cancellationToken)
    {
        var userData = SessionStore.GetOrCreate(user.Id);

        await context.BotClient.SendMessage(
            context.Message.Chat,
            $"Характеристики крепиля с именем {userData.CharacterName} такие:" +
            $"\nУстановлено болтов: {userData.BoltsInWorkSession}" +
            $"\nПоставлено стоек: {userData.LogsInWorkSession}" +
            $"\nУроень СИЛЫ юного крепиляна: {userData.Force}",
            cancellationToken: cancellationToken);
    }
}