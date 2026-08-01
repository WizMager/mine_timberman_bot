using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Features.Commands;

public class DoWorkCommand(
    IUserSessionStore userSessionStore,
    ILogger<DoWorkCommand> logger
) : BotCommandBase(logger, userSessionStore)
{
    public override string Name => "work";

    public override string Description => "Хорошенько крепануть сегодня";

    protected override string MissingCharacterMessage => "А кому работать, главному инженеру чтоль? Создай крепиля сразу!";

    protected override async Task ExecuteCoreAsync(BotCommandContext context, User user, CancellationToken cancellationToken)
    {
        var userData = SessionStore.GetOrCreate(user.Id);

        if (userData.LastWorkTime.Date == DateTime.Today)
        {
            await context.BotClient.SendMessage(
                context.Message.Chat,
                "Крепиль и так уже заебался, хочешь угробить его?",
                cancellationToken: cancellationToken);
            return;
        }

        var boltCount = Random.Shared.Next(1, 5);
        var logsCount = Random.Shared.Next(1, 2);
        var isLuckyDay = Random.Shared.Next(100) < userData.Force;
        var resultText = isLuckyDay
            ? $"Твой крепиль нихуёво работнул и поставил {boltCount} болтов да ещё и въебал стоек {logsCount}!"
            : $"Твой крепиль работнул и поставил болтов - {boltCount}";
        userData.BoltsInWorkSession += boltCount;
        userData.LastWorkTime = DateTime.Now;

        if (isLuckyDay)
        {
            userData.LogsInWorkSession += logsCount;
        }

        await context.BotClient.SendMessage(
            context.Message.Chat,
            resultText,
            cancellationToken: cancellationToken);
    }
}