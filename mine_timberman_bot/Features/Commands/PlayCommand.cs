using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MineTimbermanBot.Features.Commands;

public sealed class PlayCommand(
    IUserSessionStore sessionStore,
    ILogger<PlayCommand> logger
) : BotCommandBase(logger, sessionStore)
{
    public override string Name => "play";

    public override string Description => "Начать учебную игру";

    protected override async Task ExecuteCoreAsync(BotCommandContext context, User user, CancellationToken cancellationToken)
    {
        var session = SessionStore.GetOrCreate(user.Id);

        lock (session)
        {
            session.State = UserSessionStates.Choosing;
            session.Score = 0;
            session.SelectedSide = null;
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Влево", "side:left"),
            InlineKeyboardButton.WithCallbackData("Вправо", "side:right")
        });

        await context.BotClient.SendMessage(
            context.Message.Chat,
            "Выберите сторону для игры:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}
