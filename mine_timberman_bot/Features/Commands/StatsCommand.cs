using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace MineTimbermanBot.Features.Commands;

public class StatsCommand(
    IUserSessionStore userSessionStore,
    ILogger<StatsCommand> logger
) : IBotCommand
{
    public string Name => "stats";

    public string Description => "Узнать свои характеристики";
    
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
            "Характеристики твоего пениса - 4см, а чтобы узнать характеристики крепиля его нужно создать!",
                cancellationToken: cancellationToken);
            return;
        }
        
        await context.BotClient.SendMessage(
            context.Message.Chat,
            $"Характеристики крепиля с именем {userData.CharacterName} такие:" +
            $"\nУстановлено болтов: {userData.BoltsInWorkSession}" + 
            $"\nПоставлено стоек: {userData.LogsInWorkSession}" +
            $"\nУроень СИЛЫ юного крепиляна: {userData.Force}",
            cancellationToken: cancellationToken);
    }
}