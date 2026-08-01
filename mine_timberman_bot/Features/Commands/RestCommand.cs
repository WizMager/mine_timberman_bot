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

    protected override string MissingCharacterMessage { get; } =
        "А кому спать, РМУшники и не просыпались с начала смены? Создай крепиля сразу!";

    protected override async Task ExecuteCoreAsync(BotCommandContext context, User user, CancellationToken cancellationToken)
    {
        var userData = SessionStore.GetOrCreate(user.Id);

        if (userData.LastRestTime.Date == DateTime.Today)
        {
            await context.BotClient.SendMessage(
                context.Message.Chat,
                "Нельзя спать когда рядом враги(с) Где-то ходят ИТРовцы!",
                cancellationToken: cancellationToken);
            return;
        }

        var randomValue = userData.Force == 100 ? Random.Shared.Next(101) : Random.Shared.Next(100);
        var isLuckyDay = randomValue < userData.Force;
        var resultText = isLuckyDay
            ? "Тебе удалось кемарнуть, ты чувствуешь силу, юный падаван."
            : "Тебя разбудил начальник за спиной у которого стоял ТБшник. Возможно тебя ждёт сЭкс...";

        if (isLuckyDay)
        {
            userData.Force += 10;
        }
        else
        {
            userData.Force -= Math.Max(1, userData.Force / 4);
        }

        if (userData.Force <= 0)
        {
            userData.Force = 1;
        }

        userData.LastRestTime = DateTime.Today;

        await context.BotClient.SendMessage(
            context.Message.Chat,
            resultText + $"\nТеперь твоя сила равна {userData.Force}%",
            cancellationToken: cancellationToken);
    }
}