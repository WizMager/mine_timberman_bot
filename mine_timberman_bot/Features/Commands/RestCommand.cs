using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace MineTimbermanBot.Features.Commands;

public class RestCommand(
    IUserSessionStore userSessionStore,
    ILogger<RestCommand> logger
) : IBotCommand
{
    public string Name => "rest";

    public string Description => "Попытаться дремануть до приезда ИТР";
    
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
        
        if (userData.CharacterName is null)
        {
            await context.BotClient.SendMessage(
                context.Message.Chat,
                "А кому спать, РМУшники и не просыпались с начала смены? Создай крепиля сразу!",
                cancellationToken: cancellationToken);
            return;
        }
        
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