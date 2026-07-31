using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace MineTimbermanBot.Features.Commands;

public class DoWorkCommand(
    IUserSessionStore userSessionStore,
    ILogger<DoWorkCommand> logger
) : IBotCommand
{
    public string Name => "work";

    public string Description => "Хорошенько крепануть сегодня";
    
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
                "А кому работать, главному инженеру чтоль? Создай крепиля сразу!",
                cancellationToken: cancellationToken);
            return;
        }
        
        logger.LogDebug(
            "LastWorkTime={LastWorkTime}, Now={Now}",
            userData.LastWorkTime,
            DateTime.Now);

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
        var isLuckyDay = Random.Shared.NextDouble() < userData.Lucky;
        var resultText = Random.Shared.NextDouble() < 0.15
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