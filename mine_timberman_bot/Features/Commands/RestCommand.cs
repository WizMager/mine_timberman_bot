using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Features.Commands;

public class RestCommand(
    IUserSessionStore userSessionStore,
    ILogger<RestCommand> logger
) : BotCommandBase(logger, userSessionStore)
{
    public override string Name => "rest";

    public override string Description => "Попытаться дремануть до приезда ИТР";

    protected override string MissingCharacterMessage => "А кому спать, РМУшники и не просыпались с начала смены? Создай крепиля сразу!";

    protected override async Task ExecuteCoreAsync(BotCommandContext context, User user, CancellationToken cancellationToken)
    {
        var userData = await SessionStore.GetOrCreateAsync(user.Id, cancellationToken);

        if (userData.LastRestTime.Date == DateTime.Today)
        {
            await context.BotClient.SendMessage(
                context.Message.Chat,
                "Нельзя спать когда рядом враги(с) Где-то ходят ИТРовцы!",
                cancellationToken: cancellationToken);
            return;
        }

        var randomValue = userData.Lucky == 100 ? Random.Shared.Next(101) : Random.Shared.Next(100);
        var isLuckyDay = randomValue < userData.Lucky;
        var resultText = isLuckyDay
            ? $"Твоему {userData.CharacterName} удалось кемарнуть, ты чувствуешь силу, юный падаван."
            : $"Твоего {userData.CharacterName} разбудил начальник за спиной у которого стоял ТБшник. Возможно тебя ждёт сЭкс... Хотя ты и успел дремануть";

        if (isLuckyDay)
        {
            userData.Force += 50;
        }
        else
        {
            userData.Force += 25;
        }

        userData.LastRestTime = DateTime.Today;

        await context.BotClient.SendMessage(
            context.Message.Chat,
            resultText + $"\nТеперь Cила {userData.CharacterName} равна {userData.Force}",
            cancellationToken: cancellationToken);
    }
}